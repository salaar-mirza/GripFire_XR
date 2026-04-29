using UnityEngine;

namespace ARFps.Features.Weapon
{
    /// <summary>
    /// Immutable configuration data defining weapon behavior, damage, and pooling limits.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "ARFps/Features/Weapon/WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("Combat Stats")]
        [Tooltip("Damage dealt per bullet.")]
        public int Damage = 25;
        
        [Tooltip("Rounds fired per minute (RPM).")]
        public float FireRateRPM = 600f;
        
        [Tooltip("How fast the bullet travels in meters per second.")]
        public float BulletSpeed = 50f;
        
        [Tooltip("Maximum distance the bullet can travel before returning to the pool.")]
        public float MaxDistance = 100f;

        [Header("Memory & Pooling")]
        [Tooltip("How many bullets to pre-instantiate on startup to prevent lag.")]
        public int InitialPoolSize = 20;
        
        [Tooltip("The absolute maximum number of bullets allowed in the pool.")]
        public int MaxPoolSize = 50;
        
        [Header("Prefabs")]
        [Tooltip("The visual prefab spawned when a bullet is fired.")]
        public BulletView BulletPrefab;
    }
}