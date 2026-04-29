using ARFps.Core.Events;
using UnityEngine;

namespace ARFps.Features.Enemy.Events
{
    /// <summary>
    /// Published when a target's health reaches zero.
    /// </summary>
    public readonly struct SwarmEnemyDestroyedEvent : IGameEvent
    {
        public readonly SwarmEnemyView View;
        
        public SwarmEnemyDestroyedEvent(SwarmEnemyView view) => View = view;
    }
    
}