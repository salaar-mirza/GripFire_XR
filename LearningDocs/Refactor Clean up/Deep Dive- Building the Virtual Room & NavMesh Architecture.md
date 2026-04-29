# 🧠 Deep Dive: Building the Virtual Room & NavMesh Architecture

## 🎯 1. Why do we need a NavMesh and a Virtual Room?
In an Augmented Reality game, the physical world (your living room) and the digital world (the game) do not naturally interact. 

*   **The AI Problem:** Digital enemies (like Swarm Ants) cannot "see" your physical couch. They need a digital roadmap to know where they can walk and what they must avoid. This roadmap is called a **NavMesh (Navigation Mesh)**.
*   **The Physics Problem:** If you drop a digital bouncy ball into AR, it will fall straight through the camera into the void because AR planes aren't solid physics objects. 
*   **The Solution:** We must mathematically construct a 1:1 invisible 3D replica of your living room—complete with a solid floor, walls, and obstacle blockers. We call this the **Virtual Room**.

---

## 🏗️ 2. The Master Architect: `NavMeshBuilderService`
The `NavMeshBuilderService` is the "Conductor" of this entire operation. It listens for the `PlayableAreaDefinedEvent` (which fires when the player finishes mapping the room) and orchestrates the construction.

It is arguably one of the most important scripts in the game because it handles **Multithreading** and **Drift Prevention**.

### Preventing AR Drift (Local Space Conversion)
Before doing any math, the service converts every single world coordinate into **Local Space** relative to the `MasterOrigin` AR Anchor:


*Why?* If the AR tracking hardware shifts or corrects itself, the AR Anchor will move. Because our 3D room is built using coordinates *relative* to that anchor, the entire digital room will perfectly shift alongside the real-world room, completely eliminating AR drift!

### The Main Thread Bottleneck
Calculating 3D geometry is heavy on the CPU. If we did this on Unity's main thread, the AR camera feed would temporarily freeze. 
To prevent this, the service uses `Task.Run(() => GenerateFloorMeshAsync(...))` to offload the heavy math to a background processor thread, keeping the game running at a silky smooth 60+ FPS.

---

## 🧮 3. Generating the Floor (The Math)
Inside the background thread, we must create a Unity `Mesh`. A mesh requires **Vertices** (corners) and **Triangles** (connecting the corners into a solid surface).

### The "Double-Sided Fan Triangulation"
Because a human player can walk around their room clockwise OR counter-clockwise while mapping, we don't know which way the 3D faces will point. If we guess wrong, the floor will be invisible and broken.
*   **The Fix:** We draw a "Triangle Fan" connecting Point 0 to every other point. We draw it twice—once facing UP, and once facing DOWN. 
*   This mathematical brute-force guarantees that no matter how the player mapped the room, the Unity physics engine will successfully register a solid, watertight floor.

Once the math is done, the data is packed into a `readonly struct` (`FloorMathCalculatedEvent`) and shipped safely back to the Main Thread.

---

## 🧱 4. Constructing the 3D Room: `VirtualRoomView`
Once the Main Thread receives the math, the `VirtualRoomView` script physically builds the arena.

### The Floor
The generated vertices and triangles are plugged into a `MeshFilter` and a **`MeshCollider`**. 
We also apply a `_floorOffset` (lifting it by roughly 1-5 centimeters) so our digital floor doesn't graphically clip into the physical AR planes (Z-fighting). The room now has a solid bottom!

### The Invisible Walls
To prevent AI from walking out of your house, and to trap bouncing balls, the script dynamically spawns invisible walls using basic primitives (`BoxCollider`):
1.  **Midpoint:** `(pointA + pointB) / 2f` (This calculates exactly where to place the wall).
2.  **Distance:** `Vector3.Distance(pointA, pointB)` (This dictates how long the wall needs to be).
3.  **Rotation:** `Quaternion.LookRotation(pointB - pointA)` (This perfectly angles the wall to line up with your physical walls).
It scales the wall upward to match the `CeilingHeight`. The room is now boxed in!

### The Obstacle Blockers
If the player mapped a table, the script calculates a "Bounding Box" around the table's corners (finding the Min/Max X and Z coordinates). It spawns an invisible `BoxCollider` over the physical table.

---

## 🤖 5. Baking the NavMesh (The Final Step)
At this stage, we have a fully functioning, physics-based 3D room. It has a floor, walls, and obstacle blocks, but **it is completely invisible** because we only added physics colliders, not renderers.

Finally, `VirtualRoomView` tells Unity's `NavMeshSurface` to bake the pathfinding data: