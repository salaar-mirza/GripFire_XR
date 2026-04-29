using UnityEngine;

namespace ARFps.Features.RoomMapping
{
    [CreateAssetMenu(fileName = "ManualRoomMappingConfig", menuName = "ARFps/Features/RoomMapping/ManualRoomMappingConfig")]
    public class ManualRoomMappingConfig : ScriptableObject
    {
        [Tooltip("The minimum number of corner points the player must define to create a valid room.")]
        public int MinCornersRequired = 3;
           
        [Header("Visuals")]
        [Tooltip("The prefab spawned to show the player where a corner has been placed.")]
        public GameObject BoundaryMarkerPrefab;
        
        [Tooltip("The prefab spawned for obstacle corners (should be a different color).")]
        public GameObject ObstacleMarkerPrefab;
         
        [Tooltip("The TextMeshPro prefab spawned at the midpoint of walls to show length.")]
        public GameObject MeasurementTextPrefab;
    }
}