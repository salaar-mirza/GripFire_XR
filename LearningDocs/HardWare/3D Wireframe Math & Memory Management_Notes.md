# 3D Wireframe Math & Memory Management_Notes.md

## 3D Vector Projection (The Math)
To extrude a 2D floor polygon into a 3D volume, you project the vertices along the Up axis.
*   **Formula:** `Vector3 topCorner = new Vector3(bottomCorner.x, bottomCorner.y + height, bottomCorner.z);`
*   **Why it's fast:** This is basic scalar addition. It requires almost zero CPU cycles compared to complex matrix multiplication or Quaternion rotations.

## Managing Dynamic Lines (The Memory)
A `LineRenderer` is a heavy Unity component. Constantly calling `AddComponent<LineRenderer>()` and `Destroy()` during gameplay violates **RULE 5** and causes fatal lag spikes.
*   **The Solution:** Use `ObjectPool<LineRenderer>`. 
*   **The Topology Strategy:** We use two permanent `LineRenderer` components for the live "Rubber Band" loops (one for the floor, one for the ceiling). We use the Object Pool exclusively for the vertical pillars that connect the floor to the ceiling.

## The Loop Property
To connect the last point back to the first point, DO NOT duplicate the first coordinate at the end of the array. Set `LineRenderer.loop = true`. The GPU will automatically draw the closing segment, saving memory and processing time.