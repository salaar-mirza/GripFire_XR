using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.Audio;
using UnityEngine;
using ARFps.Core.State;
using ARFps.Features.HandTracking;
using ARFps.Features.PlayerInput;
using ARFps.Features.SwarmPathfinding;
using ARFps.Features.Weapon;
using ARFps.Features.Enemy;
using ARFps.Features.RoomMapping;
using ARFps.Features.Player;
using ARFps.Core.Vfx;
using ARFps.Core.UI;
using ARFps.Features.Player.UI;
using UnityEngine.XR.ARFoundation;
using ARFps.Features.LevelDirector;
using ARFps.Features.Sandbox;
using System.Collections.Generic;

/// <summary>
/// The central bootstrapper and lifecycle manager for the game.
/// Orchestrates the two-phase boot sequence, service ticking, and clean teardown.
/// Should be the main entry point MonoBehaviour in the scene.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Feature Configurations")]
    [SerializeField] 
    private ManualRoomMappingConfig _manualRoomMappingConfig;
    
    [Header("Level & Gameplay Configs")]

    [SerializeField] private LevelConfig _levelConfig;
    [SerializeField] private SwarmConfig _swarmConfig;
    [SerializeField] private WeaponConfig _weaponConfig;
    [SerializeField] private PlayerConfig _playerConfig;
     
    [Header("UI")]
    [SerializeField] private ModeSelectionView _modeSelectionView;
    [SerializeField] private CombatUIView _combatUIView;
    
    [Header("Sandbox")]
    [SerializeField] private SandboxConfig _sandboxConfig;
    [SerializeField] private SandboxUIView _sandboxUIView;

    [Header("Audio")]
    [SerializeField] private AudioConfig _audioConfig;
    
     
    [Header("VFX")]
    [SerializeField] VfxConfig _vfxConfig;


    
    [Header("Scene Views")]
    [SerializeField] private ARAnchorManager _arAnchorManager;
    [SerializeField] private ARRaycastManager _arRaycastManager;
    [SerializeField] private ARPlaneManager _arPlaneManager; // We need this for the raycast to work
    [SerializeField] private RoomMappingUIView _roomMappingUIView;
    [SerializeField] private FloorReticleView _floorReticleView;
    [SerializeField] private VirtualRoomView _virtualRoomViewPrefab;

 
    [SerializeField]
    private WeaponView _weaponView;

    // Cached list of tickable services to completely prevent GC Allocations and Type Casting overhead during Update
    private readonly List<ITickable> _tickableServices = new List<ITickable>();
    
    
    /// <summary>
    /// Awake is used to control the strict boot sequence.
    /// </summary>
    private void Awake()
    {
        // Capture the Unity Main Thread ID for background thread event marshaling.
        EventBus.InitializeMainThreadId();
        
        Debug.Log("[GameInitializer] --- STARTING TWO-PHASE BOOT ---");

        // --- PHASE 1 (Awake): REGISTRATION ---
        Debug.Log("[GameInitializer] Phase 1: Registering Services...");
        
        // Core Systems
        GameService.Register(new GameStateService());
        GameService.Register(new AudioService(_audioConfig));
        GameService.Register(new VfxService(_vfxConfig));
        
        // Input Systems
        GameService.Register(new TouchInputService());
        GameService.Register(new MicrophoneInputService());
        
        // AR & Room Mapping Systems
        GameService.Register(new ManualRoomMappingService(_manualRoomMappingConfig, _arRaycastManager,_arPlaneManager, _floorReticleView));
        GameService.Register(new NavMeshBuilderService(_virtualRoomViewPrefab));
        GameService.Register(new BoundaryVisualsService(_manualRoomMappingConfig));
        
        // UI Systems
        GameService.Register(new RoomMappingUIController(_roomMappingUIView));
        GameService.Register(new ModeSelectionController(_modeSelectionView));
        GameService.Register(new CombatUIController(_combatUIView));
        GameService.Register(new SandboxUIController(_sandboxUIView));

        // Gameplay Systems
        GameService.Register(new PlayerHealthService(_playerConfig));
        GameService.Register(new WeaponService(_weaponConfig, _weaponView));
        GameService.Register(new TargetSpawningService(_swarmConfig));
        GameService.Register(new LevelDirectorService(_levelConfig));
        GameService.Register( new SandboxEntityService(_sandboxConfig));
        GameService.Register(new SandboxWeaponService(_sandboxConfig, _weaponView));
    }

    private void Start()
    {
        // --- PHASE 2 (Start): INITIALIZATION ---
        Debug.Log("[GameInitializer] Phase 2: Initializing Services...");
        foreach (var service in GameService.GetAllServices())
        {
            service.OnInit();
            
            // Cache any service that requires a frame-by-frame tick
            if (service is ITickable tickable)
            {
                _tickableServices.Add(tickable);
            }
        }

        GameService.Get<GameStateService>().ChangeState(GameState.RoomScanning);

        Debug.Log("[GameInitializer] --- BOOT SEQUENCE COMPLETE ---");
    }

    /// <summary>
    /// The central Update loop that ticks all registered services.
    /// </summary>
    private void Update()
    {
        // Flush any events that were published from background threads.
        EventBus.ProcessMainThreadActions();
        
        // A standard 'for' loop prevents IEnumerable boxing, achieving true 0B GC Allocation!
        for (int i = 0; i < _tickableServices.Count; i++)
        {
            _tickableServices[i].OnTick();
        }
    }

    /// <summary>
    /// OnDestroy ensures a clean shutdown, preventing static memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        Debug.Log("[GameInitializer] --- INITIATING SHUTDOWN & CLEANUP ---");

        // Dispose services, allowing them to unregister from events and clean up.
        foreach (var service in GameService.GetAllServices())
        {
            service.OnDispose();
        }

        // Unregister all services from the locator.
        GameService.UnregisterAll();

        // Clear all event bus listeners to prevent static memory leaks (RULE 2.2).
        EventBus.ClearAllBuses();

        Debug.Log("[GameInitializer] --- SHUTDOWN COMPLETE ---");
    }
}