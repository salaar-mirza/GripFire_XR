using ARFps.Core.Events;
using UnityEngine;

namespace ARFps.Features.Weapon.Events
{
    /// <summary>
    /// Published when the weapon is fired, containing trajectory data.
    /// Used by VFX, Audio, and projectile systems to react to shots without direct coupling.
    /// </summary>
    public readonly struct WeaponFiredEvent : IGameEvent
    {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;

        public WeaponFiredEvent(Vector3 origin, Vector3 direction) => (Origin, Direction) = (origin, direction);
    }

    /// <summary>
    /// Published by the Weapon System when a bullet hits a physical collider.
    /// </summary>
    public readonly struct EntityHitEvent : IGameEvent
    {
        public readonly Collider HitCollider;
        public readonly int Damage;

        public EntityHitEvent(Collider hitCollider, int damage)
        {
            HitCollider = hitCollider;
            Damage = damage;
        }
    }
}