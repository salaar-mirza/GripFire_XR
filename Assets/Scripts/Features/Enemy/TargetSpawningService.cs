using System.Collections.Generic;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using ARFps.Features.Enemy.Events;
using ARFps.Features.RoomMapping.Events;
using ARFps.Features.SwarmPathfinding.Events;
using ARFps.Features.Weapon.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace ARFps.Features.Enemy
{
    /// <summary>
    /// The Manager for the Swarm System. Handles Ant Hills and Swarm Enemies.
    /// </summary>
    public class TargetSpawningService : IService, ITickable
    {
        private readonly SwarmConfig _config;
        private GameStateService _gameStateService;
        
        private ObjectPool<GameObject> _antHillPool;
        private ObjectPool<SwarmEnemyController> _swarmEnemyPool;
        
        // Room Bounds
        private Vector3[] _floorVertices;
        private int[] _floorTriangles;
        
        private bool _isNavMeshReady = false;

        // Active Spawners
        private readonly List<GameObject> _activeAntHills = new List<GameObject>();
        
        // Swarm Management
        // The O(1) Dictionary to instantly find the Brain from a hit Collider!
        private readonly Dictionary<Collider, SwarmEnemyController> _enemyLookup = new Dictionary<Collider, SwarmEnemyController>();
        private readonly List<SwarmEnemyController> _activeEnemies = new List<SwarmEnemyController>();
        private int _enemiesToSpawnThisWave = 0;
        private float _spawnTimer = 0f;
        
        // Player Proxy for AI Chasing
        private Transform _playerTransform;
        private Transform _masterOrigin;

        public TargetSpawningService(SwarmConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            _gameStateService = GameService.Get<GameStateService>();
            if (Camera.main != null) _playerTransform = Camera.main.transform;
            
            _antHillPool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(_config.AntHillPrefab),
                actionOnGet: obj => obj.SetActive(true),
                actionOnRelease: obj => obj.SetActive(false),
                actionOnDestroy: Object.Destroy,
                collectionCheck: false,
                defaultCapacity: 5,
                maxSize: 10
            );
            
            _swarmEnemyPool = new ObjectPool<SwarmEnemyController>(
                createFunc: () => {
                    var view = Object.Instantiate(_config.SwarmEnemyPrefab).GetComponent<SwarmEnemyView>();
                    return new SwarmEnemyController(_config, view, _playerTransform);
                },
                actionOnGet: null,
                actionOnRelease: controller => controller.View.gameObject.SetActive(false),
                actionOnDestroy: controller => Object.Destroy(controller.View.gameObject),
                collectionCheck: false,
                defaultCapacity: 20,
                maxSize: 50
            );

            // Pre-warm the pools to prevent mid-game GC spikes (The Hoard & Return method)
            var preWarmEnemies = new List<SwarmEnemyController>();
            for (int i = 0; i < 20; i++) preWarmEnemies.Add(_swarmEnemyPool.Get());
            foreach (var e in preWarmEnemies) _swarmEnemyPool.Release(e);

            var preWarmHills = new List<GameObject>();
            for (int i = 0; i < 5; i++) preWarmHills.Add(_antHillPool.Get());
            foreach (var h in preWarmHills) _antHillPool.Release(h);
            
            EventBus<FloorMathCalculatedEvent>.Subscribe(OnFloorMathCalculated);
            EventBus<NavMeshBakedEvent>.Subscribe(OnNavMeshBaked);
            EventBus<SwarmEnemyDestroyedEvent>.Subscribe(OnEnemyDestroyed);
            EventBus<EntityHitEvent>.Subscribe(OnEntityHit);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            
            Debug.Log("[TargetSpawningService] Initialized. Awaiting Room Data.");
        }

         
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            // If we return to the menu, cleanly wipe out the combat arena!
            if (e.CurrentState == GameState.ModeSelection)
            {
                for (int i = _activeEnemies.Count - 1; i >= 0; i--)
                {
                    _swarmEnemyPool.Release(_activeEnemies[i]);
                }
                _activeEnemies.Clear();
                
                for (int i = _activeAntHills.Count - 1; i >= 0; i--)
                {
                    _antHillPool.Release(_activeAntHills[i]);
                }
                _activeAntHills.Clear();
            }
        }
        public void OnTick()
        {
            // Only execute game logic if we are actively playing
            if (_gameStateService.CurrentState != GameState.Playing) return;
            
            // Ensure we have at least 1 Ant Hill spawned when the game starts!
            if (_activeAntHills.Count == 0 && _isNavMeshReady)
            {
                SpawnAntHill();
            }
            
            // Tick all active enemy AI Brains!
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                _activeEnemies[i].OnTick();
            }

            // Handle Wave Spawning over time
            if (_enemiesToSpawnThisWave > 0 && _activeAntHills.Count > 0)
            {
                _spawnTimer += Time.deltaTime;
                // Framerate-independent catch-up loop to ensure accurate spawn rates
                while (_spawnTimer >= _config.SpawnIntervalSeconds && _enemiesToSpawnThisWave > 0)
                {
                    SpawnSwarmEnemy();
                    _spawnTimer -= _config.SpawnIntervalSeconds;
                    _enemiesToSpawnThisWave--;
                }
            }
        }

        private void SpawnAntHill()
        {
            if (!_isNavMeshReady || _floorTriangles == null || _floorTriangles.Length == 0) return;

            // Try up to 5 times instantly to find a safe point that isn't inside a furniture hole
            for (int i = 0; i < 5; i++)
            {
                // 1. Pick a random triangle from the procedural floor mesh
                int triangleCount = _floorTriangles.Length / 3;
                int randomIndex = Random.Range(0, triangleCount) * 3;
             
                Vector3 a = _floorVertices[_floorTriangles[randomIndex]];
                Vector3 b = _floorVertices[_floorTriangles[randomIndex + 1]];
                Vector3 c = _floorVertices[_floorTriangles[randomIndex + 2]];
 
                // 2. Barycentric Math to pick a guaranteed point INSIDE that triangle
                float r1 = Random.value;
                float r2 = Random.value;
                if (r1 + r2 > 1f)
                {
                    r1 = 1f - r1;
                    r2 = 1f - r2;
                }
 
                Vector3 localRandomPos = a + r1 * (b - a) + r2 * (c - a);
             
                // 3. Convert to World Space
                Vector3 worldRandomPos = _masterOrigin != null ? _masterOrigin.TransformPoint(localRandomPos) : localRandomPos;
 
                // Use Unity's AI API to find the closest mathematically walkable point on the NavMesh
                // Use 5.0f radius here so we can safely slide off couches!
                if (NavMesh.SamplePosition(worldRandomPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    var antHill = _antHillPool.Get();
                         
                    float yOffset = 0f;
                    if (antHill.TryGetComponent<Collider>(out var collider)) yOffset = collider.bounds.extents.y;
                    else if (antHill.TryGetComponent<Renderer>(out var renderer)) yOffset = renderer.bounds.extents.y;
     
                    antHill.transform.position = hit.position + new Vector3(0, yOffset, 0);

                    // Parent the Ant Hill to the AR Anchor so it never drifts away from the room!
                    if (_masterOrigin != null) antHill.transform.SetParent(_masterOrigin, true);
                    
                    _activeAntHills.Add(antHill);
                    Debug.Log($"[TargetSpawningService] Ant Hill Spawned safely at {hit.position}!");
                    return; // Success! Exit the method so we don't spawn 5 ant hills or log warnings.
                }
            }
            
            // If all 5 attempts failed (very rare), THEN log a single warning to prevent console spam
            Debug.LogWarning("[TargetSpawningService] Failed to find a safe NavMesh spawn point this frame. Retrying next frame.");
        }
        
        public void StartWave(int enemyCount)
        {
            _enemiesToSpawnThisWave = enemyCount;
            _spawnTimer = _config.SpawnIntervalSeconds; // Force immediate first spawn
            Debug.Log($"[TargetSpawningService] Spawning {enemyCount} enemies this wave.");
        }

        private void SpawnSwarmEnemy()
        {
            // Pick a random Ant Hill to erupt from
            Transform randomAntHill = _activeAntHills[Random.Range(0, _activeAntHills.Count)].transform;
            
            var controller = _swarmEnemyPool.Get();
            var enemyView = controller.View;
            
            // 1. Move the raw transform FIRST while it is still asleep
            Vector3 searchPos = randomAntHill.position;
            searchPos.y = _masterOrigin != null ? _masterOrigin.position.y : 0f; // Drop search to true floor
             
            Vector3 finalSpawnPos = searchPos;
            
            // Find the perfect NavMesh ground directly beneath the floating Ant Hill so the Agent doesn't crash!
            if (NavMesh.SamplePosition(searchPos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                finalSpawnPos = hit.position;
            }
            
            enemyView.transform.position = finalSpawnPos;

            // Parent the Enemy to the AR Anchor so it stays perfectly synced with the physical room
            if (_masterOrigin != null) enemyView.transform.SetParent(_masterOrigin, true);
            
            // 2. WAKE IT UP NOW that it is safely touching the NavMesh!
            enemyView.gameObject.SetActive(true);
             
            // 3. Force the internal C++ Agent to mathematically snap to the grid
            if (enemyView.Agent.isActiveAndEnabled) enemyView.Agent.Warp(finalSpawnPos);
            
            // Reset the recycled controller state instead of allocating a new instance
            controller.Reset();
            if (!_activeEnemies.Contains(controller)) _activeEnemies.Add(controller);
            
            // Map the Collider to the Brain for instant damage routing!
            if (enemyView.EnemyCollider != null && !_enemyLookup.ContainsKey(enemyView.EnemyCollider)) _enemyLookup.Add(enemyView.EnemyCollider, controller);
        }
 
        private void OnEntityHit(EntityHitEvent e)
        {
            if (_enemyLookup.TryGetValue(e.HitCollider, out var controller)) controller.TakeDamage(e.Damage);
        }
        
        private void OnEnemyDestroyed(SwarmEnemyDestroyedEvent e)
        {
            if (e.View.EnemyCollider != null && _enemyLookup.TryGetValue(e.View.EnemyCollider, out var controller))
            {
                _swarmEnemyPool.Release(controller);
                _activeEnemies.Remove(controller);
                _enemyLookup.Remove(e.View.EnemyCollider);
            }
        }

        private void OnFloorMathCalculated(FloorMathCalculatedEvent e)
        {
            // Cache the raw geometry for our Barycentric spawn math!
            _masterOrigin = e.MasterOrigin;
            _floorVertices = e.Vertices;
            _floorTriangles = e.Triangles;
        }

        private void OnNavMeshBaked(NavMeshBakedEvent e)
        {
            // NOW it is safe to drop the Ant Hills!
            _isNavMeshReady = true;
        }

        public void OnDispose()
        {
            EventBus<FloorMathCalculatedEvent>.Unsubscribe(OnFloorMathCalculated);
            EventBus<NavMeshBakedEvent>.Unsubscribe(OnNavMeshBaked);
            EventBus<SwarmEnemyDestroyedEvent>.Unsubscribe(OnEnemyDestroyed);
            EventBus<EntityHitEvent>.Unsubscribe(OnEntityHit);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);

            // Cleanly destroy active entities to prevent memory leaks before clearing pools
            foreach (var antHill in _activeAntHills)
            {
                if (antHill != null) Object.Destroy(antHill);
            }
            _activeAntHills.Clear();
 
            foreach (var enemy in _activeEnemies)
            {
                if (enemy?.View?.gameObject != null) Object.Destroy(enemy.View.gameObject);
            }
            _activeEnemies.Clear();
            _enemyLookup.Clear();

            _antHillPool?.Clear();
            _swarmEnemyPool?.Clear();
        }
    }
}