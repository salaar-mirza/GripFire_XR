using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using ARFps.Core.Services;
using ARFps.Core.Events;
using ARFps.Features.PlayerInput.Events;
using ARFps.Features.Weapon;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using ARFps.Core.Audio.Events;
using ARFps.Core.Vfx.Events;
using ARFps.Features.Weapon.Events;


namespace ARFps.Features.Sandbox
{
    public class SandboxWeaponService : IService, ITickable
    {
        private readonly SandboxConfig _config;
        private readonly WeaponView _weaponView;

        private ObjectPool<SandboxBallController> _ballPool;
        private readonly List<SandboxBallController> _activeBalls = new List<SandboxBallController>();

        private ObjectPool<SandboxBalloonController> _balloonPool;
        private readonly List<SandboxBalloonController> _activeBalloons = new List<SandboxBalloonController>();
         
        private ObjectPool<SandboxSmokeController> _smokePool;
        private readonly List<SandboxSmokeController> _activeSmokes = new List<SandboxSmokeController>();
        
        private SandboxWeaponMode _currentMode = SandboxWeaponMode.Bullets;
        private GameState _currentState;
        
        // Laser / Tractor Beam State
        private SandboxLaserMode _laserMode = SandboxLaserMode.Destroy;
        private SandboxHoldAction _holdAction = SandboxHoldAction.Drop;
        private object _heldObject; // Can be SandboxBallController OR SandboxBalloonController
        
        // Visual Laser
        private LineRenderer _laserVisual;

        // Continuous Firing State
        private bool _isFiring = false;
        private float _fireTimer = 0f;
        private const float FireInterval = 0.15f; // Shoots a ball every 0.15 seconds!
        private float _blowCooldownTimer = 0f;
        private float _laserSparkTimer = 0f; // Throttle for wall sparks!


        public SandboxWeaponService(SandboxConfig config, WeaponView weaponView)
        {
            _config = config;
            _weaponView = weaponView;
        }

        public void OnInit()
        {
            _ballPool = new ObjectPool<SandboxBallController>(
                createFunc: () =>
                {
                    var view = Object.Instantiate(_config.BallPrefab).GetComponent<SandboxBallView>();
                    return new SandboxBallController(_config, view);
                },
                actionOnGet: null,
                actionOnRelease: c => c.View.gameObject.SetActive(false),
                actionOnDestroy: c => Object.Destroy(c.View.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.BallPoolSize,
                maxSize: 300
            );

            // RULE 3: Hoard & Return Pre-warm
            var preWarm = new List<SandboxBallController>();
            for (int i = 0; i < _config.BallPoolSize; i++) preWarm.Add(_ballPool.Get());
            foreach (var b in preWarm) _ballPool.Release(b);

            // Pre-warm the Balloons
            _balloonPool = new ObjectPool<SandboxBalloonController>(
                createFunc: () => {
                    var view = Object.Instantiate(_config.BalloonPrefab).GetComponent<SandboxBalloonView>();
                    return new SandboxBalloonController(_config, view);
                },
                actionOnGet: null,
                actionOnRelease: c => c.View.gameObject.SetActive(false),
                actionOnDestroy: c => Object.Destroy(c.View.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.BalloonPoolSize,
                maxSize: 150
            );
            var preWarmBalloons = new List<SandboxBalloonController>();
            for (int i = 0; i < _config.BalloonPoolSize; i++) preWarmBalloons.Add(_balloonPool.Get());
            foreach (var b in preWarmBalloons) _balloonPool.Release(b);
            
             
            // Pre-warm the Smoke Gun
            _smokePool = new ObjectPool<SandboxSmokeController>(
                createFunc: () => {
                    var view = Object.Instantiate(_config.SmokePrefab).GetComponent<SandboxSmokeView>();
                    return new SandboxSmokeController(_config, view);
                },
                actionOnGet: null,
                actionOnRelease: c => {
                    c.View.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    c.View.gameObject.SetActive(false);
                },
                actionOnDestroy: c => Object.Destroy(c.View.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.SmokePoolSize,
                maxSize: 100
            );
            var preWarmSmokes = new List<SandboxSmokeController>();
            for (int i = 0; i < _config.SmokePoolSize; i++) preWarmSmokes.Add(_smokePool.Get());
            foreach (var s in preWarmSmokes) _smokePool.Release(s);

            

            // Create the Visual Laser Beam dynamically to adhere to Rule 3 (No mid-game allocation)
            GameObject laserObj = new GameObject("SandboxLaserVisual");
            _laserVisual = laserObj.AddComponent<LineRenderer>();
            _laserVisual.positionCount = 2;
            _laserVisual.startWidth = 0.02f;
            _laserVisual.endWidth = 0.02f;
            _laserVisual.material = new Material(Shader.Find("Sprites/Default")); // Gives a solid, bright colored laser
            _laserVisual.enabled = false;

            EventBus<PrimaryFireStartedEvent>.Subscribe(OnFireStarted);
            
            // FIX: Removed anonymous lambdas to prevent memory leaks during OnDispose!
            EventBus<SandboxWeaponModeChangedEvent>.Subscribe(OnWeaponModeChanged);
            EventBus<LaserModeToggledEvent>.Subscribe(OnLaserModeToggled);
            EventBus<HoldActionToggledEvent>.Subscribe(OnHoldActionToggled);
            EventBus<PrimaryFireEndedEvent>.Subscribe(OnFireEnded);
            EventBus<BlowDetectedEvent>.Subscribe(OnBlowDetected);
            
            // RULE 6: Decoupled Destruction! Listen for bullets hitting balls.
            EventBus<EntityHitEvent>.Subscribe(OnEntityHit);
            
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }
        
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            _currentState = e.CurrentState;
              
            if (_currentState != GameState.Sandbox)
            {
                ClearAllSandboxWeapons();
            }
        }
 
        private void ClearAllSandboxWeapons()
        {
            // 1. Drop any held object
            if (_heldObject is SandboxBallController b) b.SetGrabbed(false);
            else if (_heldObject is SandboxBalloonController bal) bal.SetGrabbed(false);
            _heldObject = null;
            EventBus<ObjectGrabbedStateChangedEvent>.Publish(new ObjectGrabbedStateChangedEvent(false));
 
            // 2. Return all active projectiles to their pools
            for (int i = _activeBalls.Count - 1; i >= 0; i--) _ballPool.Release(_activeBalls[i]);
            _activeBalls.Clear();
            
            for (int i = _activeBalloons.Count - 1; i >= 0; i--) _balloonPool.Release(_activeBalloons[i]);
            _activeBalloons.Clear();
            
            for (int i = _activeSmokes.Count - 1; i >= 0; i--) _smokePool.Release(_activeSmokes[i]);
            _activeSmokes.Clear();
            
            if (_laserVisual != null) _laserVisual.enabled = false;
            ManageLoopingAudio(false, false);
        }

        private void OnLaserModeToggled(LaserModeToggledEvent e)
        {
            _laserMode = e.Mode;
        }
        
        private void OnHoldActionToggled(HoldActionToggledEvent e)
        {
            _holdAction = e.Action;
        }

        private void OnWeaponModeChanged(SandboxWeaponModeChangedEvent e)
        {
            _currentMode = e.Mode;
            
            // If we switch weapons while holding a ball, forcefully drop it!
            if (_heldObject != null)
            {
                if (_heldObject is SandboxBallController b) b.SetGrabbed(false);
                else if (_heldObject is SandboxBalloonController bal) bal.SetGrabbed(false);
                _heldObject = null;
                EventBus<ObjectGrabbedStateChangedEvent>.Publish(new ObjectGrabbedStateChangedEvent(false));
            }
            
            // FIX: Instantly cut the visual laser beam and audio when switching away from the Laser mode!
            if (_laserVisual != null) _laserVisual.enabled = false;
            ManageLoopingAudio(false, false);
        }

        private void OnFireStarted(PrimaryFireStartedEvent e)
        {
            if (_currentState != GameState.Sandbox) return;
            _isFiring = true; // Tracks that the screen is being held down

            if (_currentMode == SandboxWeaponMode.Laser && Camera.main != null)
            {
                if (_heldObject != null)
                {
                    // We are holding a ball. Action time!
                    Rigidbody heldRb = null;
                    if (_heldObject is SandboxBallController b) { b.SetGrabbed(false); heldRb = b.View.Rb; }
                    else if (_heldObject is SandboxBalloonController bal) { bal.SetGrabbed(false); heldRb = bal.View.Rb; }

                    if (_holdAction == SandboxHoldAction.Launch)
                    {
                        if (heldRb != null) heldRb.AddForce(Camera.main.transform.forward * _config.LaserLaunchForce, ForceMode.VelocityChange);
                    }

                    _heldObject = null;
                    EventBus<ObjectGrabbedStateChangedEvent>.Publish(new ObjectGrabbedStateChangedEvent(false));

                    // Prevent immediately grabbing another ball or destroying one in the same frame
                    _isFiring = false;
                }
                else if (_laserMode == SandboxLaserMode.TractorBeam)
                {
                    // We are empty handed. Shoot a raycast ONCE to try and grab a ball!
                    Transform camT = Camera.main.transform;
                    if (Physics.Raycast(camT.position, camT.forward, out RaycastHit hit, _config.LaserRange))
                    {
                        SandboxBallController hitBall = null;
                        SandboxBalloonController hitBalloon = null;
                        
                        foreach (var ball in _activeBalls)
                        {
                            if (ball.View.Collider == hit.collider)
                            {
                                hitBall = ball;
                                break;
                            }
                        }
                        foreach (var balloon in _activeBalloons)
                        {
                            if (balloon.View.Collider == hit.collider) { hitBalloon = balloon; break; }
                        }

                        if (hitBall != null || hitBalloon != null)
                        {
                            _heldObject = hitBall != null ? (object)hitBall : (object)hitBalloon;
                            if (hitBall != null) hitBall.SetGrabbed(true);
                            else if (hitBalloon != null) hitBalloon.SetGrabbed(true);
                            EventBus<ObjectGrabbedStateChangedEvent>.Publish(new ObjectGrabbedStateChangedEvent(true));
                        }
                    }
                }
            }
        }

        private void OnFireEnded(PrimaryFireEndedEvent e)
        {
            _isFiring = false;
        }
        
        private void OnBlowDetected(BlowDetectedEvent e)
        {
            if (_currentMode != SandboxWeaponMode.Balloons || _weaponView.BarrelPoint == null) return;
            if (_blowCooldownTimer > 0f) return;
            
            var balloon = _balloonPool.Get();
            _activeBalloons.Add(balloon);
            
            // Launch slightly forward and upwards like it's coming out of the mouth
            Vector3 startPos = _weaponView.BarrelPoint.position;
            Vector3 velocity = Camera.main.transform.forward * (e.Volume * _config.BalloonBlowForceMultiplier) + Vector3.up * 1.5f;
            
            balloon.Launch(startPos, velocity);
            _blowCooldownTimer = _config.BalloonBlowCooldown;
        }
         
         
        private void FireSmoke()
        {
            if (_weaponView.BarrelPoint == null) return;
            var smoke = _smokePool.Get();
            _activeSmokes.Add(smoke);
             
            Vector3 startPos = _weaponView.BarrelPoint.position;
            // Add a scatter-gun spread to the velocity so it blasts out like a fog machine!
            Vector3 velocity = (Camera.main.transform.forward + Random.insideUnitSphere * 0.15f).normalized * 15f;
             
            smoke.Launch(startPos, velocity);
            EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.SmokeFire, startPos));
        }
        
        private void FireBall()
        {
            if (_weaponView.BarrelPoint == null)
            {
                Debug.LogError("[SandboxWeaponService] Barrel Point is missing on the WeaponView!");
                return;
            }
 
            var ball = _ballPool.Get();
            _activeBalls.Add(ball);
 
            Vector3 startPos = _weaponView.BarrelPoint.position;
            
            // Free aim (Shoot straight out of camera)
            Vector3 velocity = Camera.main.transform.forward * 8f; 
 
            ball.Launch(startPos, velocity);
        }

        private void OnEntityHit(EntityHitEvent e)
        {
            // Did a bullet hit our bouncy ball? Pop it!
            for (int i = _activeBalls.Count - 1; i >= 0; i--)
            {
                if (_activeBalls[i].View.Collider == e.HitCollider)
                {
                    _ballPool.Release(_activeBalls[i]);
                    _activeBalls.RemoveAt(i);
                    EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BallDestroy, e.HitCollider.transform.position));
                    EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.BallDestroyExplosion, e.HitCollider.transform.position));
                    break;
                }
            }
            
            for (int i = _activeBalloons.Count - 1; i >= 0; i--)
            {
                if (_activeBalloons[i].View.Collider == e.HitCollider)
                {
                    _balloonPool.Release(_activeBalloons[i]);
                    _activeBalloons.RemoveAt(i);
                    
                    EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BalloonPop, e.HitCollider.transform.position));
                    EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.BalloonPopConfetti, e.HitCollider.transform.position));
                    break;
                }
            }
            
            for (int i = _activeSmokes.Count - 1; i >= 0; i--)
            {
                if (_activeSmokes[i].View.Collider == e.HitCollider)
                {
                    _smokePool.Release(_activeSmokes[i]);
                    _activeSmokes.RemoveAt(i);
                    break;
                }
            }
            
        }

        public void OnTick()
        {
            if (_currentState != GameState.Sandbox) return;
            if (_blowCooldownTimer > 0f) _blowCooldownTimer -= Time.deltaTime;
            foreach (var balloon in _activeBalloons) balloon.Tick(Time.deltaTime);
            
            // Tick and naturally clean up dissipated smoke clouds
            for (int i = _activeSmokes.Count - 1; i >= 0; i--)
            {
                _activeSmokes[i].Tick(Time.deltaTime);
                // Once the lifetime is up AND all the visible smoke particles have faded away, return it to the pool!
                if (!_activeSmokes[i].IsActive && !_activeSmokes[i].View.Particles.IsAlive(true))
                {
                    _smokePool.Release(_activeSmokes[i]);
                    _activeSmokes.RemoveAt(i);
                }
            }

            // 1. Handle Continuous Firing Loop
            if (_isFiring && (_currentMode == SandboxWeaponMode.BouncingBalls || _currentMode == SandboxWeaponMode.Smoke))
            {
                _fireTimer += Time.deltaTime;
                while (_fireTimer >= FireInterval)
                {
                    if (_currentMode == SandboxWeaponMode.BouncingBalls) FireBall();
                    else FireSmoke();
                    _fireTimer -= FireInterval;
                }
            }
            else
            {
                _fireTimer = FireInterval; // Ready to shoot immediately next time
            }
            
            bool isLaserActive = false;
            bool isTractorActive = false;

            
            // 2. Continuous Laser Visuals & Death Ray Logic
            if (_currentMode == SandboxWeaponMode.Laser && Camera.main != null && _weaponView.BarrelPoint != null)
            {
                _laserVisual.enabled = true;
                Transform camT = Camera.main.transform;

                if (_heldObject != null)
                {
                    isTractorActive = true;
                    
                    Transform heldTransform = _heldObject is SandboxBallController b ? b.View.transform : ((SandboxBalloonController)_heldObject).View.transform;
                    Rigidbody heldRb = _heldObject is SandboxBallController b2 ? b2.View.Rb : ((SandboxBalloonController)_heldObject).View.Rb;

                    // Tractor Beam: Cyan, pulls object to set distance
                    Vector3 targetPos = camT.position + camT.forward * _config.TractorBeamDistance;
                    
                    Vector3 pullVelocity = (targetPos - heldTransform.position) * 10f;
                     
                    // Upward Bias: If the ball is below our holding point, pull UP harder so it doesn't drag!
                    if (heldTransform.position.y < targetPos.y - 0.2f)
                    {
                        pullVelocity.y += 5f; 
                    }
                     
                    heldRb.linearVelocity = pullVelocity;
                    heldRb.angularVelocity = Vector3.zero; // Prevent floor-rolling friction

                    
                    // Only show the Cyan laser while it's actively being pulled towards us.
                    // Once it reaches the holding distance, turn the laser off to simulate a local gravity field!
                    if (Vector3.Distance(heldTransform.position, targetPos) > 0.5f)
                    {
                        _laserVisual.startColor = Color.cyan;
                        _laserVisual.endColor = Color.cyan;
                        _laserVisual.SetPosition(0, _weaponView.BarrelPoint.position);
                        _laserVisual.SetPosition(1, heldTransform.position);
                    }
                    else
                    {
                        _laserVisual.enabled = false;
                    }
                }
                else
                {
                    // Continuous Pointer (Green) or Death Ray (Red)
                    bool isDeathRay = _isFiring && _laserMode == SandboxLaserMode.Destroy;
                    Color laserColor = isDeathRay ? Color.red : Color.green;
                    
                    if (isDeathRay) isLaserActive = true;

                    _laserVisual.startColor = laserColor;
                    _laserVisual.endColor = laserColor;
                    _laserVisual.SetPosition(0, _weaponView.BarrelPoint.position);

                    if (Physics.Raycast(camT.position, camT.forward, out RaycastHit hit, _config.LaserRange))
                    {
                        _laserVisual.SetPosition(1, hit.point);

                        // Continuous Death Ray Destruction! Sweeping the laser obliterates balls instantly.
                        if (isDeathRay)
                        {
                            SandboxBallController hitBall = null;
                            SandboxBalloonController hitBalloon = null;
                            
                            foreach (var ball in _activeBalls)
                            {
                                if (ball.View.Collider == hit.collider) { hitBall = ball; break; }
                            }
                            foreach (var balloon in _activeBalloons)
                            {
                                if (balloon.View.Collider == hit.collider) { hitBalloon = balloon; break; }
                            }

                            if (hitBall != null || hitBalloon != null)
                            {
                                if (hitBall != null) 
                                { 
                                    _ballPool.Release(hitBall);
                                    _activeBalls.Remove(hitBall); 
                                    EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BallDestroy, hit.point));
                                    EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.BallDestroyExplosion, hit.point));
                                }

                                if (hitBalloon != null) { 
                                    _balloonPool.Release(hitBalloon);
                                    _activeBalloons.Remove(hitBalloon); 
                                    EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BalloonPop, hit.point));
                                    EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.BalloonPopConfetti, hit.point));
                                }
                            }
                            else
                            {
                                // We missed the objects and are hitting a wall/floor!
                                _laserSparkTimer += Time.deltaTime;
                                if (_laserSparkTimer >= 0.1f)
                                {
                                    EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.LaserHitSparks, hit.point, hit.normal));
                                    _laserSparkTimer = 0f;
                                }
                            }
                        }
                        else
                        {
                            _laserSparkTimer = 0.1f; // Reset so it sparks instantly the next time we fire
                        }
                    }
                    else
                    {
                        _laserVisual.SetPosition(1, camT.position + camT.forward * _config.LaserRange);
                    }
                }
            }
            else
            {
                _laserVisual.enabled = false;
            }
            
            ManageLoopingAudio(isLaserActive, isTractorActive);
        }
        
        private void ManageLoopingAudio(bool isLaserActive, bool isTractorActive)
        {
            if (_weaponView.LoopingAudioSource == null) return;

            AudioClip targetClip = isTractorActive ? _config.TractorHumClip : (isLaserActive ? _config.LaserHumClip : null);

            if (targetClip != null)
            {
                if (_weaponView.LoopingAudioSource.clip != targetClip || !_weaponView.LoopingAudioSource.isPlaying)
                {
                    _weaponView.LoopingAudioSource.clip = targetClip;
                    _weaponView.LoopingAudioSource.loop = true;
                    _weaponView.LoopingAudioSource.Play();
                }
            }
            else
            {
                if (_weaponView.LoopingAudioSource.isPlaying) _weaponView.LoopingAudioSource.Stop();
            }
        }

        public void OnDispose()
        {
            EventBus<PrimaryFireStartedEvent>.Unsubscribe(OnFireStarted);
            EventBus<PrimaryFireEndedEvent>.Unsubscribe(OnFireEnded);
            
            // FIX: Safely unsubscribe to prevent memory leaks on shutdown
            EventBus<SandboxWeaponModeChangedEvent>.Unsubscribe(OnWeaponModeChanged);
            EventBus<LaserModeToggledEvent>.Unsubscribe(OnLaserModeToggled);
            EventBus<HoldActionToggledEvent>.Unsubscribe(OnHoldActionToggled);
            EventBus<EntityHitEvent>.Unsubscribe(OnEntityHit);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<BlowDetectedEvent>.Unsubscribe(OnBlowDetected);

            if (_weaponView.LoopingAudioSource != null) _weaponView.LoopingAudioSource.Stop();
            if (_laserVisual != null) Object.Destroy(_laserVisual.gameObject);
            foreach (var ball in _activeBalls) Object.Destroy(ball.View.gameObject);
            _activeBalls.Clear();
            _ballPool?.Clear();
            
            foreach (var balloon in _activeBalloons) Object.Destroy(balloon.View.gameObject);
            _activeBalloons.Clear();
            _balloonPool?.Clear();
             
            foreach (var smoke in _activeSmokes) Object.Destroy(smoke.View.gameObject);
            _activeSmokes.Clear();
            _smokePool?.Clear();
            
        }
    }
}