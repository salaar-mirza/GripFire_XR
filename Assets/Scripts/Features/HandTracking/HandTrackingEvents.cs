using ARFps.Core.Events;
using UnityEngine;

namespace ARFps.Features.HandTracking.Events
{
    /// <summary>
    /// Published when the thumb and index finger close past the PinchDistanceThreshold.
    /// </summary>
    public struct PinchStartedEvent : IGameEvent
    {
        public readonly Vector3 PinchPosition;

        public PinchStartedEvent(Vector3 pinchPosition) => PinchPosition = pinchPosition;
    }

    /// <summary>
    /// Published when the thumb and index finger open past the PinchReleaseThreshold.
    /// </summary>
    public struct PinchEndedEvent : IGameEvent
    {
    }
}