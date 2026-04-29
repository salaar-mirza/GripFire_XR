using System.Collections.Generic;
using ARFps.Core.Events;
using UnityEngine;

namespace ARFps.Features.RoomMapping.Events
{
    /// <summary>
    /// Published when the player sets the floor origin anchor.
    /// </summary>
    public readonly struct FloorOriginSetEvent : IGameEvent
    {
        public readonly Vector3 OriginPosition;
        public readonly Transform MasterOrigin;
        public FloorOriginSetEvent(Vector3 originPosition, Transform masterOrigin) { OriginPosition = originPosition; MasterOrigin = masterOrigin; }
    }
     
    /// <summary>
    /// Published when the player undoes the floor origin.
    /// </summary>
    public readonly struct FloorOriginUndoneEvent : IGameEvent { }
    
    /// <summary>
    /// Published when the player sets the ceiling height.
    /// </summary>
    public readonly struct CeilingHeightSetEvent : IGameEvent 
    { 
        public readonly Vector3 CeilingPoint; 
        public CeilingHeightSetEvent(Vector3 ceilingPoint) { CeilingPoint = ceilingPoint; }
    }

     
    /// <summary>
    /// Published when the player undoes the ceiling height.
    /// </summary>
    public readonly struct CeilingHeightUndoneEvent : IGameEvent { }
    
    /// <summary>
    /// Published each time the player adds or re-adds a boundary corner point.
    /// </summary>
    public readonly struct BoundaryPointAddedEvent : IGameEvent 
    { 
        public readonly Vector3 NewPoint; 
        public readonly Transform ParentAnchor; 
        public BoundaryPointAddedEvent(Vector3 newPoint, Transform parentAnchor) { NewPoint = newPoint; ParentAnchor = parentAnchor; }
    }
    /// <summary>
    /// Published when the player undoes a boundary point.
    /// </summary>
    public readonly struct BoundaryPointRemovedEvent : IGameEvent 
    { 
        public readonly Vector3 RemovedPoint; 
        public BoundaryPointRemovedEvent(Vector3 removedPoint) { RemovedPoint = removedPoint; }
    }

     
    public readonly struct ObstacleHeightSetEvent : IGameEvent 
    { 
        public readonly Vector3 CeilingPoint; 
        public ObstacleHeightSetEvent(Vector3 ceilingPoint) { CeilingPoint = ceilingPoint; }
    }
    public readonly struct ObstacleHeightUndoneEvent : IGameEvent { }
     
    public readonly struct ObstaclePointAddedEvent : IGameEvent 
    { 
        public readonly Vector3 NewPoint; 
        public readonly Transform ParentAnchor; 
        public ObstaclePointAddedEvent(Vector3 newPoint, Transform parentAnchor) { NewPoint = newPoint; ParentAnchor = parentAnchor; }
    }
    public readonly struct ObstaclePointRemovedEvent : IGameEvent 
    { 
        public readonly Vector3 RemovedPoint; 
        public ObstaclePointRemovedEvent(Vector3 removedPoint) { RemovedPoint = removedPoint; }
    }

     
    public readonly struct ObstacleMappingStartedEvent : IGameEvent { }
    public readonly struct RoomBoundariesRestoredEvent : IGameEvent { }
    public readonly struct ObstacleRestoredEvent : IGameEvent { }
    
    /// <summary>
    /// Published when the player finalizes the room boundaries.
    /// </summary>
    public readonly struct PlayableAreaDefinedEvent : IGameEvent
    {
        public readonly Transform MasterOrigin;
        public readonly List<Vector3> BoundaryPoints;
        public readonly float CeilingHeight;
        public readonly List<ObstacleData> Obstacles;

        public PlayableAreaDefinedEvent(Transform origin, List<Vector3> points, float height, List<ObstacleData> obstacles)
        {
            MasterOrigin = origin;
            BoundaryPoints = points;
            CeilingHeight = height;
            Obstacles = obstacles;
        }
    }
}