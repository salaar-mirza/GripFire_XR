using UnityEngine;
using UnityEngine.AI;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// The pure C# logic controller for a Sandbox Fan.
    /// </summary>
    public class SandboxFanController
    {
        public enum FanMovementType { FloorRoam, CeilingRoam, CeilingAscend }
        
        private readonly SandboxConfig _config;
        public SandboxFanView View { get; }

        private FanMovementType _movementType;
        private Vector3 _currentDirection;
        private float _targetCeilingY;

        public SandboxFanController(SandboxConfig config, SandboxFanView view)
        {
            _config = config;
            View = view;
        }

        public void Reset(Vector3 startPosition, FanMovementType moveType, float targetY = 0f)
        {
            _movementType = moveType;
            View.Agent.enabled = false; // Disable agent before warping
            View.transform.position = startPosition;
            View.transform.rotation = Quaternion.identity;
            View.Agent.updateRotation = false; // Stop the NavMeshAgent from fighting our spinning physics

            // Start the continuous mechanical whirring sound!
            if (View.Audio != null && _config.FanWhirClip != null)
            {
                View.Audio.clip = _config.FanWhirClip;
                View.Audio.loop = true;
                if (!View.Audio.isPlaying) View.Audio.Play();
            }

            if (_movementType == FanMovementType.FloorRoam)
            {
                SetRandomRoamDirection();
            }
            else if (_movementType == FanMovementType.CeilingAscend)
            {
                _targetCeilingY = targetY;
            }
            else if (_movementType == FanMovementType.CeilingRoam)
            {
                SetRandomRoamDirection();
            }
        }

        public void Tick(float deltaTime)
        {
            // 1. Violent Spinning
            View.transform.Rotate(0, _config.FanSpinSpeed * deltaTime, 0, Space.Self);

            // 2. Roaming
            if (_movementType == FanMovementType.CeilingAscend)
            {
                // Fly straight up to the ceiling like a launching drone!
                Vector3 targetPos = new Vector3(View.transform.position.x, _targetCeilingY, View.transform.position.z);
                View.transform.position = Vector3.MoveTowards(View.transform.position, targetPos, _config.FanMoveSpeed * deltaTime);
                
                if (Vector3.Distance(View.transform.position, targetPos) < 0.01f)
                {
                    // Reached the top! Switch to bouncing mode.
                    _movementType = FanMovementType.CeilingRoam;
                    SetRandomRoamDirection();
                }
            }
            else if (_movementType == FanMovementType.CeilingRoam || _movementType == FanMovementType.FloorRoam)
            {
                // Use a Raycast to detect walls, preventing blind spots near geometry
                if (Physics.Raycast(View.transform.position, _currentDirection, out RaycastHit hit, _config.FanMoveSpeed * deltaTime + 0.1f))
                {
                    // Ignore our own blade collisions and only bounce off vertical surfaces (walls)
                    if (!hit.transform.IsChildOf(View.transform) && Mathf.Abs(hit.normal.y) < 0.5f)
                    {
                        // Only bounce if actively flying TOWARDS the wall to prevent tunneling
                        if (Vector3.Dot(_currentDirection, hit.normal) < 0)
                        {
                            // Reflect off the wall, and add a random twist so they don't get stuck in parallel loops
                            _currentDirection = Vector3.Reflect(_currentDirection, hit.normal);
                            _currentDirection = Quaternion.Euler(0, Random.Range(-25f, 25f), 0) * _currentDirection;
                            _currentDirection.y = 0; // Keep movement strictly horizontal
                            _currentDirection.Normalize();
                        }
                    }
                }

                View.transform.position += _currentDirection * _config.FanMoveSpeed * deltaTime;
            }
        }

        private void SetRandomRoamDirection()
        {
            _currentDirection = new Vector3(Random.value - 0.5f, 0, Random.value - 0.5f);
            if (_currentDirection.sqrMagnitude < 0.01f) _currentDirection = Vector3.forward; // Failsafe
            _currentDirection.Normalize();
        }
    }
}