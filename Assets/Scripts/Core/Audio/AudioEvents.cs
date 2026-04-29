using UnityEngine;
using ARFps.Core.Events;

namespace ARFps.Core.Audio.Events
{
    public enum SfxType { BallBounce, BalloonPop, SmokeFire, LaserGreen, LaserRed, TractorBeam, BulletFire, BallDestroy }

    /// <summary>
    /// Published when an entity wants to play a 3D spatial sound effect at a specific location.
    /// </summary>
    public readonly struct PlaySfxEvent : IGameEvent
    {
        public readonly SfxType Type;
        public readonly Vector3 Position;

        public PlaySfxEvent(SfxType type, Vector3 position)
        {
            Type = type;
            Position = position;
        }
    }
}