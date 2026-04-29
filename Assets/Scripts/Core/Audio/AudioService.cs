using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using ARFps.Core.Services;
using ARFps.Core.Events;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using ARFps.Core.Audio.Events;

namespace ARFps.Core.Audio
{
    public class AudioService : IService, ITickable
    {
        private readonly AudioConfig _config;
        private AudioSource _bgmSource;
        
        private ObjectPool<SfxView> _sfxPool;
        private readonly List<SfxView> _activeSfx = new List<SfxView>();

        public AudioService(AudioConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            // 1. Setup 2D Background Music Player
            GameObject bgmObj = new GameObject("BGM_Player");
            _bgmSource = bgmObj.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f; // 2D sound (everywhere)
            _bgmSource.volume = 0.4f; // Keep BGM soft
            Object.DontDestroyOnLoad(bgmObj);

            // 2. Setup 3D Spatial SFX Pool (RULE 3)
            _sfxPool = new ObjectPool<SfxView>(
                createFunc: () => Object.Instantiate(_config.SfxPrefab).GetComponent<SfxView>(),
                actionOnGet: null,
                actionOnRelease: view => {
                    view.Source.Stop();
                    view.gameObject.SetActive(false);
                },
                actionOnDestroy: view => Object.Destroy(view.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.SfxPoolSize,
                maxSize: 50
            );

            var preWarm = new List<SfxView>();
            for (int i = 0; i < _config.SfxPoolSize; i++) preWarm.Add(_sfxPool.Get());
            foreach (var s in preWarm) _sfxPool.Release(s);

            // 3. Subscriptions
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<PlaySfxEvent>.Subscribe(OnPlaySfx);
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            _bgmSource.Stop();
            
            if (e.CurrentState == GameState.RoomScanning && _config.BgmMappingPhase != null)
            {
                _bgmSource.clip = _config.BgmMappingPhase;
                _bgmSource.Play();
            }
            else if ((e.CurrentState == GameState.Sandbox || e.CurrentState == GameState.Playing) && _config.BgmPlayingPhases.Length > 0)
            {
                // Pick a random playing track
                _bgmSource.clip = _config.BgmPlayingPhases[Random.Range(0, _config.BgmPlayingPhases.Length)];
                _bgmSource.Play();
            }
        }

        private void OnPlaySfx(PlaySfxEvent e)
        {
            AudioClip clipToPlay = e.Type switch
            {
                SfxType.BallBounce => _config.SfxBallBounce,
                SfxType.BalloonPop => _config.SfxBalloonPop,
                SfxType.SmokeFire => _config.SfxSmokeFire,
                SfxType.LaserGreen => _config.SfxLaserGreen,
                SfxType.LaserRed => _config.SfxLaserRed,
                SfxType.TractorBeam => _config.SfxTractorBeam,
                SfxType.BulletFire => _config.SfxBulletFire,
                SfxType.BallDestroy => _config.SfxBallDestroy,
                _ => null
            };

            if (clipToPlay == null) return;

            var sfxView = _sfxPool.Get();
            sfxView.gameObject.SetActive(true);
            sfxView.transform.position = e.Position;
            
            // Add slight pitch variation so repetitive sounds (bounces) don't get annoying!
            sfxView.Source.pitch = Random.Range(0.9f, 1.1f); 
            sfxView.Source.PlayOneShot(clipToPlay);
            
            _activeSfx.Add(sfxView);
        }

        public void OnTick()
        {
            // Automatically return audio sources to the pool once they finish playing (RULE 4)
            for (int i = _activeSfx.Count - 1; i >= 0; i--)
            {
                if (!_activeSfx[i].Source.isPlaying)
                {
                    _sfxPool.Release(_activeSfx[i]);
                    _activeSfx.RemoveAt(i);
                }
            }
        }

        public void OnDispose()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<PlaySfxEvent>.Unsubscribe(OnPlaySfx);
        }
    }
}