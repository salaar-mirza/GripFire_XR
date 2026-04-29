using UnityEngine;

namespace ARFps.Core.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SfxView : MonoBehaviour
    {
        public AudioSource Source { get; private set; }

        private void Awake()
        {
            Source = GetComponent<AudioSource>();
        }
    }
}