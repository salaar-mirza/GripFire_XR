using UnityEngine;
using TMPro;
namespace ARFps.Features.RoomMapping
{
    /// <summary>
    /// The visual representation of the targeting reticle.
    /// Exposes transform methods for the mapping service.
    /// </summary>
    public class FloorReticleView : MonoBehaviour
    {
        [SerializeField] private GameObject _visuals; // The child object containing the mesh
        [SerializeField] private LineRenderer _laserLine; // The laser beam
        [SerializeField] private TextMeshPro _liveMeasurementText; // The floating text next to the sphere
        private float? _cachedHalfHeight;

        public void Show()
        {
            _visuals.SetActive(true);
            if (_laserLine != null) 
            {
                _laserLine.enabled = true;
                _laserLine.positionCount = 2; // Enforce exactly 2 points (start and end)
                _laserLine.useWorldSpace = true; // Enforce world coordinates
            }
        }
        
        public void Hide()
        {
            _visuals.SetActive(false);
            if (_laserLine != null) _laserLine.enabled = false;
        }
        
        public void UpdateLiveMeasurement(float distance)
        {
            if (_liveMeasurementText != null)
            {
                bool showText = distance > 0.01f;
                _liveMeasurementText.gameObject.SetActive(showText);
                if (showText) _liveMeasurementText.text = $"{distance:F2}m";
            }
        }


        public void SetPose(Vector3 position, Quaternion rotation, Vector3 cameraPosition)
        {
            float yOffset = GetHalfHeight();
            Vector3 adjustedPosition = position + new Vector3(0, yOffset, 0);

            transform.SetPositionAndRotation(adjustedPosition, rotation);
            
            // Draw the laser from slightly below the camera (like holding a gun/pointer) to the floor dot
            if (_laserLine != null)
            {
                Vector3 laserStartPos = cameraPosition + (Vector3.down * 0.2f);
                _laserLine.SetPosition(0, laserStartPos);
                _laserLine.SetPosition(1, adjustedPosition);
            }

            // Billboard the live measurement text so it perfectly faces the camera at all times
            if (_liveMeasurementText != null && _liveMeasurementText.gameObject.activeSelf)
            {
                // By pointing the text's "forward" vector exactly away from the camera, the front of the text faces the player
                _liveMeasurementText.transform.rotation = Quaternion.LookRotation(_liveMeasurementText.transform.position - cameraPosition);
            }
        }

        private float GetHalfHeight()
        {
            if (_cachedHalfHeight.HasValue) return _cachedHalfHeight.Value;
            
            if (_visuals != null)
            {
                if (_visuals.TryGetComponent<Renderer>(out var renderer)) 
                    _cachedHalfHeight = renderer.bounds.extents.y;
                else if (_visuals.TryGetComponent<Collider>(out var collider)) 
                    _cachedHalfHeight = collider.bounds.extents.y;
                else
                    _cachedHalfHeight = 0f;
            }
            else _cachedHalfHeight = 0f;
            
            return _cachedHalfHeight.Value;
        }
    }
}