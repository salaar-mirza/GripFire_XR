using UnityEngine;

namespace ARFps.Core.Vfx
{
    [CreateAssetMenu(fileName = "VfxConfig", menuName = "ARFps/Core/VfxConfig")]
    public class VfxConfig : ScriptableObject
    {
        [Tooltip("How many of EACH particle system to pre-warm in the object pool.")]
        public int DefaultPoolSize = 10;

        [Header("VFX Prefabs")]
        public GameObject VfxBallBounceDust;
        public GameObject VfxBalloonPopConfetti;
        public GameObject VfxLaserHitSparks;
        public GameObject VfxBallDestroyExplosion;

    }
}