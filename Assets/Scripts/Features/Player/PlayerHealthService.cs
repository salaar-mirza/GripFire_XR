using UnityEngine;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State.Events;
using ARFps.Core.State;
using ARFps.Features.Player.Events;

namespace ARFps.Features.Player
{
    /// <summary>
    /// Manages the player's health state and handles incoming damage events.
    /// </summary>
    public class PlayerHealthService : IService
    {
        private readonly PlayerConfig _config;
        private int _currentHealth;

        public PlayerHealthService(PlayerConfig config)
        {
            _config = config;
        }

        public void OnInit()
        {
            _currentHealth = _config.MaxHealth;
            
            EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }
        
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (e.CurrentState == GameState.Playing)
            {
                _currentHealth = _config.MaxHealth;
                EventBus<PlayerHealthChangedEvent>.Publish(new PlayerHealthChangedEvent(_currentHealth, _config.MaxHealth));
            }
        }
        
        private void OnPlayerDamaged(PlayerDamagedEvent e)
        {
            if (_currentHealth <= 0) return; // Already dead

            _currentHealth -= e.DamageAmount;

            EventBus<PlayerHealthChangedEvent>.Publish(new PlayerHealthChangedEvent(_currentHealth, _config.MaxHealth));

            if (_currentHealth <= 0)
            {
                EventBus<PlayerDiedEvent>.Publish(new PlayerDiedEvent());
            }
        }

        public void OnDispose()
        {
            EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
        }
    }
}