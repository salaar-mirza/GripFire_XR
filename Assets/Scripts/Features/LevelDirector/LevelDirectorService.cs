using System.Collections.Generic;
using UnityEngine;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using ARFps.Features.Enemy;
using ARFps.Features.Enemy.Events;
using ARFps.Features.Player.Events;

namespace ARFps.Features.LevelDirector
{
    public class LevelDirectorService : IService, ITickable
    {
        private readonly LevelConfig _levelConfig;
        private GameStateService _gameStateService;
        private TargetSpawningService _targetSpawningService;

        private int _currentWaveIndex = -1;
        private float _waveDelayTimer;
        private int _activeEnemiesInWave; // Tracked by the service, not a global state

        public LevelDirectorService(LevelConfig levelConfig)
        {
            _levelConfig = levelConfig;
        }

        public void OnInit()
        {
            _gameStateService = GameService.Get<GameStateService>();
            _targetSpawningService = GameService.Get<TargetSpawningService>();
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<SwarmEnemyDestroyedEvent>.Subscribe(OnTargetDestroyed);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.CurrentState == GameState.Playing)
            {
                _currentWaveIndex = -1;
                TryStartNextWave();
            }
        }

        private void OnTargetDestroyed(SwarmEnemyDestroyedEvent e)
        {
            // Only process if combat is actively playing
            if (_gameStateService.CurrentState != GameState.Playing) return;
            
            // Ignore stray deaths from previous waves if we are currently in a wave delay countdown
            if (_waveDelayTimer >= 0) return;
 
            _activeEnemiesInWave--;
            if (_activeEnemiesInWave <= 0)
            {
                TryStartNextWave();
            }
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            _gameStateService.ChangeState(GameState.GameOver);
        }

        private void TryStartNextWave()
        {
            _currentWaveIndex++;
            if (_currentWaveIndex >= _levelConfig.Waves.Count)
            {
                _gameStateService.ChangeState(GameState.GameOver);
                return;
            }

            var nextWave = _levelConfig.Waves[_currentWaveIndex];
            _waveDelayTimer = nextWave.DelayBeforeWave;

            if (_currentWaveIndex == 0)
            {
                // This is the official start of combat
                _gameStateService.ChangeState(GameState.Playing);
            }
        }

        public void OnTick()
        {
            if (_gameStateService.CurrentState != GameState.Playing || _waveDelayTimer < 0) return;

            _waveDelayTimer -= Time.deltaTime;
            if (_waveDelayTimer <= 0)
            {
                var wave = _levelConfig.Waves[_currentWaveIndex];
                
                // Set the active enemy count exactly when they spawn to prevent state desync
                _activeEnemiesInWave = wave.TargetCount;
                _targetSpawningService.StartWave(wave.TargetCount);
                _waveDelayTimer = -1; // Ensure this only runs once
            }
        }

        public void OnDispose()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<SwarmEnemyDestroyedEvent>.Unsubscribe(OnTargetDestroyed);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
        }
    }
}