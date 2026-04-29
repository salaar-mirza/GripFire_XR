using UnityEngine;
using UnityEngine.AI;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// The visual and physical representation of a Sandbox Fan.
    /// Contains no logic. Adheres to RULE 1.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent), typeof(AudioSource))]
    public class SandboxFanView : MonoBehaviour
    {
        public Rigidbody Rb { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public AudioSource Audio { get; private set; }

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Agent = GetComponent<NavMeshAgent>();
            Audio = GetComponent<AudioSource>();
            Rb.isKinematic = true; // Crucial for batting balls away!
        }
    }
}