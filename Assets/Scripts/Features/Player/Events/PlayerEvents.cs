using ARFps.Core.Events;

namespace ARFps.Features.Player.Events
{
    /// <summary>
    /// Published when the player takes damage from an enemy or hazard.
    /// </summary>
    public readonly struct PlayerDamagedEvent : IGameEvent
    {
        public readonly int DamageAmount;

        public PlayerDamagedEvent(int damageAmount)
        {
            DamageAmount = damageAmount;
        }
    }

    /// <summary>
    /// Published when the player's health drops to 0.
    /// </summary>
    public readonly struct PlayerDiedEvent : IGameEvent { }

    /// <summary>
    /// Published whenever the player's health value changes (e.g., taking damage, resetting on new game).
    /// </summary>
    public readonly struct PlayerHealthChangedEvent : IGameEvent
    {
        public readonly int CurrentHealth;
        public readonly int MaxHealth;

        public PlayerHealthChangedEvent(int currentHealth, int maxHealth)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
}