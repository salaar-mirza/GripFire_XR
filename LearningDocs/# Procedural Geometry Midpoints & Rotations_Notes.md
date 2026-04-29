# Procedural Geometry: Midpoints & Rotations_Notes.md

## The Core Concept: Building Walls from Points
If a player draws a room by placing corners, you must procedurally generate physical walls (Colliders) between those corners to keep physics objects (bullets, physics props) inside the room.

## The Math (Position, Scale, Rotation)
For every two corners (Point A and Point B):
1.  **Position (The Midpoint):** `Vector3 midPoint = (A + B) / 2f;`
2.  **Scale (The Distance):** The length of the wall is exactly `Vector3.Distance(A, B)`. *(Note: Using `Distance` here is safe because we only calculate it ONCE during setup, not every frame).* We set the width to a thin `0.1f` and the height to the room's ceiling height.
3.  **Rotation (The Direction):** A 3D box needs to rotate to face the next corner perfectly. We find the directional vector (`B - A`) and feed it into Unity's built-in math function: `Quaternion.LookRotation(B - A)`.

## Big-O Performance
Spawning primitive `BoxColliders` during the loading/setup phase is highly optimized. Once spawned, Unity's C++ PhysX engine handles all collision detection with $O(1)$ time complexity, ensuring bullets are trapped inside the room with zero C# overhead.


1.
The Square Root Trap: You correctly identified that for a 10-meter range, you compare sqrMagnitude against 100 ($10 \times 10$). The CPU skips the square root, and your game runs lightning fast.
2.
The Midpoint Math: You correctly deduced the fundamental formula for a centroid/midpoint. To find the exact middle of two points, you add them together and divide by 2! Vector3 midpoint = (PointA + PointB) / 2f;