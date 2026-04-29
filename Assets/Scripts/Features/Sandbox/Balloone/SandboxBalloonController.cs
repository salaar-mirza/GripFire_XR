using UnityEngine;

namespace ARFps.Features.Sandbox
{
    public class SandboxBalloonController
    {
        private readonly SandboxConfig _config;
        public SandboxBalloonView View { get; }
        
        private bool _isGrabbed = false;

        public SandboxBalloonController(SandboxConfig config, SandboxBalloonView view)
        {
            _config = config;
            View = view;
            View.OnBalloonCollision += HandleCollision;
        }

        public void Launch(Vector3 startPosition, Vector3 initialVelocity)
        {
            View.gameObject.SetActive(true);
            View.transform.position = startPosition;
            View.Rb.linearVelocity = initialVelocity; // A slight push outward
            // Add random spin for visual feedback of being blown through the air!
            View.Rb.angularVelocity = Random.insideUnitSphere * 5f;
            
            _isGrabbed = false;
        }

        public void SetGrabbed(bool isGrabbed)
        {
            _isGrabbed = isGrabbed;
            if (isGrabbed)
            {
                View.Rb.linearVelocity = Vector3.zero;
                View.Rb.angularVelocity = Vector3.zero;
            }
        }

        public void Tick(float deltaTime)
        {
            // Apply constant anti-gravity upward force to simulate helium!
            if (!_isGrabbed) View.Rb.AddForce(Vector3.up * _config.BalloonFloatForce * deltaTime, ForceMode.VelocityChange);
        }

        private void HandleCollision(Collision collision)
        {
            // Bounce off Sandbox Fans (handles both the base and the child blade)
            if (collision.gameObject.GetComponentInParent<SandboxFanView>() != null)
            {
                // Calculate outward direction away from impact, heavily biased downwards for a "swat" effect
                Vector3 burstDirection = (View.transform.position - collision.contacts[0].point).normalized;
                
                burstDirection.y -= 0.8f; 
                burstDirection += Random.insideUnitSphere * 0.4f;
                burstDirection.Normalize();
                
                View.Rb.AddForce(burstDirection * _config.BalloonFanBounceForce, ForceMode.Impulse);
            }
        }
    }
}