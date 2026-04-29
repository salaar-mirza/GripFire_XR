using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using ARFps.Core.Services;
using ARFps.Core.Events;
using ARFps.Core.Vfx.Events;

namespace ARFps.Core.Vfx
{
    public class VfxService : IService, ITickable
    {
        private readonly VfxConfig _config;
        
        private readonly Dictionary<VfxType, ObjectPool<VfxView>> _pools = new Dictionary<VfxType, ObjectPool<VfxView>>();
        private readonly List<(VfxView view, VfxType type)> _activeVfx = new List<(VfxView, VfxType)>();

        public VfxService(VfxConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            CreatePool(VfxType.BallBounceDust, _config.VfxBallBounceDust);
            CreatePool(VfxType.BalloonPopConfetti, _config.VfxBalloonPopConfetti);
            CreatePool(VfxType.LaserHitSparks, _config.VfxLaserHitSparks);
            CreatePool(VfxType.BallDestroyExplosion, _config.VfxBallDestroyExplosion);

            EventBus<PlayVfxEvent>.Subscribe(OnPlayVfx);
        }

        private void CreatePool(VfxType type, GameObject prefab)
        {
            if (prefab == null) return;

            var pool = new ObjectPool<VfxView>(
                createFunc: () => Object.Instantiate(prefab).GetComponent<VfxView>(),
                actionOnGet: null,
                actionOnRelease: v => {
                    v.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    v.gameObject.SetActive(false);
                },
                actionOnDestroy: v => Object.Destroy(v.gameObject),
                collectionCheck: false,
                defaultCapacity: _config.DefaultPoolSize,
                maxSize: 50
            );

            // RULE 3: Hoard & Return Pre-warm
            var preWarm = new List<VfxView>();
            for (int i = 0; i < _config.DefaultPoolSize; i++) preWarm.Add(pool.Get());
            foreach (var v in preWarm) pool.Release(v);

            _pools[type] = pool;
        }

        private void OnPlayVfx(PlayVfxEvent e)
        {
            if (!_pools.TryGetValue(e.Type, out var pool)) return;

            var view = pool.Get();
            view.gameObject.SetActive(true);
            view.transform.position = e.Position;
            view.transform.rotation = Quaternion.LookRotation(e.Normal);
            
            view.Particles.Play(true);
            _activeVfx.Add((view, e.Type));
        }

        public void OnTick()
        {
            for (int i = _activeVfx.Count - 1; i >= 0; i--)
            {
                var active = _activeVfx[i];
                if (!active.view.Particles.IsAlive(true))
                {
                    _pools[active.type].Release(active.view);
                    _activeVfx.RemoveAt(i);
                }
            }
        }

        public void OnDispose()
        {
            EventBus<PlayVfxEvent>.Unsubscribe(OnPlayVfx);
            foreach (var active in _activeVfx) Object.Destroy(active.view.gameObject);
            _activeVfx.Clear();
            foreach (var pool in _pools.Values) pool.Clear();
            _pools.Clear();
        }
    }
}