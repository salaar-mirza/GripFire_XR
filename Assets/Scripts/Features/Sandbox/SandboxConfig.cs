using UnityEngine;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// Immutable configuration data for the physics sandbox entities.
    /// </summary>
    [CreateAssetMenu(fileName = "SandboxConfig", menuName = "ARFps/Features/Sandbox/SandboxConfig")]
    public class SandboxConfig : ScriptableObject
    {
        [Header("Dynamic Fans")]
        public GameObject FanPrefab;
        public int NumberOfFloorFans = 2;
        public int NumberOfCelingFans = 2;
        public float FanSpinSpeed = 720f;
        public float FanMoveSpeed = 1.5f;
         
        [Header("Bouncing Balls")]
        public GameObject BallPrefab;
        public int BallPoolSize = 150;
        [Tooltip("How many seconds the ball takes to complete its parabolic arc to the target.")]
        public float BallTimeOfFlight = 1.2f;
        [Tooltip("The amount of explosive impulse force applied when a ball hits a moving fan.")]
        public float BallChaoticBounceForce = 15f;

        [Header("Laser / Tractor Beam")]
        public float LaserRange = 50f;
        public float TractorBeamDistance = 0.50f;
        public float LaserLaunchForce = 25f;
        
          
        [Header("Floating Balloons")]
        public GameObject BalloonPrefab;
        public int BalloonPoolSize = 50;
        [Tooltip("The constant upward force applied to counteract gravity.")]
        public float BalloonFloatForce = 12f; 
        public float BalloonBlowCooldown = 0.2f; // Prevents spawning 60 balloons a second
        [Tooltip("Multiplier for the microphone volume to calculate forward blow force.")]
        public float BalloonBlowForceMultiplier = 20f;
        
        [Tooltip("The downward impulse force applied when a balloon hits a moving fan.")]
        public float BalloonFanBounceForce = 10f;
        
         
        [Header("Smoke Gun")]
        public GameObject SmokePrefab;
        public int SmokePoolSize = 25;
        public float SmokeDuration = 4f; // How long it emits smoke before stopping
   
        [Tooltip("The amount of explosive impulse force applied when a smoke canister hits a moving fan.")]
        public float SmokeChaoticBounceForce = 15f;
        
         
        [Header("Looping Audio (SFX)")]
        public AudioClip FanWhirClip;
        public AudioClip LaserHumClip;
        public AudioClip TractorHumClip;
    
    }
}