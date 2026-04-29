# Advanced NavMesh Pathfinding Quirks_Notes.md

## Target Projection (The Floating Goal)
A `NavMeshAgent` cannot pathfind to a point floating in the air. If the target is a flying object or an AR camera, you must **Project** the target's (X, Z) coordinates down to the NavMesh's (Y) floor height before calling `SetDestination()`.

## NavMesh Baking Extents (Step Height & Slope)
When a `NavMeshSurface` bakes, it uses an Agent's parameters to define the geometry:
*   **Max Slope:** The steepest angle the AI can walk up.
*   **Step Height:** The tallest vertical ledge the AI can instantly step onto. 
*   **The Trap:** If you spawn a square `BoxCollider` (an obstacle), but its height is lower than the Agent's `Step Height`, the NavMesh will literally bake a walkable ramp over the obstacle instead of cutting a hole around it.

## AI Obstacle Avoidance (Personal Space)
`NavMeshAgent` has a built-in local avoidance system.
*   **`Radius`:** Defines the physical width of the agent. Increasing this forces agents to stay further apart from walls and each other.
*   **Avoidance Priority:** Dictates who moves out of the way. (0 = Most important, 99 = Least important).

The Diagnosis: Why the AI is Broken
1. The Floating Phone (Why they ignore you) Unity's NavMeshAgent is glued to the floor. When we tell it to SetDestination(Camera.main.transform.position), we are telling a ground-based bug to run to a point 1.5 meters in the sky. Because the destination is floating in the air, the A* Pathfinding algorithm says, "I can't fly there. Path invalid." and the enemy just stands still until you lower your phone to the floor.
2. The Invisible Ramps (Why they walk over obstacles) When you spawned the invisible BoxCollider over your coffee table, you expected the NavMesh to cut a hole. But instead, it built a ramp right over it! Why? Because Unity's NavMesh baker looks at your default Humanoid Agent Settings. It has a setting called Step Height. If your coffee table is 0.4 meters tall, and the Step Height is set to 0.5 meters, the Baker says, "Oh, the player can just step onto this table!" and drapes the pathfinding mesh right over the top of it like a tablecloth.
3. Personal Space (Why they clump together) Currently, the agents are all using a default Unity radius. If you spawn 10 of them, they will push into each other because their invisible physical boundaries are too small. 