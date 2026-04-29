using UnityEngine;

namespace ARFps.Features.Weapon
{
    /// <summary>
    /// The visual and physical representation of a bullet projectile.
    /// </summary>
    public class BulletView : MonoBehaviour
    {
        [Tooltip("Optional: Drag a TrailRenderer here so the pool can clear it upon respawn.")]
        public TrailRenderer Trail;
    }
}