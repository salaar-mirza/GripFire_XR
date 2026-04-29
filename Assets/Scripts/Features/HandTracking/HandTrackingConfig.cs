using UnityEngine;

namespace ARFps.Features.HandTracking
{
    /// <summary>
    /// Configuration data for hand tracking gestures.
    /// Contains safe thresholds for pinch detection and hysteresis.
    /// </summary>
    [CreateAssetMenu(fileName = "HandTrackingConfig", menuName = "ARFps/Features/HandTracking/HandTrackingConfig")]
    public class HandTrackingConfig : ScriptableObject
    {
        [Header("Gesture Thresholds")]
        [Tooltip("The distance in meters between the Thumb Tip and Index Proximal joint to register a 'trigger pull'.")]
        [Range(0.01f, 0.15f)]
        public float PinchDistanceThreshold = 0.03f;
        
        [Tooltip("The distance required to register a release. Slightly higher than the pull threshold to prevent flickering (Hysteresis).")]
        [Range(0.02f, 0.20f)]
        public float PinchReleaseThreshold = 0.045f;
        
        [Tooltip("Enable this to log pinch states to the console for debugging.")]
        public bool EnableDebugLogging = true;
 
        private void OnValidate()
        {
            // Ensure release threshold is always safely above the pull threshold
            if (PinchReleaseThreshold <= PinchDistanceThreshold)
            {
                PinchReleaseThreshold = PinchDistanceThreshold + 0.015f;
            }
        }
    }
}