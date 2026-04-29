using UnityEngine;
using ARFps.Core.Audio.Events;
using ARFps.Core.Vfx.Events;
using ARFps.Core.Events;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// The pure C# logic controller for a single Bouncing Ball.
    /// </summary>
    public class SandboxBallController
    {
        private readonly SandboxConfig _config;
        public SandboxBallView View { get; }

        public SandboxBallController(SandboxConfig config, SandboxBallView view)
        {
            _config = config;
            View = view;
            View.OnBallCollision += HandleCollision;
        }

        /// <summary>
        /// Resets the ball's state and launches it with a calculated velocity.
        /// </summary>
        public void Launch(Vector3 startPosition, Vector3 initialVelocity)
        {
            View.gameObject.SetActive(true);
            View.transform.position = startPosition;
            View.Rb.linearVelocity = Vector3.zero;
            View.Rb.angularVelocity = Vector3.zero;
            
            View.Rb.AddForce(initialVelocity, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Called by the central SandboxEntityService every frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
             // No lifetime tracking needed! The ball exists until shot by the player.
        }
        
        public void SetGrabbed(bool isGrabbed)
        {
            View.Rb.useGravity = !isGrabbed;
            if (isGrabbed)
            {
                View.Rb.linearVelocity = Vector3.zero;
                View.Rb.angularVelocity = Vector3.zero;
                
                // Instantly pop the ball off the ground to break any physical friction!
                View.transform.position += Vector3.up * 0.15f;
            }
        }
        
        private void HandleCollision(Collision collision)
        {
            // Check if the object we hit belongs to a Sandbox Fan (handles both the base and the child blade!)
            if (collision.gameObject.GetComponentInParent<SandboxFanView>() != null)
            {
                // 1. Calculate the outward direction away from the exact point of impact
                Vector3 burstDirection = (View.transform.position - collision.contacts[0].point).normalized;
                
                // 2. Add a random twist to the vector for maximum unpredictability and chaos!
                burstDirection += Random.insideUnitSphere * 0.5f;
                burstDirection.Normalize();
                
                // 3. Apply an explosive burst of force!
                View.Rb.AddForce(burstDirection * _config.BallChaoticBounceForce, ForceMode.Impulse);
            }
            
            // Publish a generic bounce sound every time it hits ANYTHING (walls, floors, fans)
            EventBus<PlaySfxEvent>.Publish(new PlaySfxEvent(SfxType.BallBounce, View.transform.position));
            EventBus<PlayVfxEvent>.Publish(new PlayVfxEvent(VfxType.BallBounceDust, View.transform.position, collision.contacts[0].normal));
        }
    }
}