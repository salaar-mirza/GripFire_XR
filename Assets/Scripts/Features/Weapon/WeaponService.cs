using System.Collections.Generic;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.PlayerInput.Events;
using ARFps.Features.Weapon.Events;
using ARFps.Features.Sandbox;
using ARFps.Core.State;
using ARFps.Core.Audio.Events;
using ARFps.Core.State.Events;
using UnityEngine;
using UnityEngine.Pool;

namespace ARFps.Features.Weapon
{
    /// <summary>
    /// Manages weapon firing logic, bullet pooling, and raycast hit detection.
    /// </summary>
    public class WeaponService : IService, ITickable
    {
        private readonly WeaponConfig _config;
        private readonly WeaponView _view;
        
        // Firing State
        private bool _isFiring;
        private float _fireTimer;
        private readonly float _fireInterval;
        private bool _isCombatActive;
        private GameState _currentState;

        private ObjectPool<BulletView> _bulletPool;
        private readonly List<ActiveBullet> _activeBullets = new List<ActiveBullet>();
        
        private SandboxWeaponMode _sandboxMode = Features.Sandbox.SandboxWeaponMode.Bullets;

        // Use a struct for active bullets to prevent heap allocations during rapid fire
        private struct ActiveBullet
        {
            public BulletView View;
            public Vector3 Position;
            public Vector3 Direction;
            public float DistanceTraveled;
        }

        public WeaponService(WeaponConfig config, WeaponView view)
        {
            _config = config;
            _view = view;
            _fireInterval = 60f / _config.FireRateRPM; // Convert RPM to seconds
        }

        public void OnInit()
        {
            // Initialize the Object Pool
            _bulletPool = new ObjectPool<BulletView>(
                createFunc: () => Object.Instantiate(_config.BulletPrefab),
                actionOnGet: (b) => b.gameObject.SetActive(true),
                actionOnRelease: (b) =>
                {
                    if (b != null && b.Trail != null) b.Trail.Clear(); // Clear BEFORE deactivating
                    if (b != null) b.gameObject.SetActive(false);
                },
                actionOnDestroy: (b) => { if (b != null) Object.Destroy(b.gameObject); },
                collectionCheck: false,
                defaultCapacity: _config.InitialPoolSize,
                maxSize: _config.MaxPoolSize
            );

            // Pre-warm the pool to prevent lag spikes on the first shot
            var preWarmList = new List<BulletView>();
            for (int i = 0; i < _config.InitialPoolSize; i++) preWarmList.Add(_bulletPool.Get());
            foreach (var b in preWarmList) _bulletPool.Release(b);

            // Subscribe to decoupled input and state events
            EventBus<PrimaryFireStartedEvent>.Subscribe(OnFireStarted);
            EventBus<PrimaryFireEndedEvent>.Subscribe(OnFireEnded);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<SandboxWeaponModeChangedEvent>.Subscribe(OnSandboxWeaponModeChanged);
        }

        private void OnFireStarted(PrimaryFireStartedEvent e) => _isFiring = true;
        
        private void OnFireEnded(PrimaryFireEndedEvent e) => _isFiring = false;
        
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            _currentState = e.CurrentState;
            UpdateWeaponSafety();
            
            if (_currentState == GameState.ModeSelection)
            {
                // Clean up leftover bullets currently flying through the air
                for (int i = _activeBullets.Count - 1; i >= 0; i--)
                {
                    _bulletPool.Release(_activeBullets[i].View);
                }
                _activeBullets.Clear();
            }
        }
        
        private void OnSandboxWeaponModeChanged(SandboxWeaponModeChangedEvent e)
        {
            _sandboxMode = e.Mode;
            UpdateWeaponSafety();
        }
        
        private void UpdateWeaponSafety()
        {
            bool isPlaying = _currentState == GameState.Playing;
            bool isSandboxBullets = _currentState == GameState.Sandbox && _sandboxMode == SandboxWeaponMode.Bullets;
            
            // Enable weapon if we are in normal combat, or if we are in Sandbox mode and "Bullets" are selected
            _isCombatActive = isPlaying || isSandboxBullets;
            
            // If safety is engaged while pulling the trigger, force it to stop firing
            if (!_isCombatActive) _isFiring = false;
        }
        

        public void OnTick()
        {
            HandleFiring();
            UpdateActiveBullets();
        }

        private void HandleFiring()
        {
            if (!_isCombatActive) return; // Safety is ON! Don't fire.

            if (_isFiring)
            {
                _fireTimer += Time.deltaTime;
                while (_fireTimer >= _fireInterval)
                {
                    FireBullet();
                    _fireTimer -= _fireInterval;
                }
            }
            else
            {
                _fireTimer = _fireInterval; // Reset so the first shot is immediate
            }
        }

        private void FireBullet()
        {
            if (_view.BarrelPoint == null)
            {
                Debug.LogError("[WeaponService] BARREL POINT IS MISSING! The gun cannot fire. Please assign it in the Inspector.");
                return;
            }

            // Grab a bullet from the pool instead of Instantiating!
            BulletView bulletView = _bulletPool.Get();
            bulletView.transform.position = _view.BarrelPoint.position;
            
            // True FPS Aiming (Barrel-to-Crosshair Convergence)
            Transform camTransform = Camera.main.transform;
            Vector3 targetPoint;
            if (Physics.Raycast(camTransform.position, camTransform.forward, out RaycastHit aimHit, _config.MaxDistance))
            {
                targetPoint = aimHit.point;
            }
            else
            {
                // If they are looking at the sky, just pick a point super far away in the center of the screen
                targetPoint = camTransform.position + camTransform.forward * _config.MaxDistance;
            }
            
            // Shoot the bullet from the lower Barrel Point towards the camera's target point
            Vector3 shootDirection = (targetPoint - _view.BarrelPoint.position).normalized;
            bulletView.transform.rotation = Quaternion.LookRotation(shootDirection);

            _activeBullets.Add(new ActiveBullet
            {
                View = bulletView,
                Position = _view.BarrelPoint.position,
                Direction = shootDirection,
                DistanceTraveled = 0f
            });

            // Broadcast that we fired so Audio/VFX can react later
            EventBus<WeaponFiredEvent>.Publish(new WeaponFiredEvent(_view.BarrelPoint.position, shootDirection));
            EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BulletFire, _view.BarrelPoint.position));
        }

        private void UpdateActiveBullets()
        {
            float step = _config.BulletSpeed * Time.deltaTime;

            // Loop backwards so we can safely remove bullets from the list while iterating
            for (int i = _activeBullets.Count - 1; i >= 0; i--)
            {
                ActiveBullet bullet = _activeBullets[i];

                // Raycast forward to check for hits (much faster than Rigidbodies for fast bullets)
                if (Physics.Raycast(bullet.Position, bullet.Direction, out RaycastHit hit, step))
                {
                    // Broadcast the hit so any listening system can react
                    EventBus<EntityHitEvent>.Publish(new EntityHitEvent(hit.collider, _config.Damage));

                    _bulletPool.Release(bullet.View);
                    _activeBullets.RemoveAt(i);
                }
                else
                {
                    // Move the bullet forward
                    bullet.Position += bullet.Direction * step;
                    bullet.View.transform.position = bullet.Position;
                    bullet.DistanceTraveled += step;
                    
                    // Write the updated struct back to the list!
                    _activeBullets[i] = bullet;

                    if (bullet.DistanceTraveled >= _config.MaxDistance)
                    {
                        _bulletPool.Release(bullet.View);
                        _activeBullets.RemoveAt(i);
                    }
                }
            }
        }

        public void OnDispose()
        {
            EventBus<PrimaryFireStartedEvent>.Unsubscribe(OnFireStarted);
            EventBus<PrimaryFireEndedEvent>.Unsubscribe(OnFireEnded);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<SandboxWeaponModeChangedEvent>.Unsubscribe(OnSandboxWeaponModeChanged);
            
            // Cleanly destroy active bullets in the air to prevent memory leaks
            foreach (var bullet in _activeBullets)
            {
                if (bullet.View != null && bullet.View.gameObject != null) Object.Destroy(bullet.View.gameObject);
            }
            _activeBullets.Clear();
            
            _bulletPool?.Clear();
        }
    }
}