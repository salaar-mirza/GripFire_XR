using System.Collections.Generic;
using ARFps.Features.RoomMapping;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace ARFps.Features.SwarmPathfinding
{
    /// <summary>
    /// The procedural mesh and navigation surface generation view.
    /// Safely generates invisible boundaries for Unity's NavMesh system.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    [RequireComponent(typeof(NavMeshSurface))]
    public class VirtualRoomView : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private NavMeshSurface _navMeshSurface;
        private MeshRenderer _meshRenderer;
        private List<GameObject> _spawnedObstacles = new List<GameObject>();
        private List<GameObject> _spawnedWalls = new List<GameObject>();
        private GameObject _spawnedCeiling;

        [Tooltip("If true, draws a visible floor. If false, the floor is invisible (better for Mixed Reality) but enemies can still walk on it.")]
        [SerializeField] private bool _renderFloorMesh = false;
        
        [Tooltip("Lifts the floor mesh slightly to prevent Z-fighting with invisible AR planes.")]
        [SerializeField] private float _floorOffset = 0.05f;


        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _navMeshSurface = GetComponent<NavMeshSurface>();
            _meshRenderer = GetComponent<MeshRenderer>();
              
            // Force the NavMesh to bake using Physics Colliders so it generates even if the visual floor is disabled.
            _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        public void ApplyMesh(Vector3[] vertices, int[] triangles)
        {
            // Prevent VRAM Memory Leak by destroying the old mesh before assigning a new one
            if (_meshFilter != null && _meshFilter.mesh != null) {
                Destroy(_meshFilter.mesh);
            }

            // Apply an offset to prevent Z-fighting if the floor is rendered visually
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].y += _floorOffset;
            }

            Mesh newMesh = new Mesh { vertices = vertices, triangles = triangles };
            newMesh.RecalculateNormals(); // Required for lighting to work

            _meshFilter.mesh = newMesh;
            _meshCollider.sharedMesh = newMesh;

            _meshRenderer.enabled = _renderFloorMesh;
        }
        
          
        public void BuildInvisibleWalls(Vector3[] boundaryPoints, float ceilingHeight)
        {
            // Clean up any old walls if the player remaps the room
            foreach (var wall in _spawnedWalls) if (wall != null) Destroy(wall);
            _spawnedWalls.Clear();
 
            if (boundaryPoints == null || boundaryPoints.Length < 3) return;
            
            // First, find the geometric center (centroid) of the room.
            // This is crucial for determining the "outward" direction of each wall.
            Vector3 roomCentroid = Vector3.zero;
            foreach (var point in boundaryPoints)
            {
                roomCentroid += point;
            }
            roomCentroid /= boundaryPoints.Length;
 
            for (int i = 0; i < boundaryPoints.Length; i++)
            {
                // Get this corner and the NEXT corner (looping back to 0 at the end)
                Vector3 pointA = boundaryPoints[i];
                Vector3 pointB = boundaryPoints[(i + 1) % boundaryPoints.Length];
 
                // 1. The Midpoint
                Vector3 midpoint = (pointA + pointB) / 2f;
 
                // 2. The Distance and Direction
                float distance = Vector3.Distance(pointA, pointB);
                Vector3 direction = pointB - pointA;
                
                // 3. The Outward Push
                float wallThickness = 0.5f;
                Vector3 outwardNormal = Vector3.Cross(Vector3.up, direction.normalized);
                
                // Ensure the normal truly points outwards by checking it against the room's center
                if (Vector3.Dot(outwardNormal, roomCentroid - midpoint) > 0)
                {
                    outwardNormal *= -1; // Flip it if it's pointing inwards
                }
                Vector3 thicknessOffset = outwardNormal * (wallThickness / 2.0f);
 
                // 4. Spawn and Align the Wall
                GameObject wall = new GameObject($"InvisibleWall_{i}");
                wall.transform.SetParent(transform, false);
                
                // Thicken the invisible walls and slightly lengthen them to seal any procedural cracks
                wall.transform.localScale = new Vector3(wallThickness, ceilingHeight, distance + 0.1f);
                wall.transform.localRotation = Quaternion.LookRotation(direction);
 
                // Lift the wall up by half its height to sit flush on the floor
                wall.transform.localPosition = new Vector3(midpoint.x + thicknessOffset.x, ceilingHeight / 2f, midpoint.z + thicknessOffset.z);

                wall.AddComponent<BoxCollider>();
                _spawnedWalls.Add(wall);
            }
        }
        
        public void BuildCeilingLid(Vector3[] boundaryPoints, float ceilingHeight)
        {
            if (_spawnedCeiling != null) Destroy(_spawnedCeiling);
            
            if (boundaryPoints == null || boundaryPoints.Length < 3) return;

            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;

            foreach (var pt in boundaryPoints)
            {
                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
                if (pt.z < minZ) minZ = pt.z;
                if (pt.z > maxZ) maxZ = pt.z;
            }

            _spawnedCeiling = new GameObject("InvisibleCeiling");
            _spawnedCeiling.transform.SetParent(transform, false);
            
            // Make the ceiling thick to stop physics tunneling, and add an overhang to seal the corners.
            // Offset the Y position so the flat bottom of the ceiling sits EXACTLY at the ceilingHeight.
            _spawnedCeiling.transform.localPosition = new Vector3((minX + maxX) / 2f, ceilingHeight + 0.5f, (minZ + maxZ) / 2f);
            _spawnedCeiling.transform.localScale = new Vector3((maxX - minX) + 2f, 1.0f, (maxZ - minZ) + 2f); 
            _spawnedCeiling.AddComponent<BoxCollider>();
        }
         
        public void BakeNavMesh(List<ObstacleData> obstacles)
        {
            // Clean up any old obstacle colliders if the player re-mapped the room
            foreach (var obs in _spawnedObstacles) if (obs != null) Destroy(obs);
            _spawnedObstacles.Clear();

            // Spawn invisible cookie-cutter colliders for the NavMeshSurface to read
            if (obstacles != null)
            {
                foreach (var obs in obstacles)
                {
                    if (obs.FootprintCorners == null || obs.FootprintCorners.Length == 0) continue;

                    // Find the Min/Max bounds of the obstacle footprint
                    float minX = float.MaxValue, minZ = float.MaxValue;
                    float maxX = float.MinValue, maxZ = float.MinValue;
                    
                    foreach (var pt in obs.FootprintCorners)
                    {
                        if (pt.x < minX) minX = pt.x;
                        if (pt.x > maxX) maxX = pt.x;
                        if (pt.z < minZ) minZ = pt.z;
                        if (pt.z > maxZ) maxZ = pt.z;
                    }

                    // Create a lightweight, invisible physical blocker
                    GameObject blocker = new GameObject("ObstacleBlocker");
                    blocker.transform.SetParent(transform, false);
                    
                    blocker.transform.localPosition = new Vector3((minX + maxX) / 2f, obs.Height / 2f, (minZ + maxZ) / 2f);
                    blocker.transform.localScale = new Vector3(maxX - minX, obs.Height, maxZ - minZ);
                    
                    blocker.AddComponent<BoxCollider>();
                    _spawnedObstacles.Add(blocker);
                }
            }

            _navMeshSurface.BuildNavMesh();
        }
    }
}