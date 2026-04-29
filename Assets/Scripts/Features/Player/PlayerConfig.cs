using UnityEngine;

namespace ARFps.Features.Player
{
    /// <summary>
    /// Immutable configuration data for the player's base stats.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "ARFps/Features/Player/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("Health Settings")]
        public int MaxHealth = 100;
    }
}