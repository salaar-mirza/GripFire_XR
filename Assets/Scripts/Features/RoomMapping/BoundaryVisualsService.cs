using System.Collections.Generic;
using System.Linq;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.RoomMapping.Events;
using UnityEngine;
using UnityEngine.Pool;

namespace ARFps.Features.RoomMapping
{
    /// <summary>
    /// Listens to mapping events and manages a memory-safe Object Pool of physical markers.
    /// Adheres to RULE 1 (Decoupling) and RULE 5 (Object Pooling).
    /// </summary>
    public class BoundaryVisualsService : IService, ITickable
    {
        private readonly ManualRoomMappingConfig _config;
        private ObjectPool<GameObject> _markerPool;
        private ObjectPool<GameObject> _obstacleMarkerPool;
        
        private LineRenderer _perimeterLine;
        private LineRenderer _topPerimeterLine;
        private LineRenderer _liveVerticalLine;
        
        private ObjectPool<LineRenderer> _verticalPillarPool;
        private ObjectPool<LineRenderer> _polygonLinePool;
        
        private ObjectPool<GameObject> _measurementTextPool;
        private GameObject _ceilingTextObj;
        private Material _lineMaterial;

        private ManualRoomMappingService _mappingService;

        // A helper class to group and animate a finished 3D shape (Floor, Ceiling, and Pillars)
        private class ShapeGroup
        {
            public LineRenderer FloorLine;
            public LineRenderer CeilingLine;
            public List<LineRenderer> Pillars = new List<LineRenderer>();
            public float TargetHeight;
            public List<Vector3> BasePoints;
            public float AnimationProgress;
            public List<GameObject> TopMarkers = new List<GameObject>();
            public ObjectPool<GameObject> MarkerPool;
            public float MarkerYOffset;
        }

        private readonly Stack<GameObject> _activeMarkers = new Stack<GameObject>();
        private readonly Stack<ShapeGroup> _activeRoomShapes = new Stack<ShapeGroup>();
        
        private readonly Stack<GameObject> _activeObstacleMarkers = new Stack<GameObject>();
        private readonly Stack<ShapeGroup> _activeObstacleShapes = new Stack<ShapeGroup>();
        private readonly List<ShapeGroup> _animatingShapes = new List<ShapeGroup>();
        private readonly List<GameObject> _activeMeasurementTexts = new List<GameObject>();

        // Lifts floor objects by 1cm to prevent clipping into the invisible AR Occlusion Planes
        private const float FloorOffset = 0.01f;

        private Transform _masterOrigin;

        public BoundaryVisualsService(ManualRoomMappingConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            _mappingService = GameService.Get<ManualRoomMappingService>();
            
            _lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Color.cyan };
            
            _perimeterLine = CreateLine("LivePerimeterLine");
            _perimeterLine.useWorldSpace = true; // Live rubber-band lines still use World Space
            
            _topPerimeterLine = CreateLine("LiveTopPerimeterLine");
            _topPerimeterLine.useWorldSpace = true;
            
            _liveVerticalLine = CreateLine("LiveVerticalLine");
            _liveVerticalLine.useWorldSpace = true;
            _liveVerticalLine.loop = false; // Vertical line is just a straight segment
            
            _markerPool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(_config.BoundaryMarkerPrefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => { obj.SetActive(false); obj.transform.SetParent(null, false); },
                actionOnDestroy: Object.Destroy,
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            );
            
            _verticalPillarPool = new ObjectPool<LineRenderer>(
                createFunc: () => CreateLine("VerticalPillar"),
                actionOnGet: lr => { lr.gameObject.SetActive(true); lr.loop = false; },
                actionOnRelease: lr => { lr.gameObject.SetActive(false); lr.transform.SetParent(null, false); lr.positionCount = 0; },
                actionOnDestroy: lr => Object.Destroy(lr.gameObject),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            );
               
            _polygonLinePool = new ObjectPool<LineRenderer>(
                createFunc: () => CreateLine("PermanentPolygon"),
                actionOnGet: lr => { lr.gameObject.SetActive(true); },
                actionOnRelease: lr => { lr.gameObject.SetActive(false); lr.transform.SetParent(null, false); lr.positionCount = 0; },
                actionOnDestroy: lr => Object.Destroy(lr.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
              
            _obstacleMarkerPool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(_config.ObstacleMarkerPrefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => { obj.SetActive(false); obj.transform.SetParent(null, false); },
                actionOnDestroy: Object.Destroy,
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 100
            );
            
            if (_config.MeasurementTextPrefab != null)
            {
                _measurementTextPool = new ObjectPool<GameObject>(
                    createFunc: () => Object.Instantiate(_config.MeasurementTextPrefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => { obj.SetActive(false); obj.transform.SetParent(null, false); },
                    actionOnDestroy: Object.Destroy,
                    collectionCheck: false,
                    defaultCapacity: 20,
                    maxSize: 100
                );
            }

            EventBus<FloorOriginSetEvent>.Subscribe(OnFloorOriginSet);
            EventBus<FloorOriginUndoneEvent>.Subscribe(OnFloorOriginUndone);
            EventBus<CeilingHeightSetEvent>.Subscribe(OnCeilingHeightSet);
            EventBus<CeilingHeightUndoneEvent>.Subscribe(OnCeilingHeightUndone);
            EventBus<ObstacleHeightSetEvent>.Subscribe(OnObstacleHeightSet);
            EventBus<ObstacleHeightUndoneEvent>.Subscribe(OnObstacleHeightUndone);
            EventBus<ObstaclePointAddedEvent>.Subscribe(OnObstaclePointAdded);
            EventBus<ObstaclePointRemovedEvent>.Subscribe(OnObstaclePointRemoved);
            EventBus<BoundaryPointAddedEvent>.Subscribe(OnPointAdded);
            EventBus<BoundaryPointRemovedEvent>.Subscribe(OnPointRemoved);
            EventBus<PlayableAreaDefinedEvent>.Subscribe(OnMappingComplete);
            EventBus<ObstacleMappingStartedEvent>.Subscribe(OnShapeFinished);
            EventBus<RoomBoundariesRestoredEvent>.Subscribe(OnRoomBoundariesRestored);
            EventBus<ObstacleRestoredEvent>.Subscribe(OnObstacleRestored);
        }

        private LineRenderer CreateLine(string name)
        {
            var lr = new GameObject(name).AddComponent<LineRenderer>();
            lr.startWidth = 0.015f;
            lr.endWidth = 0.015f;
            lr.useWorldSpace = false; // THE FIX: Permanent lines must obey their parent's AR drift!
            lr.loop = true; // Auto-closes the polygon!
            lr.positionCount = 0;
            lr.material = _lineMaterial;
            return lr;
        }

        private void AddMeasurementText(Vector3 p1, Vector3 p2)
        {
            if (_measurementTextPool == null) return;
            float distance = Vector3.Distance(p1, p2);
            Vector3 midpoint = (p1 + p2) / 2f;

            GameObject textObj = _measurementTextPool.Get();
            if (_masterOrigin != null) textObj.transform.SetParent(_masterOrigin, false);
            textObj.transform.position = midpoint + new Vector3(0, FloorOffset * 2f, 0);
            
            // Flip the text 180 degrees if it is upside down relative to the camera
            Vector3 wallDir = p2 - p1;
            if (Camera.main != null)
            {
                Vector3 toCam = Camera.main.transform.position - midpoint;
                toCam.y = 0;
                if (Vector3.Dot(Vector3.Cross(Vector3.up, wallDir), toCam) > 0)
                {
                    wallDir = -wallDir;
                }
            }
            
            if (wallDir != Vector3.zero) textObj.transform.rotation = Quaternion.LookRotation(wallDir) * Quaternion.Euler(90, 90, 0);
            if (textObj.TryGetComponent<TMPro.TextMeshPro>(out var tmp)) tmp.text = $"{distance:F2}m";
            _activeMeasurementTexts.Add(textObj);
        }

        private void RemoveLastMeasurementText()
        {
            if (_activeMeasurementTexts.Count > 0 && _measurementTextPool != null)
            {
                var lastText = _activeMeasurementTexts[_activeMeasurementTexts.Count - 1];
                _activeMeasurementTexts.RemoveAt(_activeMeasurementTexts.Count - 1);
                _measurementTextPool.Release(lastText);
            }
        }

        public void OnTick()
        {
            if (_mappingService == null || _perimeterLine == null) return;
            
            // Process the "Lightsaber" Extrusion Animations
            for (int i = _animatingShapes.Count - 1; i >= 0; i--)
            {
                var shape = _animatingShapes[i];
                shape.AnimationProgress += Time.deltaTime * 1.5f; // Speed of the extrusion
                float currentYOffset = Mathf.Lerp(0, shape.TargetHeight, shape.AnimationProgress);

                // Animate Ceiling moving up
                for (int p = 0; p < shape.BasePoints.Count; p++)
                {
                    Vector3 basePt = shape.BasePoints[p];
                    shape.CeilingLine.SetPosition(p, new Vector3(basePt.x, basePt.y + currentYOffset, basePt.z));
                }

                // Animate Pillars growing up
                for (int p = 0; p < shape.Pillars.Count; p++)
                {
                    Vector3 basePt = shape.BasePoints[p];
                    shape.Pillars[p].SetPosition(1, new Vector3(basePt.x, basePt.y + currentYOffset, basePt.z));
                }
            
                // Animate Top Markers growing up
                for (int p = 0; p < shape.TopMarkers.Count; p++)
                {
                    Vector3 basePt = shape.BasePoints[p];
                    // We add currentYOffset AND the MarkerYOffset so they don't sink into the ceiling line
                    shape.TopMarkers[p].transform.localPosition = new Vector3(basePt.x, basePt.y + currentYOffset + shape.MarkerYOffset + FloorOffset, basePt.z);
                }

                // Stop animating when it hits 100%
                if (shape.AnimationProgress >= 1f)
                {
                    _animatingShapes.RemoveAt(i);
                }
            }

            // Billboard the hanging ceiling text so it's always perfectly readable from any angle
            if (_ceilingTextObj != null && Camera.main != null)
            {
                _ceilingTextObj.transform.rotation = Quaternion.LookRotation(_ceilingTextObj.transform.position - Camera.main.transform.position);
            }
          
            if (_mappingService.CurrentState == MappingState.DefiningBoundaries)
            {
                _perimeterLine.material.color = Color.cyan;
                _topPerimeterLine.material.color = Color.cyan;
                _liveVerticalLine.material.color = Color.cyan;
                DrawRubberBand3D(_mappingService.BoundaryPoints.ToList(), _mappingService.CurrentTargetPosition, _mappingService.CeilingHeight);
            }
            else if (_mappingService.CurrentState == MappingState.DefiningObstacleBoundaries)
            {
                _perimeterLine.material.color = Color.red;
                _topPerimeterLine.material.color = Color.red;
                _liveVerticalLine.material.color = Color.red;
                DrawRubberBand3D(_mappingService.CurrentObstaclePoints.ToList(), _mappingService.CurrentTargetPosition, _mappingService.CurrentObstacleHeight);
            }
            else
            {
                _perimeterLine.positionCount = 0;
                _topPerimeterLine.positionCount = 0;
                _liveVerticalLine.positionCount = 0;
            }
        }
 
        private void DrawRubberBand3D(List<Vector3> points, Vector3? currentTarget, float height)
        {
            int count = points.Count;
            int totalPoints = currentTarget.HasValue ? count + 1 : count;
 
            _perimeterLine.positionCount = totalPoints;
            _topPerimeterLine.positionCount = totalPoints;
 
            for (int i = 0; i < count; i++)
            {
                _perimeterLine.SetPosition(i, points[i] + new Vector3(0, FloorOffset, 0));
                _topPerimeterLine.SetPosition(i, new Vector3(points[i].x, points[i].y + height, points[i].z));
            }

            if (currentTarget.HasValue)
            {
                Vector3 floorTarget = currentTarget.Value;
                Vector3 ceilingTarget = new Vector3(floorTarget.x, floorTarget.y + height, floorTarget.z);
 
                _perimeterLine.SetPosition(count, floorTarget + new Vector3(0, FloorOffset, 0));
                _topPerimeterLine.SetPosition(count, ceilingTarget);
 
                // Draw the live vertical pillar
                _liveVerticalLine.positionCount = 2;
                _liveVerticalLine.SetPosition(0, floorTarget + new Vector3(0, FloorOffset, 0));
                _liveVerticalLine.SetPosition(1, ceilingTarget);
            }
            else
            {
                _liveVerticalLine.positionCount = 0;
            }
        }
        
        private void DrawPermanentShape(List<Vector3> points, float height, Color color, Stack<ShapeGroup> trackingStack, ObjectPool<GameObject> markerPool)
        {
            ShapeGroup group = new ShapeGroup();
            group.TargetHeight = height;
            group.AnimationProgress = 0f;
            group.MarkerPool = markerPool;
            bool offsetCalculated = false;
 
            group.BasePoints = new List<Vector3>();
            foreach (var pt in points)
            {
                // Convert World points to Local Space immediately!
                group.BasePoints.Add(_masterOrigin != null ? _masterOrigin.InverseTransformPoint(pt) : pt);
            }
 
            // 1. Draw Floor
            group.FloorLine = _polygonLinePool.Get();
            group.FloorLine.material.color = color;
            group.FloorLine.positionCount = group.BasePoints.Count;
            if (_masterOrigin != null) group.FloorLine.transform.SetParent(_masterOrigin, false);
            for (int i = 0; i < group.BasePoints.Count; i++) group.FloorLine.SetPosition(i, group.BasePoints[i] + new Vector3(0, FloorOffset, 0));
 
            // 2. Draw Ceiling (Start it hidden at the floor!)
            group.CeilingLine = _polygonLinePool.Get();
            group.CeilingLine.material.color = color;
            group.CeilingLine.positionCount = group.BasePoints.Count;
            if (_masterOrigin != null) group.CeilingLine.transform.SetParent(_masterOrigin, false);
            for (int i = 0; i < group.BasePoints.Count; i++) group.CeilingLine.SetPosition(i, group.BasePoints[i] + new Vector3(0, FloorOffset, 0));
 
            // 3. Draw Vertical Pillars (Start them hidden at the floor!)
            for (int i = 0; i < group.BasePoints.Count; i++)
            {
                var localPt = group.BasePoints[i];
                var pillar = _verticalPillarPool.Get();
                pillar.material.color = color;
                pillar.positionCount = 2;
                
                if (_masterOrigin != null) pillar.transform.SetParent(_masterOrigin, false);
                pillar.SetPosition(0, localPt + new Vector3(0, FloorOffset, 0));
                pillar.SetPosition(1, localPt + new Vector3(0, FloorOffset, 0)); // Top point starts at floor
                group.Pillars.Add(pillar);
                 
                // Spawn the Top Marker and start it hidden at the floor!
                var topMarker = markerPool.Get();
                if (!offsetCalculated)
                {
                    group.MarkerYOffset = GetMarkerHalfHeight(topMarker);
                    offsetCalculated = true; // Calculate once per shape to save CPU overhead
                }
                if (_masterOrigin != null) topMarker.transform.SetParent(_masterOrigin, false);
                topMarker.transform.localPosition = localPt + new Vector3(0, group.MarkerYOffset + FloorOffset, 0); 
                group.TopMarkers.Add(topMarker);
            }
 
            trackingStack.Push(group);
            _animatingShapes.Add(group); // Tell OnTick to start growing it!
        }

        private void OnPointAdded(BoundaryPointAddedEvent e)
        {
            if (_config.BoundaryMarkerPrefab == null) return;
            var marker = _markerPool.Get();
             
            float yOffset = GetMarkerHalfHeight(marker);
            Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(e.NewPoint) : e.NewPoint;   
            
            // Parent it to the AR Anchor so it never drifts!
            if (_masterOrigin != null) marker.transform.SetParent(_masterOrigin, false);
            marker.transform.localPosition = localPt + new Vector3(0, yOffset + FloorOffset, 0);
            
            _activeMarkers.Push(marker);
            
            // Draw the permanent length text at the midpoint
            if (_mappingService.BoundaryPoints.Count > 1)
            {
                Vector3 p1 = _mappingService.BoundaryPoints[_mappingService.BoundaryPoints.Count - 2];
                Vector3 p2 = _mappingService.BoundaryPoints[_mappingService.BoundaryPoints.Count - 1];
                AddMeasurementText(p1, p2);
            }
        }
        
        private void OnObstacleHeightSet(ObstacleHeightSetEvent e)
        {
            if (_config.ObstacleMarkerPrefab == null) return;
            var marker = _obstacleMarkerPool.Get();
            float yOffset = GetMarkerHalfHeight(marker);
            
            Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(e.CeilingPoint) : e.CeilingPoint;
            if (_masterOrigin != null) marker.transform.SetParent(_masterOrigin, false);
            marker.transform.localPosition = localPt + new Vector3(0, yOffset, 0);
            
            _activeObstacleMarkers.Push(marker);

            // Draw the height text for the obstacle
            if (_measurementTextPool != null)
            {
                GameObject textObj = _measurementTextPool.Get();
                if (_masterOrigin != null) textObj.transform.SetParent(_masterOrigin, false);
                textObj.transform.position = e.CeilingPoint + new Vector3(0, 0.05f, 0); // Slightly above the marker
                
                // Stand the text upright, facing the camera
                if (Camera.main != null)
                {
                    Vector3 fromCam = textObj.transform.position - Camera.main.transform.position;
                    fromCam.y = 0; // Keep it perfectly vertical
                    if (fromCam != Vector3.zero) textObj.transform.rotation = Quaternion.LookRotation(fromCam);
                }
                
                if (textObj.TryGetComponent<TMPro.TextMeshPro>(out var tmp)) tmp.text = $"Height: {_mappingService.CurrentObstacleHeight:F2}m";
                _activeMeasurementTexts.Add(textObj);
            }
        }
         
        private void OnObstaclePointAdded(ObstaclePointAddedEvent e)
        {
            if (_config.ObstacleMarkerPrefab == null) return;
            var marker = _obstacleMarkerPool.Get();
            float yOffset = GetMarkerHalfHeight(marker);

            Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(e.NewPoint) : e.NewPoint;
            if (_masterOrigin != null) marker.transform.SetParent(_masterOrigin, false);
            marker.transform.localPosition = localPt + new Vector3(0, yOffset + FloorOffset, 0);
            
            _activeObstacleMarkers.Push(marker);

            // Draw the permanent length text at the midpoint for obstacles
            if (_mappingService.CurrentObstaclePoints.Count > 1)
            {
                Vector3 p1 = _mappingService.CurrentObstaclePoints[_mappingService.CurrentObstaclePoints.Count - 2];
                Vector3 p2 = _mappingService.CurrentObstaclePoints[_mappingService.CurrentObstaclePoints.Count - 1];
                AddMeasurementText(p1, p2);
            }
        }
        
        private void OnCeilingHeightSet(CeilingHeightSetEvent e)
        {
            if (_config.BoundaryMarkerPrefab == null) return;
            var marker = _markerPool.Get();

            Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(e.CeilingPoint) : e.CeilingPoint;
            if (_masterOrigin != null) marker.transform.SetParent(_masterOrigin, false);
            marker.transform.localPosition = localPt;

            _activeMarkers.Push(marker);

            if (_measurementTextPool != null)
            {
                _ceilingTextObj = _measurementTextPool.Get();
                if (_masterOrigin != null) _ceilingTextObj.transform.SetParent(_masterOrigin, false);
                _ceilingTextObj.transform.position = e.CeilingPoint + new Vector3(0, -0.1f, 0); // Slightly below ceiling
                
                if (_ceilingTextObj.TryGetComponent<TMPro.TextMeshPro>(out var tmp)) tmp.text = $"Ceiling: {_mappingService.CeilingHeight:F2}m";
            }
        }
        
        private void OnFloorOriginSet(FloorOriginSetEvent e)
        {
            if (_config.BoundaryMarkerPrefab == null) return;
            _masterOrigin = e.MasterOrigin; // Cache the master origin FIRST
            
            var marker = _markerPool.Get();
              
            float yOffset = GetMarkerHalfHeight(marker);
            
            Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(e.OriginPosition) : e.OriginPosition;
            if (_masterOrigin != null) marker.transform.SetParent(_masterOrigin, false);
            marker.transform.localPosition = localPt + new Vector3(0, yOffset + FloorOffset, 0);
            
            _activeMarkers.Push(marker);
        }
        
        private float GetMarkerHalfHeight(GameObject marker)
        {
            // Try to find the height from the visual mesh first, then fallback to a collider if available
            if (marker.TryGetComponent<Renderer>(out var renderer)) return renderer.bounds.extents.y;
            if (marker.TryGetComponent<Collider>(out var collider)) return collider.bounds.extents.y;
            return 0f; // Default to 0 if the prefab has neither
        }

        private void OnPointRemoved(BoundaryPointRemovedEvent e) 
        { 
            ReleaseLastMarker(); 
            RemoveLastMeasurementText();
        }
        
        private void OnFloorOriginUndone(FloorOriginUndoneEvent e) => ReleaseLastMarker();

        private void OnCeilingHeightUndone(CeilingHeightUndoneEvent e) 
        { 
            ReleaseLastMarker(); 
            if (_ceilingTextObj != null && _measurementTextPool != null) { _measurementTextPool.Release(_ceilingTextObj); _ceilingTextObj = null; }
        }
        
        private void OnObstacleHeightUndone(ObstacleHeightUndoneEvent e) 
        { 
            ReleaseLastObstacleMarker(); 
            RemoveLastMeasurementText();
        }
  
        private void OnObstaclePointRemoved(ObstaclePointRemovedEvent e) 
        { 
            ReleaseLastObstacleMarker(); 
            RemoveLastMeasurementText();
        }
         
        private void ReleaseShapeGroup(ShapeGroup shape)
        {
            _animatingShapes.Remove(shape); // Stop animating if it was
            _polygonLinePool.Release(shape.FloorLine);
            _polygonLinePool.Release(shape.CeilingLine);
            foreach (var p in shape.Pillars) _verticalPillarPool.Release(p);
            foreach (var m in shape.TopMarkers) shape.MarkerPool.Release(m);
        }
        
        private void OnShapeFinished(ObstacleMappingStartedEvent e)
        {
            if (_activeRoomShapes.Count == 0)
            {
                // The Room boundaries just finished mapping
                if (_mappingService.BoundaryPoints.Count > 2)
                {
                    Vector3 p1 = _mappingService.BoundaryPoints[_mappingService.BoundaryPoints.Count - 1];
                    Vector3 p2 = _mappingService.BoundaryPoints[0];
                    AddMeasurementText(p1, p2);
                }
                
                DrawPermanentShape(_mappingService.BoundaryPoints.ToList(), _mappingService.CeilingHeight, Color.cyan, _activeRoomShapes, _markerPool);
            }
            else if (_mappingService.Obstacles.Any())
            {
                // An obstacle just finished mapping
                var lastObs = _mappingService.Obstacles.Last();
                var corners = lastObs.FootprintCorners.ToList(); // Convert to concrete list to avoid explicit interface Count errors
                
                if (corners.Count > 2)
                {
                    // Obstacle points are saved in Local Space, so we convert them back to World Space for the distance math
                    Vector3 p1 = _masterOrigin != null ? _masterOrigin.TransformPoint(corners[corners.Count - 1]) : corners[corners.Count - 1];
                    Vector3 p2 = _masterOrigin != null ? _masterOrigin.TransformPoint(corners[0]) : corners[0];
                    AddMeasurementText(p1, p2);
                }

                // Convert the drift-proof Local coordinates back into World coordinates for the visual LineRenderer!
                List<Vector3> worldCorners = new List<Vector3>();
                foreach (var localPt in corners)
                {
                    worldCorners.Add(_masterOrigin != null ? _masterOrigin.TransformPoint(localPt) : localPt);
                }
                
                DrawPermanentShape(worldCorners, lastObs.Height, Color.red, _activeObstacleShapes, _obstacleMarkerPool);
            }
        }
         
        private void OnRoomBoundariesRestored(RoomBoundariesRestoredEvent e)
        {
            if (_activeRoomShapes.Count > 0) ReleaseShapeGroup(_activeRoomShapes.Pop());
            RemoveLastMeasurementText();
        }
         
        private void OnObstacleRestored(ObstacleRestoredEvent e)
        {
            // Pop the shape group for the undone obstacle
            if (_activeObstacleShapes.Count > 0) ReleaseShapeGroup(_activeObstacleShapes.Pop());
            RemoveLastMeasurementText();
        }
        
        private void ReleaseLastObstacleMarker() 
        { 
            if (_activeObstacleMarkers.Count > 0) _obstacleMarkerPool.Release(_activeObstacleMarkers.Pop()); 
        }
            
        private void ClearAllMarkers()
        {
            while (_activeMarkers.Count > 0) ReleaseLastMarker();
            while (_activeMeasurementTexts.Count > 0) RemoveLastMeasurementText();
            if (_ceilingTextObj != null && _measurementTextPool != null) { _measurementTextPool.Release(_ceilingTextObj); _ceilingTextObj = null; }
        }

        private void OnMappingComplete(PlayableAreaDefinedEvent e) 
        { 
            ClearAllMarkers(); 
            while (_activeObstacleMarkers.Count > 0) _obstacleMarkerPool.Release(_activeObstacleMarkers.Pop()); 
            while (_activeRoomShapes.Count > 0) ReleaseShapeGroup(_activeRoomShapes.Pop());
            while (_activeObstacleShapes.Count > 0) ReleaseShapeGroup(_activeObstacleShapes.Pop());
        }
        
        private void ReleaseLastMarker() { if (_activeMarkers.Count > 0) _markerPool.Release(_activeMarkers.Pop()); }

        public void OnDispose()
        {
            EventBus<FloorOriginSetEvent>.Unsubscribe(OnFloorOriginSet);
            EventBus<FloorOriginUndoneEvent>.Unsubscribe(OnFloorOriginUndone);
            EventBus<CeilingHeightSetEvent>.Unsubscribe(OnCeilingHeightSet);
            EventBus<CeilingHeightUndoneEvent>.Unsubscribe(OnCeilingHeightUndone);
            EventBus<ObstacleHeightSetEvent>.Unsubscribe(OnObstacleHeightSet);
            EventBus<ObstacleHeightUndoneEvent>.Unsubscribe(OnObstacleHeightUndone);
            EventBus<ObstaclePointAddedEvent>.Unsubscribe(OnObstaclePointAdded);
            EventBus<ObstaclePointRemovedEvent>.Unsubscribe(OnObstaclePointRemoved);
            EventBus<BoundaryPointAddedEvent>.Unsubscribe(OnPointAdded);
            EventBus<BoundaryPointRemovedEvent>.Unsubscribe(OnPointRemoved);
            EventBus<PlayableAreaDefinedEvent>.Unsubscribe(OnMappingComplete);
            EventBus<ObstacleMappingStartedEvent>.Unsubscribe(OnShapeFinished);
            EventBus<RoomBoundariesRestoredEvent>.Unsubscribe(OnRoomBoundariesRestored);
            EventBus<ObstacleRestoredEvent>.Unsubscribe(OnObstacleRestored);

            _markerPool?.Clear();
            _obstacleMarkerPool?.Clear();
            _verticalPillarPool?.Clear();
            _polygonLinePool?.Clear();
            _measurementTextPool?.Clear();
            
            if (_perimeterLine != null) Object.Destroy(_perimeterLine.gameObject);
            if (_topPerimeterLine != null) Object.Destroy(_topPerimeterLine.gameObject);
            if (_liveVerticalLine != null) Object.Destroy(_liveVerticalLine.gameObject);
        }
    }
}