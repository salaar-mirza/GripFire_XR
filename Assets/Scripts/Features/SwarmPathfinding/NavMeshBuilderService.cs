using System.Collections.Generic;
using System.Threading.Tasks;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.RoomMapping.Events;
using ARFps.Features.RoomMapping;
using ARFps.Features.SwarmPathfinding.Events;
using UnityEngine;

namespace ARFps.Features.SwarmPathfinding
{
    public class NavMeshBuilderService : IService
    {
        private VirtualRoomView _roomViewPrefab;
        private VirtualRoomView _roomViewInstance;

        public NavMeshBuilderService(VirtualRoomView roomViewPrefab)
        {
            _roomViewPrefab = roomViewPrefab;
        }

        public void OnInit()
        {
            EventBus<PlayableAreaDefinedEvent>.Subscribe(OnPlayableAreaDefined);
            EventBus<FloorMathCalculatedEvent>.Subscribe(OnFloorMathCalculated);
        }

        private void OnPlayableAreaDefined(PlayableAreaDefinedEvent e)
        {
            // Extract data on the Main Thread
            Transform origin = e.MasterOrigin;
            if (origin == null) return;

            // Convert World Space points to drift-proof Local Space before processing
            Vector3[] localBoundaryVertices = new Vector3[e.BoundaryPoints.Count];
            for (int i = 0; i < e.BoundaryPoints.Count; i++)
            {
                localBoundaryVertices[i] = origin.InverseTransformPoint(e.BoundaryPoints[i]);
            }
            
            List<ObstacleData> obstacles = e.Obstacles;
            float ceilingHeight = e.CeilingHeight;
            
            Task.Run(() => GenerateFloorMeshAsync(localBoundaryVertices, origin, obstacles, ceilingHeight));
        }

        private void GenerateFloorMeshAsync(Vector3[] localBoundaryVertices, Transform origin, List<ObstacleData> obstacles, float ceilingHeight)
        {
            // Background Thread processing (No UnityEngine API allowed)
            
            int vertexCount = localBoundaryVertices.Length;
            if (vertexCount < 3) return;

            // Double-Sided Fan Triangulation ensures the NavMesh will successfully bake 
            // regardless of whether the player mapped the room clockwise or counter-clockwise.
            int triangleCount = (vertexCount - 2) * 6; 
            int[] triangles = new int[triangleCount];

            int triIndex = 0;
            for (int i = 1; i < vertexCount - 1; i++)
            {
                // Top Face (Clockwise)
                triangles[triIndex] = 0;
                triangles[triIndex + 1] = i;
                triangles[triIndex + 2] = i + 1;
                triIndex += 3;
                      
                // Bottom Face (Counter-Clockwise)
                triangles[triIndex] = 0;
                triangles[triIndex + 1] = i + 1;
                triangles[triIndex + 2] = i;
                triIndex += 3;
            }

            EventBus<FloorMathCalculatedEvent>.Publish(new FloorMathCalculatedEvent(
                localBoundaryVertices,
                triangles,
                origin,
                obstacles,
                ceilingHeight
            ));
        }

        private void OnFloorMathCalculated(FloorMathCalculatedEvent e)
        {
            // Main Thread logic
            if (_roomViewInstance == null && _roomViewPrefab != null)
            {
                _roomViewInstance = Object.Instantiate(_roomViewPrefab, e.MasterOrigin);
            }
            
            _roomViewInstance.ApplyMesh(e.Vertices, e.Triangles);
            _roomViewInstance.BuildInvisibleWalls(e.Vertices, e.CeilingHeight);
            _roomViewInstance.BakeNavMesh(e.Obstacles);
            _roomViewInstance.BuildCeilingLid(e.Vertices, e.CeilingHeight);
                
            // Notify systems that the room is fully processed
            EventBus<NavMeshBakedEvent>.Publish(new NavMeshBakedEvent());
        }

        public void OnDispose()
        {
            EventBus<PlayableAreaDefinedEvent>.Unsubscribe(OnPlayableAreaDefined);
            EventBus<FloorMathCalculatedEvent>.Unsubscribe(OnFloorMathCalculated);
            if (_roomViewInstance != null) Object.Destroy(_roomViewInstance.gameObject);
        }
    }
}