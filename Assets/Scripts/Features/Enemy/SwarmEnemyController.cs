using ARFps.Core.Events;
using ARFps.Features.Enemy.Events;
using ARFps.Features.Player.Events;
using UnityEngine;

namespace ARFps.Features.Enemy
{
    public enum SwarmState { Spawning, Chasing, Attacking, Dead }

    /// <summary>
    /// The AI controller for a single Swarm Enemy.
    /// Manages its micro-state machine and throttles tick updates for performance optimization.
    /// </summary>
    public class SwarmEnemyController
    {
        private readonly SwarmConfig _config;
        private readonly SwarmEnemyView _view;
        private readonly Transform _playerTransform;
        private readonly Material _originalMaterial;

        public SwarmEnemyView View => _view;

        public SwarmState CurrentState { get; private set; }
        private int _currentHealth;

        // Tick Throttling Variables for Performance!
        private float _pathfindingTimer;
        private const float PathfindingInterval = 0.5f; // Recalculate A* path only twice per second

        private float _hitFlashTimer;
        private float _attackTimer;

        public SwarmEnemyController(SwarmConfig config, SwarmEnemyView view, Transform playerTransform)
        {
            _config = config;
            _view = view;
            _playerTransform = playerTransform;
            
            if (_view.EnemyRenderer != null) _originalMaterial = _view.EnemyRenderer.sharedMaterial;
            Reset();
        }

        public void Reset()
        {
            _currentHealth = _config.MaxHealth;
            if (_view.Agent != null) _view.Agent.speed = _config.SwarmSpeed;
            if (_view.Agent != null) _view.Agent.radius = _config.SwarmRadius;
            if (_view.Agent != null) _view.Agent.stoppingDistance = 0.5f; // Prevents them from crowding the exact same pixel
            CurrentState = SwarmState.Chasing;
            if (_view.EnemyRenderer != null) _view.EnemyRenderer.sharedMaterial = _originalMaterial;
            _pathfindingTimer = 0f;
            _hitFlashTimer = 0f;
            _attackTimer = 0f;
        }

        public void OnTick()
        {
            if (CurrentState == SwarmState.Dead) return;
            // 1. Handle Visuals
            if (_hitFlashTimer > 0)
            {
                _hitFlashTimer -= Time.deltaTime;
                if (_hitFlashTimer <= 0 && _view.EnemyRenderer != null) _view.EnemyRenderer.sharedMaterial = _originalMaterial;
            }

            // 2. Handle AI Pathfinding (Throttled for Performance!)
            if (CurrentState == SwarmState.Chasing)
            {
                // Check Distance using highly optimized sqrMagnitude!
                // If within 0.5 meters (0.5 * 0.5 = 0.25), attack!
                Vector3 distanceToPlayer = _playerTransform.position - _view.transform.position;
                // We check X and Z distance, ignoring Y so height differences don't break the attack range
                distanceToPlayer.y = 0; 
                
                if (distanceToPlayer.sqrMagnitude < 0.25f)
                {
                    CurrentState = SwarmState.Attacking;
                    _attackTimer = _config.AttackRateSeconds; // Force an immediate bite
                    return;
                }

                _pathfindingTimer += Time.deltaTime;
                if (_pathfindingTimer >= PathfindingInterval)
                {
                    _pathfindingTimer = 0f;
                    if (_view.Agent.isOnNavMesh && _playerTransform != null)
                    {
                        // Flatten the target position to the floor so the Agent doesn't try to fly!
                        // Add a tiny bit of random noise so the swarm spreads out and surrounds the player naturally!
                        Vector3 randomNoise = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                         
                        Vector3 targetPos = _playerTransform.position + randomNoise;

                        targetPos.y = _view.transform.position.y; // Keep it on the Agent's current floor level
                        _view.Agent.SetDestination(targetPos);
                    }
                }
            }
            else if (CurrentState == SwarmState.Attacking)
            {
                // 3. Handle Attacking & Range Checking
                Vector3 distanceToPlayer = _playerTransform.position - _view.transform.position;
                distanceToPlayer.y = 0;
                 
                // If the player walked away, go back to chasing!
                if (distanceToPlayer.sqrMagnitude > 0.3f) 
                {
                    CurrentState = SwarmState.Chasing;
                    return;
                }
 
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= _config.AttackRateSeconds)
                {
                    _attackTimer = 0f;
                    EventBus<PlayerDamagedEvent>.Publish(new PlayerDamagedEvent(_config.AttackDamage));
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (CurrentState == SwarmState.Dead) return;
            _currentHealth -= amount;
            if (_view.EnemyRenderer != null && _config.HitFlashMaterial != null) _view.EnemyRenderer.sharedMaterial = _config.HitFlashMaterial;
            _hitFlashTimer = _config.HitFlashDuration;

            if (_currentHealth <= 0)
            {
                CurrentState = SwarmState.Dead;
                
                // Clean up state: Reset the material BEFORE it goes back to the Object Pool to prevent dirty state
                if (_view.EnemyRenderer != null) _view.EnemyRenderer.sharedMaterial = _originalMaterial;
                
                EventBus<SwarmEnemyDestroyedEvent>.Publish(new SwarmEnemyDestroyedEvent(_view));
            }
        }
    }
}