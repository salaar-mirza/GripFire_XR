using ARFps.Core.Events;

namespace ARFps.Features.Sandbox
{

    public enum SandboxWeaponMode { Bullets, BouncingBalls, Laser, Balloons, Smoke }
    public enum SandboxLaserMode { Destroy, TractorBeam }
    public enum SandboxHoldAction { Drop, Launch }

    /// <summary>
    /// Published when the player toggles between shooting bullets and shooting physics balls.
    /// </summary>
    public readonly struct SandboxWeaponModeChangedEvent : IGameEvent
    {
        public readonly SandboxWeaponMode Mode;
        public SandboxWeaponModeChangedEvent(SandboxWeaponMode mode) { Mode = mode; }
    }
    /// Published when the player toggles the Laser mode (Destroy vs Tractor Beam).    
    public readonly struct LaserModeToggledEvent : IGameEvent
    {
        public readonly SandboxLaserMode Mode;
        public LaserModeToggledEvent(SandboxLaserMode mode) { Mode = mode; }
    }
 
    /// <summary>
    /// Published when the player toggles the Holding action (Drop vs Launch).
    /// </summary>
    public readonly struct HoldActionToggledEvent : IGameEvent
    {
        public readonly SandboxHoldAction Action;
        public HoldActionToggledEvent(SandboxHoldAction action) { Action = action; }
    }
     
    public readonly struct ObjectGrabbedStateChangedEvent : IGameEvent
    {
        public readonly bool IsGrabbing;
        public ObjectGrabbedStateChangedEvent(bool isGrabbing) { IsGrabbing = isGrabbing; }
    }
    
}