using System;
using UnityEngine;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// The "dumb" visual and physical representation of a Bouncing Ball.
    /// Contains no logic. Adheres to RULE 1.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class SandboxBallView : MonoBehaviour
    {
        public Rigidbody Rb { get; private set; }
        public SphereCollider Collider { get; private set; }
        
        public event Action<Collision> OnBallCollision;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Collider = GetComponent<SphereCollider>();
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            OnBallCollision?.Invoke(collision);
        }
    }
}