using System;
using System.Collections.Generic;
using ARFps.Features.Enemy;
using UnityEngine;

namespace ARFps.Features.LevelDirector
{
    /// <summary>
    /// Immutable configuration data for a complete level, defining the sequence of enemy waves.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLevelConfig", menuName = "ARFps/Features/LevelDirector/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public List<Wave> Waves;
    }

    /// <summary>
    /// A serializable data structure representing a single wave of enemies.
    /// </summary>
    [Serializable]
    public class Wave
    {
        [Tooltip("The type of target to spawn in this wave (e.g., SuspectConfig, HostageConfig).")]
        public SwarmConfig TargetType;
        
        [Tooltip("The number of targets to spawn in this wave.")]
        public int TargetCount = 1;
        
        [Tooltip("The delay in seconds after the previous wave is cleared before this wave begins.")]
        public float DelayBeforeWave = 2.0f;
    }
}