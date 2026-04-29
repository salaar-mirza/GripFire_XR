using UnityEngine;
using UnityEngine.AI;

namespace ARFps.Features.Enemy
{
    /// <summary>
    /// The visual and physical representation of a Swarm Enemy.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class SwarmEnemyView : MonoBehaviour
    {
        public Collider EnemyCollider;
        public MeshRenderer EnemyRenderer;
        public NavMeshAgent Agent { get; private set; }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
        }
    }
}