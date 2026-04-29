# AI Pathfinding & Spatial Projection_Notes.md

## The Core Concept: Target Projection
A `NavMeshAgent` operates strictly on a 2D plane (the baked NavMesh) mapped into 3D space. 
*   **The Trap:** If you pass a 3D coordinate floating in the air (`Camera.main.transform.position`), the A* algorithm often fails to project it down to the NavMesh, causing the agent to freeze.
*   **The Solution:** Always flatten your target's Y-coordinate to match the NavMesh floor before calling `SetDestination()`. This is called **Vector Projection**.

## NavMesh Agent Geometry Constraints
When baking a procedural NavMesh, the surface generation is entirely dictated by the Agent's physical constraints:
*   **Step Height:** If an obstacle is shorter than this value, the NavMesh will bake a ramp *over* it. To treat obstacles as walls, `Step Height` must be lower than your shortest obstacle.
*   **Radius (Obstacle Avoidance):** Defines the "Personal Space" of the agent. A larger radius prevents agents from clipping into walls and forces them to spread out when swarming. Setting this via code (`agent.radius`) ensures designers can tweak it dynamically without touching Prefabs.


1.
The Floating Phone: You correctly deduced that we must intercept the _playerTransform.position, store it in a local Vector3, and overwrite the Y value to 0f (or the floor's height) so the Agent stays glued to the ground.
2.
The Ramps: You perfectly identified that Step Height is the culprit in the NavMesh baking settings. Lowering it forces the Baker to recognize the coffee table as a wall instead of a staircase.
3.
Personal Space: You correctly identified that the Radius property on the NavMeshAgent dictates its physical avoidance boundary, and overwriting it via the Config is the perfect data-driven solution.