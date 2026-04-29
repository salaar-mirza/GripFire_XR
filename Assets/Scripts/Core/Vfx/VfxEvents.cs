using UnityEngine;
using ARFps.Core.Events;

namespace ARFps.Core.Vfx.Events
{
    public enum VfxType { BallBounceDust, BalloonPopConfetti, LaserHitSparks, BallDestroyExplosion }

    /// <summary>
    /// Published when an entity wants to spawn a 3D particle effect at a specific location.
    /// </summary>
    public readonly struct PlayVfxEvent : IGameEvent
    {
        public readonly VfxType Type;
        public readonly Vector3 Position;
        public readonly Vector3 Normal;

        public PlayVfxEvent(VfxType type, Vector3 position, Vector3 normal = default)
        {
            Type = type;
            Position = position;
            Normal = normal == default ? Vector3.up : normal;
        }
    }
}