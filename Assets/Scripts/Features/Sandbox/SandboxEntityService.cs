using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.AI;
using ARFps.Core.Services;
using ARFps.Core.Events;
using ARFps.Features.SwarmPathfinding.Events;
using ARFps.Features.RoomMapping.Events;
using ARFps.Core.State;
using ARFps.Core.State.Events;


namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// Manages the lifecycles and object pools of dynamic sandbox entities.
    /// </summary>
    public class SandboxEntityService : IService, ITickable
    {
        private readonly SandboxConfig _config;
        
        private ObjectPool<SandboxFanController> _fanPool;
        private readonly List<SandboxFanController> _activeFans = new List<SandboxFanController>();

           
        private float _ceilingHeight;
        private float _minX, _maxX, _minZ, _maxZ;
        private Transform _masterOrigin;
        
        private int _pendingCeilingFans = 0;
        private float _ceilingFanSpawnTimer = 0f;
        
        private bool _hasSpawnedSandboxEntities = false;
        
        public SandboxEntityService(SandboxConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            _fanPool = new ObjectPool<SandboxFanController>(
                createFunc: () => 
                {
                    var view = Object.Instantiate(_config.FanPrefab).GetComponent<SandboxFanView>();
                    view.Agent.enabled = false; // FIX: Prevent NavMesh errors during Boot Sequence pre-warming
                    return new SandboxFanController(_config, view);
                },
                actionOnGet: null,
                actionOnRelease: controller => controller.View.gameObject.SetActive(false),
                actionOnDestroy: controller => Object.Destroy(controller.View.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.NumberOfFloorFans + _config.NumberOfCelingFans,
                maxSize: 20
            );

            // RULE 3: Hoard & Return Pre-warm
            var preWarmFans = new List<SandboxFanController>();
            for (int i = 0; i < _config.NumberOfFloorFans + _config.NumberOfCelingFans; i++) preWarmFans.Add(_fanPool.Get());
            foreach (var f in preWarmFans) _fanPool.Release(f);

            EventBus<PlayableAreaDefinedEvent>.Subscribe(OnPlayableAreaDefined);
            EventBus<NavMeshBakedEvent>.Subscribe(OnNavMeshBaked);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }
        
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.CurrentState == GameState.Sandbox && !_hasSpawnedSandboxEntities)
            {
                SpawnSandboxEntities();
                _hasSpawnedSandboxEntities = true;
            }
            else if (e.CurrentState == GameState.ModeSelection)
            {
                // Return all fans to the pool so the room is completely empty!
                for (int i = _activeFans.Count - 1; i >= 0; i--)
                {
                    _fanPool.Release(_activeFans[i]);
                }
                _activeFans.Clear();
                _hasSpawnedSandboxEntities = false; // Allow them to spawn again next time!
                _pendingCeilingFans = 0;
            }
        }

        

        private void OnPlayableAreaDefined(PlayableAreaDefinedEvent e)
        {
            _masterOrigin = e.MasterOrigin;
            _ceilingHeight = e.CeilingHeight;
            _minX = float.MaxValue; _minZ = float.MaxValue;
            _maxX = float.MinValue; _maxZ = float.MinValue;
            
            // Calculate the exact bounds in LOCAL space to perfectly prevent AR World Offset bugs
            foreach (var pt in e.BoundaryPoints)
            {
                Vector3 localPt = _masterOrigin != null ? _masterOrigin.InverseTransformPoint(pt) : pt;
                if (localPt.x < _minX) _minX = localPt.x;
                if (localPt.x > _maxX) _maxX = localPt.x;
                if (localPt.z < _minZ) _minZ = localPt.z;
                if (localPt.z > _maxZ) _maxZ = localPt.z;
            }
        }

        private void OnNavMeshBaked(NavMeshBakedEvent e)
        {
            // The room is ready! Hand control to the Mode Selector UI.
            GameService.Get<GameStateService>().ChangeState(GameState.ModeSelection);
        }
 
        private void SpawnSandboxEntities()
        {
            // FIX: Vector3.zero is often wrong in AR. We use the player's camera to find a guaranteed valid floor spot!
            Vector3 searchPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            
            for (int i = 0; i < _config.NumberOfFloorFans; i++)
            {
                var fan = _fanPool.Get();
                fan.View.gameObject.SetActive(true);
                
                // Ask the NavMesh for the closest valid point to the player, searching up to 10 meters away
                if (UnityEngine.AI.NavMesh.SamplePosition(searchPos, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    fan.Reset(hit.position, SandboxFanController.FanMovementType.FloorRoam);
                }
                else
                {
                    fan.Reset(searchPos, SandboxFanController.FanMovementType.FloorRoam); // Fallback
                }
                _activeFans.Add(fan);
            }
            
            // Queue the Ceiling Fans to launch sequentially!
            _pendingCeilingFans = _config.NumberOfCelingFans;
            _ceilingFanSpawnTimer = 0f; // Launch the first one immediately
        }

        public void OnTick()
        {
            foreach (var fan in _activeFans) fan.Tick(Time.deltaTime);
            
            // Launch pending drones one by one
            if (_pendingCeilingFans > 0)
            {
                _ceilingFanSpawnTimer -= Time.deltaTime;
                if (_ceilingFanSpawnTimer <= 0f)
                {
                    SpawnAscendingCeilingFan();
                    _pendingCeilingFans--;
                    _ceilingFanSpawnTimer = 1.0f; // Wait 1 second before launching the next drone
                }
            }
        }
        
        private void SpawnAscendingCeilingFan()
        {
            var fan = _fanPool.Get();
            fan.View.gameObject.SetActive(true);
            
            // Spawn safely at the physical origin (where the player started scanning)
            Vector3 spawnPos = _masterOrigin != null ? _masterOrigin.position : Vector3.zero;
            float targetY = (_masterOrigin != null ? _masterOrigin.position.y : 0f) + _ceilingHeight - 0.05f;
            
            fan.Reset(spawnPos, SandboxFanController.FanMovementType.CeilingAscend, targetY);
            if (_masterOrigin != null) fan.View.transform.SetParent(_masterOrigin, true);
            _activeFans.Add(fan);
        }

        public void OnDispose()
        {
            EventBus<PlayableAreaDefinedEvent>.Unsubscribe(OnPlayableAreaDefined);
            EventBus<NavMeshBakedEvent>.Unsubscribe(OnNavMeshBaked);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            
            foreach (var fan in _activeFans)
            {
                if (fan?.View?.gameObject != null) Object.Destroy(fan.View.gameObject);
            }
            _activeFans.Clear();
            _fanPool?.Clear();
        }
    }
}