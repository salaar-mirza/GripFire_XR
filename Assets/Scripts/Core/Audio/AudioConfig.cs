using UnityEngine;

namespace ARFps.Core.Audio
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "ARFps/Core/AudioConfig")]
    public class AudioConfig : ScriptableObject
    {
        [Header("Background Music (BGM)")]
        public AudioClip BgmMappingPhase;
        public AudioClip[] BgmPlayingPhases; // Array so it can pick randomly or loop!

        [Header("Sound Effects (SFX)")]
        public GameObject SfxPrefab; // A prefab with an AudioSource component
        public int SfxPoolSize = 20; // How many overlapping sounds can play at once
        
        [Space(10)]
        public AudioClip SfxBallBounce;
        public AudioClip SfxBalloonPop;
        public AudioClip SfxSmokeFire;
        public AudioClip SfxLaserGreen;
        public AudioClip SfxLaserRed;
        public AudioClip SfxTractorBeam;
        public AudioClip SfxBulletFire;
        public AudioClip SfxBallDestroy;
    }
}