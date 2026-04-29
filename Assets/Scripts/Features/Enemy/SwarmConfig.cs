using UnityEngine;

namespace ARFps.Features.Enemy
{
    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "ARFps/Features/Enemy/SwarmConfig")]
    public class SwarmConfig : ScriptableObject
    {
        [Header("Prefabs")]
        public GameObject AntHillPrefab;
        public GameObject SwarmEnemyPrefab;

         
        [Header("Enemy Stats")]
        public int MaxHealth = 100;
        public Material HitFlashMaterial;
        public float HitFlashDuration = 0.1f;

        [Header("Attack Settings")] 
        public int AttackDamage = 10;
        public float AttackRateSeconds = 1.0f; //Bites once per second
        
        [Header("Spawning Rules")]
        public float SpawnIntervalSeconds = 2.0f;
        public float SwarmSpeed = 0.5f;
        public float SwarmRadius = 0.15f; // Controls the "Personal Space"
    }
}