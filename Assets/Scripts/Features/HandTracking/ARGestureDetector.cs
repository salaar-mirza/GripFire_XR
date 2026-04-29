using ARFps.Core.Events;
using ARFps.Features.HandTracking.Events;
using UnityEngine;

namespace ARFps.Features.HandTracking
{
    /// <summary>
    /// Processes raw joint data into pinch gestures.
    /// Applies hysteresis logic to prevent input flickering and publishes gesture events.
    /// </summary>
    public class ARGestureDetector
    {
        private readonly HandTrackingConfig _config;
        private bool _isPinching;

        public ARGestureDetector(HandTrackingConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Processes the raw joint positions to detect a change in the pinch state.
        /// </summary>
        /// <param name="thumbTipPos">The world position of the thumb tip.</param>
        /// <param name="indexProximalPos">The world position of the index proximal joint.</param>
        public void ProcessGesture(Vector3 thumbTipPos, Vector3 indexProximalPos)
        {
            // Use squared magnitude for performance. Avoids the expensive square root operation.
            float distanceSqr = Vector3.SqrMagnitude(thumbTipPos - indexProximalPos);

            if (_isPinching)
            {
                // Hysteresis: Check for release
                // We must move *further* away than the pull threshold to register a release.
                if (distanceSqr > (_config.PinchReleaseThreshold * _config.PinchReleaseThreshold))
                {
                    _isPinching = false;
                    EventBus<PinchEndedEvent>.Publish(new PinchEndedEvent());

                    if (_config.EnableDebugLogging)
                    {
                        Debug.Log("[GestureDetector] Pinch Ended.");
                    }
                }
            }
            else
            {
                // Check for pull
                if (distanceSqr <= (_config.PinchDistanceThreshold * _config.PinchDistanceThreshold))
                {
                    _isPinching = true;
                    EventBus<PinchStartedEvent>.Publish(new PinchStartedEvent(indexProximalPos));

                    if (_config.EnableDebugLogging)
                    {
                        Debug.Log($"[GestureDetector] Pinch Started at distance: {Mathf.Sqrt(distanceSqr)}m");
                    }
                }
            }
        }

        /// <summary>
        /// Forcefully ends an active gesture. Used when hand tracking is lost mid-pinch.
        /// </summary>
        public void CancelGesture()
        {
            if (_isPinching)
            {
                _isPinching = false;
                EventBus<PinchEndedEvent>.Publish(new PinchEndedEvent());

                if (_config.EnableDebugLogging)
                {
                    Debug.Log("[GestureDetector] Pinch Canceled (Tracking Lost).");
                }
            }
        }
    }
}