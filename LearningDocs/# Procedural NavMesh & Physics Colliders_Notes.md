# Procedural NavMesh & Physics Colliders_Notes.md

## The Core Concept: Boolean Geometry vs. Voxelization
*   **Boolean Geometry (The Hard Way):** Using C# to mathematically subtract one polygon (an obstacle) from another (the floor) to create a single mesh with a hole. Incredibly difficult to write and CPU-intensive.
*   **Voxelization (The Unity Way):** Unity's `NavMeshSurface` doesn't do polygon math. It drops thousands of tiny virtual cubes (voxels) from the sky. If a voxel hits a flat floor, it marks it "Walkable". If it hits a `BoxCollider`, it marks it "Not Walkable".

## The "Invisible Cookie-Cutter" Technique
To cut a hole in a procedural NavMesh without altering the floor mesh:
1. Calculate the Bounding Box (Min/Max X and Z) of the obstacle.
2. Spawn a `GameObject` with a `BoxCollider` sized to those bounds.
3. Disable the `MeshRenderer` so it is invisible to the player.
4. Call `NavMeshSurface.BuildNavMesh()`. The AI C++ engine will automatically detect the physics collider and cut a perfect pathfinding hole around your furniture.




Instead of writing thousands of lines of complex C# math to calculate polygon intersections and cut holes into our floor mesh, we simply spawn an invisible BoxCollider exactly where the couch is.
Unity's NavMeshSurface runs on a highly optimized C++ engine. When we tell it to .BuildNavMesh(), it looks at our solid floor mesh, looks at the invisible BoxCollider sitting on top of it, and automatically carves a perfect pathfinding hole around the collider.
It takes zero math on our part, and it works flawlessly!