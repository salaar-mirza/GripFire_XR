using UnityEngine;

namespace ARFps.Core.Vfx
{
    [RequireComponent(typeof(ParticleSystem))]
    public class VfxView : MonoBehaviour
    {
        public ParticleSystem Particles { get; private set; }

        private void Awake()
        {
            Particles = GetComponent<ParticleSystem>();
        }
    }
}