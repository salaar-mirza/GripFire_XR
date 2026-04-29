# NavMeshLinks & Spatial Queries_Notes.md

## The Core Concept: Connecting the 3D World
Standard NavMeshes are baked as flat surfaces. If you have verticality (like jumping on a couch), you create disconnected "islands" of pathfinding.
*   **NavMeshLink:** A Unity component that acts as a bridge between two points on different NavMesh surfaces. When an AI agent reaches the start of a link, it triggers a custom animation (like climbing or jumping) to traverse the gap to the end point.

## Safe Spawning (Voxel Validation)
Never trust pure random coordinates when spawning AI in a procedural world. Random coordinates can land inside geometry or off the map, breaking the `NavMeshAgent`.
*   **`NavMesh.SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int allowedAreas)`**
*   **How it works:** You give it a rough guess (sourcePosition). It checks the baked voxel grid and returns `true` if it finds a valid, walkable spot within your `maxDistance`. The `hit.position` contains the mathematically perfect, safe coordinate to drop your enemy.
*   **Big-O Performance:** It uses a spatial partitioning tree (like an Octree or BVH). It is very fast, but should still be used sparingly (don't call it 1,000 times in a single `Update` frame).



1.
NavMeshLink is the exact Unity component used to connect disconnected pathfinding islands (like the floor and a tabletop). It tells the AI, "You can jump or climb between these two points."
2.
NavMesh (specifically the NavMesh.SamplePosition method) is the exact API we use to ask Unity's C++ engine: "Here is a random coordinate. Please find the closest valid point on the pathfinding grid so I don't spawn an enemy inside a wall."