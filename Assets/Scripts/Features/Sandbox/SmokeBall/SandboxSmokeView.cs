using System;
using UnityEngine;

namespace ARFps.Features.Sandbox
{
    [RequireComponent(typeof(Rigidbody), typeof(ParticleSystem), typeof(SphereCollider))]
    public class SandboxSmokeView : MonoBehaviour
    {
        public Rigidbody Rb { get; private set; }
        public ParticleSystem Particles { get; private set; }
        public SphereCollider Collider { get; private set; }
        
        public event Action<Collision> OnSmokeCollision; 

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Particles = GetComponent<ParticleSystem>();
            Collider = GetComponent<SphereCollider>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnSmokeCollision?.Invoke(collision);
        }
    }
}