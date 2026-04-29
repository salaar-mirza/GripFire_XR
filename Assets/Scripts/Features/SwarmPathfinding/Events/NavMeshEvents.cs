using ARFps.Core.Events;
using ARFps.Features.RoomMapping;
using System.Collections.Generic;
using UnityEngine;

namespace ARFps.Features.SwarmPathfinding.Events
{
    
    /// <summary>
    /// Published by the background thread when the procedural floor mesh math is complete.
    /// Contains only thread-safe primitive arrays.
    /// </summary>
    public readonly struct FloorMathCalculatedEvent : IGameEvent
    {
        public readonly Vector3[] Vertices;
        public readonly int[] Triangles;
        public readonly Transform MasterOrigin;
        public readonly List<ObstacleData> Obstacles;
        public readonly float CeilingHeight;

        public FloorMathCalculatedEvent(Vector3[] vertices, int[] triangles, Transform masterOrigin, List<ObstacleData> obstacles, float ceilingHeight)
        {
            Vertices = vertices;
            Triangles = triangles;
            MasterOrigin = masterOrigin;
            Obstacles = obstacles;
            CeilingHeight = ceilingHeight;
        }
    }
    
       
    /// <summary>
    /// Published by the NavMeshBuilderService AFTER the Unity NavMeshSurface has successfully baked.
    /// </summary>
    public readonly struct NavMeshBakedEvent : IGameEvent { }
    
}