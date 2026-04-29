using ARFps.Core.Services;
using UnityEngine;

namespace ARFps.Features.HandTracking
{
    /// <summary>
    /// The central manager orchestrating XR Hand Tracking and Gesture Detection.
    /// </summary>
    public class HandTrackingService : IService, ITickable
    {
        private readonly ARHandTracker _handTracker;
        private readonly ARGestureDetector _gestureDetector;

        public HandTrackingService(HandTrackingConfig config)
        {
            // The service creates and owns its controllers, passing the config to the one that needs it.
            _handTracker = new ARHandTracker();
            _gestureDetector = new ARGestureDetector(config);
        }

        /// <summary>
        /// Phase 2 Init: Called after all services are registered.
        /// </summary>
        public void OnInit()
        {
            // Attempt to hook into the XR Hands subsystem immediately.
            _handTracker.TryInitializeSubsystem();
            Debug.Log("[HandTrackingService] Initialized.");
        }

        /// <summary>
        /// Per-frame update loop driven by GameInitializer.
        /// </summary>
        public void OnTick()
        {
            // The core loop: Read raw data, then process it for gestures.
            if (_handTracker.TryGetPinchJoints(out Vector3 thumbPos, out Vector3 indexPos))
            {
                _gestureDetector.ProcessGesture(thumbPos, indexPos);
            }
            else
            {
                // FIX: If tracking drops off-screen while pinching, force a release!
                _gestureDetector.CancelGesture();
            }
        }

        /// <summary>
        /// Called on shutdown to clean up resources.
        /// </summary>
        public void OnDispose()
        {
            // Nothing to dispose of currently, but the contract must be fulfilled.
            Debug.Log("[HandTrackingService] Disposed.");
        }
    }
}