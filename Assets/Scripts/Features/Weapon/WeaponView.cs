using UnityEngine;

namespace ARFps.Features.Weapon
{
    /// <summary>
    /// The visual representation of the player's weapon.
    /// Provides physical scene references (like the barrel tip) for the weapon controller.
    /// </summary>
    public class WeaponView : MonoBehaviour
    {
        [Tooltip("The physical point in space where bullets will spawn. The Z-axis (forward) defines the firing direction.")]
        public Transform BarrelPoint;

        [Tooltip("The audio source used for continuous looping sounds like lasers.")]
        public AudioSource LoopingAudioSource;
    }
}