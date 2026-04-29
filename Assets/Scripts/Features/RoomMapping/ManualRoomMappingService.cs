using System.Collections.Generic;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.RoomMapping.Events;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARFps.Features.RoomMapping
{
    public enum MappingState
    {
        SettingFloorOrigin,
        SettingCeilingHeight,
        DefiningBoundaries,
        SettingObstacleHeight,
        DefiningObstacleBoundaries,
        Complete
    }

    public class ManualRoomMappingService : IService, ITickable
    {
        // Dependencies
        private readonly ManualRoomMappingConfig _config;
        private readonly ARPlaneManager _planeManager;
        private readonly ARRaycastManager _raycastManager;
        private readonly FloorReticleView _reticlePrefab;
        private readonly Camera _mainCamera;

        private FloorReticleView _reticleInstance;
        private static readonly List<ARRaycastHit> s_hits = new List<ARRaycastHit>();

        // Room State & Geometry Data
        private ARAnchor _masterOriginAnchor;
        private float _ceilingHeight;
        private readonly List<Vector3> _localBoundaryPoints = new List<Vector3>();
        private readonly List<Vector3> _worldBoundaryPoints = new List<Vector3>();
        
        private readonly List<ObstacleData> _obstacles = new List<ObstacleData>();
        private float _currentObstacleHeight;
        private readonly List<Vector3> _localObstaclePoints = new List<Vector3>();
        private readonly List<Vector3> _worldObstaclePoints = new List<Vector3>();
        
        // Public Properties
        public MappingState CurrentState { get; private set; } = MappingState.SettingFloorOrigin;
        public Vector3? CurrentTargetPosition { get; private set; }
        public bool CanUndo => CurrentState != MappingState.SettingFloorOrigin && CurrentState != MappingState.Complete;
        public int MinCornersRequired => _config.MinCornersRequired;
        
        // Exposed world data for the UI and Visuals Services (drift-corrected)
        public IReadOnlyList<Vector3> BoundaryPoints => _worldBoundaryPoints;
        public IReadOnlyList<Vector3> CurrentObstaclePoints => _worldObstaclePoints;
        public float CeilingHeight => _ceilingHeight;
        public float CurrentObstacleHeight => _currentObstacleHeight;
        public IReadOnlyList<ObstacleData> Obstacles => _obstacles;

        public ManualRoomMappingService(ManualRoomMappingConfig config, ARRaycastManager raycastManager, ARPlaneManager planeManager, FloorReticleView reticleView)
        {
            _config = config;
            _planeManager = planeManager;
            _raycastManager = raycastManager;
            _reticlePrefab = reticleView; 
            _mainCamera = Camera.main;
        }

        public void OnInit()
        {
            if (_reticlePrefab != null)
            {
                _reticleInstance = Object.Instantiate(_reticlePrefab);
                _reticleInstance.Hide(); // Start hidden
            }
            
            Debug.Log("[ManualRoomMappingService] Initialized. Waiting for player to set floor origin.");
        }
        
        public void OnTick()
        {
            if (CurrentState == MappingState.Complete || _raycastManager == null || _reticleInstance == null) return;
            
            // --- THE AR DRIFT FIX ---
            // Constantly sync World lists from Local points to prevent visual floating.
            if (_masterOriginAnchor != null)
            {
                _worldBoundaryPoints.Clear();
                foreach (var localPt in _localBoundaryPoints) _worldBoundaryPoints.Add(_masterOriginAnchor.transform.TransformPoint(localPt));
                    
                _worldObstaclePoints.Clear();
                foreach (var localPt in _localObstaclePoints) _worldObstaclePoints.Add(_masterOriginAnchor.transform.TransformPoint(localPt));
            }

            // Hide the laser and skip raycasting while the player is looking up to set the ceiling
            if (CurrentState == MappingState.SettingCeilingHeight)
            {
                _reticleInstance.Hide();
                return;
            }

            // During setup, show a reticle on the floor so the player knows where they are targeting
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            if (_raycastManager.Raycast(screenCenter, s_hits, TrackableType.PlaneWithinPolygon))
            {
                _reticleInstance.Show();
                _reticleInstance.SetPose(s_hits[0].pose.position, s_hits[0].pose.rotation, _mainCamera.transform.position);
                CurrentTargetPosition = s_hits[0].pose.position;

                if (CurrentState == MappingState.DefiningBoundaries && _worldBoundaryPoints.Count > 0)
                {
                    float distance = Vector3.Distance(_worldBoundaryPoints[_worldBoundaryPoints.Count - 1], s_hits[0].pose.position);
                    _reticleInstance.UpdateLiveMeasurement(distance);
                }
                else if (CurrentState == MappingState.DefiningObstacleBoundaries && _worldObstaclePoints.Count > 0)
                {
                    float distance = Vector3.Distance(_worldObstaclePoints[_worldObstaclePoints.Count - 1], s_hits[0].pose.position);
                    _reticleInstance.UpdateLiveMeasurement(distance);
                }
                else
                {
                    _reticleInstance.UpdateLiveMeasurement(0f);
                }
            }
            else
            {
                _reticleInstance.Hide();
                _reticleInstance.UpdateLiveMeasurement(0f);
                CurrentTargetPosition = null;
            }
        }

        public void ProcessPlayerAction()
        {
            switch (CurrentState)
            {
                case MappingState.SettingFloorOrigin:
                    SetFloorOrigin();
                    break;
                case MappingState.SettingCeilingHeight:
                    SetCeilingHeight();
                    break;
                case MappingState.DefiningBoundaries:
                    AddBoundaryPoint();
                    break;
                case MappingState.SettingObstacleHeight:
                    SetObstacleHeight();
                    break;
                case MappingState.DefiningObstacleBoundaries:
                    AddObstacleBoundaryPoint();
                    break;
            }
        }

        private void SetFloorOrigin()
        {
            // We can only set the origin if the reticle is on a valid plane
            if (s_hits.Count == 0)
            {
                Debug.LogWarning("[ManualRoomMappingService] Cannot set origin. Point at a detected floor plane.");
                return;
            }
            
            GameObject originObj = new GameObject("MasterGameplayOrigin");
            originObj.transform.SetPositionAndRotation(s_hits[0].pose.position, s_hits[0].pose.rotation);
            _masterOriginAnchor = originObj.AddComponent<ARAnchor>();
            
            CurrentState = MappingState.SettingCeilingHeight;
            EventBus<FloorOriginSetEvent>.Publish(new FloorOriginSetEvent(_masterOriginAnchor.transform.position, _masterOriginAnchor.transform));
            Debug.Log($"[ManualRoomMappingService] Floor Origin Locked at Y: {_masterOriginAnchor.transform.position.y}");
        }

        private void SetCeilingHeight()
        {
            _ceilingHeight = _mainCamera.transform.position.y - _masterOriginAnchor.transform.position.y;
            CurrentState = MappingState.DefiningBoundaries;
            EventBus<CeilingHeightSetEvent>.Publish(new CeilingHeightSetEvent(_mainCamera.transform.position));
            Debug.Log($"[ManualRoomMappingService] Ceiling Height set to: {_ceilingHeight}m");
        }

        private void AddBoundaryPoint()
        {
            // We can only add a corner if the reticle is on a valid plane
            if (s_hits.Count == 0)
            {
                Debug.LogWarning("[ManualRoomMappingService] Cannot add corner. Point at a detected floor plane.");
                return;
            }
            
            Vector3 rawPos = s_hits[0].pose.position;
            Vector3 floorPoint = new Vector3(rawPos.x, _masterOriginAnchor.transform.position.y, rawPos.z);
            
            Vector3 localPoint = _masterOriginAnchor.transform.InverseTransformPoint(floorPoint);
            _localBoundaryPoints.Add(localPoint);
            _worldBoundaryPoints.Add(floorPoint);
            
            EventBus<BoundaryPointAddedEvent>.Publish(new BoundaryPointAddedEvent(floorPoint, _masterOriginAnchor.transform));
            Debug.Log($"[ManualRoomMappingService] Boundary point {_localBoundaryPoints.Count} added at {floorPoint}.");
        }
        
        public void FinishRoomBoundaries()
        {
            if (CurrentState != MappingState.DefiningBoundaries || _localBoundaryPoints.Count < _config.MinCornersRequired) return;
            CurrentState = MappingState.SettingObstacleHeight;
            EventBus<ObstacleMappingStartedEvent>.Publish(new ObstacleMappingStartedEvent());
        }
 
        private void SetObstacleHeight()
        {
            if (s_hits.Count == 0) return;
            _currentObstacleHeight = s_hits[0].pose.position.y - _masterOriginAnchor.transform.position.y;
            CurrentState = MappingState.DefiningObstacleBoundaries;
            EventBus<ObstacleHeightSetEvent>.Publish(new ObstacleHeightSetEvent(s_hits[0].pose.position));
            Debug.Log($"[ManualRoomMappingService] Obstacle Height set to: {_currentObstacleHeight}m");
        }
 
        private void AddObstacleBoundaryPoint()
        {
            if (s_hits.Count == 0) return;
            Vector3 rawPos = s_hits[0].pose.position;
            Vector3 floorPoint = new Vector3(rawPos.x, _masterOriginAnchor.transform.position.y, rawPos.z);
            Vector3 localPoint = _masterOriginAnchor.transform.InverseTransformPoint(floorPoint);
            _localObstaclePoints.Add(localPoint);
            _worldObstaclePoints.Add(floorPoint);
            
            EventBus<ObstaclePointAddedEvent>.Publish(new ObstaclePointAddedEvent(floorPoint, _masterOriginAnchor.transform));
        }
 
        public void FinishCurrentObstacle()
        {
            if (CurrentState != MappingState.DefiningObstacleBoundaries || _localObstaclePoints.Count < 3) return;
            
            _obstacles.Add(new ObstacleData { Height = _currentObstacleHeight, FootprintCorners = _localObstaclePoints.ToArray() });
            _localObstaclePoints.Clear();
            _worldObstaclePoints.Clear();
            CurrentState = MappingState.SettingObstacleHeight; // Loop back to allow another obstacle
            EventBus<ObstacleMappingStartedEvent>.Publish(new ObstacleMappingStartedEvent());
        }

        public void UndoLastAction()
        {
            if (!CanUndo) return;
            
            if (CurrentState == MappingState.DefiningBoundaries)
            {
                if (_localBoundaryPoints.Count > 0)
                {
                    Vector3 removedWorld = _worldBoundaryPoints[_worldBoundaryPoints.Count - 1];
                    _localBoundaryPoints.RemoveAt(_localBoundaryPoints.Count - 1);
                    _worldBoundaryPoints.RemoveAt(_worldBoundaryPoints.Count - 1);
                    EventBus<BoundaryPointRemovedEvent>.Publish(new BoundaryPointRemovedEvent(removedWorld));
                }
                else
                {
                    CurrentState = MappingState.SettingCeilingHeight;
                    EventBus<CeilingHeightUndoneEvent>.Publish(new CeilingHeightUndoneEvent());
                }
            }
            else if (CurrentState == MappingState.SettingCeilingHeight)
            {
                // FIX: Publish event FIRST so pooled visuals can unparent themselves before the anchor is nuked!
                EventBus<FloorOriginUndoneEvent>.Publish(new FloorOriginUndoneEvent());
                if (_masterOriginAnchor != null) Object.Destroy(_masterOriginAnchor.gameObject);
                CurrentState = MappingState.SettingFloorOrigin;
            }
            else if (CurrentState == MappingState.DefiningObstacleBoundaries)
            {
                if (_localObstaclePoints.Count > 0)
                {
                    Vector3 removedWorld = _worldObstaclePoints[_worldObstaclePoints.Count - 1];
                    _localObstaclePoints.RemoveAt(_localObstaclePoints.Count - 1);
                    _worldObstaclePoints.RemoveAt(_worldObstaclePoints.Count - 1);
                    EventBus<ObstaclePointRemovedEvent>.Publish(new ObstaclePointRemovedEvent(removedWorld));
                }
                else
                {
                    CurrentState = MappingState.SettingObstacleHeight;
                    EventBus<ObstacleHeightUndoneEvent>.Publish(new ObstacleHeightUndoneEvent());
                }
            }
            else if (CurrentState == MappingState.SettingObstacleHeight)
            {
                if (_obstacles.Count > 0)
                {
                    var lastObs = _obstacles[_obstacles.Count - 1];
                    _obstacles.RemoveAt(_obstacles.Count - 1);
                    _localObstaclePoints.AddRange(lastObs.FootprintCorners);
                    foreach (var localPt in lastObs.FootprintCorners) _worldObstaclePoints.Add(_masterOriginAnchor.transform.TransformPoint(localPt));
                    _currentObstacleHeight = lastObs.Height;
                    CurrentState = MappingState.DefiningObstacleBoundaries;
                    EventBus<ObstacleRestoredEvent>.Publish(new ObstacleRestoredEvent());
                }
                else 
                {
                    CurrentState = MappingState.DefiningBoundaries; // Go back to room mapping
                    EventBus<RoomBoundariesRestoredEvent>.Publish(new RoomBoundariesRestoredEvent());
                }
            }
        }

        public void CompleteMapping()
        {
            if (_localBoundaryPoints.Count < _config.MinCornersRequired)
            {
                Debug.LogWarning($"[ManualRoomMappingService] Cannot complete. Need at least {_config.MinCornersRequired} corners.");
                return;
            }

            CurrentState = MappingState.Complete;
            
            // Hide the reticle and stop scanning for new planes to lock the environment
            _reticleInstance.Hide();
            _planeManager.enabled = false;
            
            EventBus<PlayableAreaDefinedEvent>.Publish(new PlayableAreaDefinedEvent(_masterOriginAnchor.transform, new List<Vector3>(_worldBoundaryPoints), _ceilingHeight, _obstacles));
            
            // Calculate Area and Volume using the Shoelace formula on the 2D local plane
            float area = 0f;
            int j = _localBoundaryPoints.Count - 1;
            for (int i = 0; i < _localBoundaryPoints.Count; i++)
            {
                area += (_localBoundaryPoints[j].x + _localBoundaryPoints[i].x) * (_localBoundaryPoints[j].z - _localBoundaryPoints[i].z);
                j = i;
            }
            area = Mathf.Abs(area / 2f);
            float volume = area * _ceilingHeight;

            Debug.Log($"[ManualRoomMappingService] Shoot House Mapping complete! Floor Area: {area:F2} sq m | Room Volume: {volume:F2} cubic m");
        }
        
        public void OnDispose()
        {
            if (_reticleInstance != null) Object.Destroy(_reticleInstance.gameObject);
        }
    }
}