using System;
using UnityEngine;

namespace ARFps.Features.Sandbox
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class SandboxBalloonView : MonoBehaviour
    {
        public Rigidbody Rb { get; private set; }
        public SphereCollider Collider { get; private set; }

        public event Action<Collision> OnBalloonCollision;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Collider = GetComponent<SphereCollider>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnBalloonCollision?.Invoke(collision);
        }
    }
}