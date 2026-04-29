using UnityEngine;

namespace ARFps.Features.Sandbox
{
    public class SandboxSmokeController
    {
        private readonly SandboxConfig _config;
        public SandboxSmokeView View { get; }
        
        public float LifeTime { get; private set; }
        public bool IsActive => LifeTime > 0;

        public SandboxSmokeController(SandboxConfig config, SandboxSmokeView view)
        {
            _config = config;
            View = view;
            View.OnSmokeCollision += HandleCollision;
        }

        public void Launch(Vector3 startPosition, Vector3 velocity)
        {
            View.gameObject.SetActive(true);
            View.transform.position = startPosition;
            View.Rb.linearVelocity = velocity;
            View.Rb.angularVelocity = Vector3.zero;

            View.Particles.Play(true);
            LifeTime = _config.SmokeDuration;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive) return;

            LifeTime -= deltaTime;
            
            // Once the lifetime expires, stop emitting new smoke.
            if (!IsActive) View.Particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void HandleCollision(Collision collision)
        {
            // Check if the object we hit belongs to a Sandbox Fan (handles both base and blade)
            if (collision.gameObject.GetComponentInParent<SandboxFanView>() != null)
            {
                // Calculate outward direction away from impact and add random chaotic twist
                Vector3 burstDirection = (View.transform.position - collision.contacts[0].point).normalized;
                burstDirection += Random.insideUnitSphere * 0.5f;
                burstDirection.Normalize();

                View.Rb.AddForce(burstDirection * _config.SmokeChaoticBounceForce, ForceMode.Impulse);
            }
        }
    }
}