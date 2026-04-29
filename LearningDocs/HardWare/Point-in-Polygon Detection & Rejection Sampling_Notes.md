# Point-in-Polygon Detection & Rejection Sampling_Notes.md (This is a classic FAANG interview question)

## The Core Concept: Spawning Inside Boundaries
When a game area is an irregular polygon (not a simple rectangle or circle), standard random coordinate generation `(Random.Range)` will often place entities outside the playable area or inside obstacles.

## The Algorithm: Ray Casting (Even-Odd Rule)
To determine if a 2D point `(X, Z)` is strictly inside a polygon:
1. Draw a horizontal "ray" from the point extending infinitely to the right.
2. Count how many times that ray intersects the line segments (walls) of the polygon.
3. **The Rule:** If the intersection count is **ODD** (1, 3, 5), the point is INSIDE. If the count is **EVEN** (0, 2, 4), the point is OUTSIDE.

## The Implementation: Rejection Sampling
Because calculating exact internal polygon geometry is highly complex, the industry standard for spawning is:
1. Calculate the absolute Bounding Box (Min/Max X and Z) of the entire polygon.
2. Generate a random point inside that simple box.
3. Run the Even-Odd Ray Cast algorithm on that point.
4. If EVEN (Outside), discard the point and try again. If ODD (Inside), accept the point and spawn the entity.

## CPU Architecture Note
This algorithm requires heavy math (division and multiplication for line-intersection logic) running inside a `while` loop. If checking 1,000 points, this MUST be offloaded to a background Worker Thread (`Task.Run`) to prevent Main Thread frame drops (The Thermal Wall / Main Thread Dictator rule).