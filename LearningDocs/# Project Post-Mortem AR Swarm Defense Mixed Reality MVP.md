# Project Post-Mortem: AR Swarm Defense (Mixed Reality MVP)

## 1. Project Overview & Hardware Compromises
**Objective:** Build a scalable, high-performance Augmented Reality (AR) defense game for mobile devices where players physically map their living room, and AI enemies dynamically navigate around real-world furniture to attack them.

**The Hardware Compromise (Hand Tracking vs. Touch):**
*   **The Initial Goal:** We originally wanted native hand-tracking (pinching/shooting with bare hands).
*   **The Reality:** Standard mobile phone cameras lack the precise depth sensors (like LiDAR or IR arrays found on the Quest 3 or HoloLens) required for robust, jitter-free skeletal hand tracking. 
*   **The Pivot:** We pivoted to a screen-space touch raycast system. However, because we built a strictly decoupled architecture, the `WeaponService` doesn't care *how* it fires. If we port this game to a Quest 3 tomorrow, we simply swap the `TouchInputService` for a `HandTrackingService` that fires the exact same `WeaponFiredEvent`. Zero core game logic needs to change.

---

## 2. Core Architecture: MVC-S & The Event Bus
To prevent "Spaghetti Code" (where MonoBehaviours rigidly reference each other), the entire game is built on a strict **Model-View-Controller-Service (MVC-S)** pattern.

*   **Rule of Decoupling:** Services never directly reference other Services. They communicate entirely through a custom generic `EventBus<T>`.
*   **Struct Payloads:** All events are defined as `structs` (Value Types). Because structs live on the Stack (LIFO memory), passing data between systems generates **Zero Garbage Collection (GC)** overhead.
*   **The Game State Machine:** The macro-flow of the game (Scanning -> Playing -> GameOver) is managed by a `GameStateService`. Individual entities (like an Enemy) have Micro-State Machines (Spawning -> Chasing -> Attacking -> Dead) managed by their own pure C# Controllers.

---

## 3. Memory Management: Defeating the Garbage Collector
In mobile AR, frame drops break immersion and cause motion sickness. Instantiating and Destroying objects during gameplay triggers the Garbage Collector, causing fatal lag spikes.

*   **ITickable Interface:** Instead of using Unity's `Update()` or `Coroutines` (which generate garbage), all Services implement an `ITickable` interface driven by a single, central loop.
*   **Object Pooling:** Enemies, Bullets, and UI Markers are pre-instantiated and stored in `UnityEngine.Pool.ObjectPool`. They are set to active/inactive as needed.

### ⚠️ The "Dirty Pool" State Leak
*   **The Problem:** When an enemy died, it turned red and went back into the Object Pool. When pulled out later, the new Controller looked at the physical View, saw it was red, and saved "Red" as its baseline material. The enemy was permanently poisoned.
*   **The Solution:** Strict memory hygiene. The exact millisecond an entity dies, its material, physics velocity, and health must be mathematically scrubbed and reset to factory defaults *before* it is released back into the pool.

---

## 4. Augmented Reality Challenges: The Physical World

### ⚠️ The AR Drift Problem (Local vs. World Space)
*   **The Problem:** AR tracking is imperfect. As the camera moves, ARCore constantly adjusts its internal map, causing the `0,0,0` origin to shift. If we saved our room boundaries in absolute World Space (GPS coordinates), the virtual walls would "float" away from the physical room whenever the AR system corrected itself.
*   **The Solution:** The Master Origin Anchor. The moment the player taps the floor, we spawn an `ARAnchor`. We convert all mapped corners into **Local Space** (`InverseTransformPoint`) relative to that anchor. If the physical room tracking shifts 2 inches, the anchor shifts 2 inches, and all our local math flawlessly shifts with it.

### ⚠️ Z-Fighting (The Patchy Floor)
*   **The Problem:** We generated a visual green floor perfectly flush with the AR plane at `Y = 0`. Because two flat mathematical planes occupied the exact same depth buffer, the GPU panicked, causing flickering patches as the player moved their camera.
*   **The Solution:** A `0.05f` (5cm) Y-axis offset. By mathematically lifting the procedural floor slightly above the tracking plane, we completely eliminated depth buffer conflicts.

---

## 5. Procedural Geometry & Multi-Threading

To make the AI pathfind around real-world furniture, we had to generate a custom NavMesh at runtime. This required heavy triangulation math.

### ⚠️ The VRAM Leak Exception
*   **The Problem:** While we pool standard GameObjects, procedural meshes are unique to every living room. We cannot pool them. If the player remaps the room, we generate a `new Mesh()`. The C# Garbage Collector clears the CPU reference, but leaves the actual geometry sitting in the GPU's Video RAM (VRAM), eventually crashing the phone.
*   **The Solution:** We implemented a strict rule to call `UnityEngine.Object.Destroy(mesh)` on the old mesh before assigning the new one to prevent VRAM memory leaks.

### ⚠️ The Invisible Floor (Winding Order)
*   **The Problem:** When building the floor, if the player traced their room counter-clockwise, the math generated triangles facing downwards (into the earth). To save performance, the GPU uses **Backface Culling** to delete triangles facing away from the camera. The floor became invisible, and the NavMesh failed to bake because it thought it was looking at a ceiling.
*   **The Solution:** Double-Sided Fan Triangulation. We doubled the array size and generated every triangle twice—once clockwise, once counter-clockwise. This mathematically guaranteed the floor would render perfectly regardless of user behavior.

### Safe Threading Payload
*   Unity API calls (`Transforms`, `GameObjects`) cannot be passed to background threads. We serialized the room data into raw primitive arrays (`Vector3[]`, `int[]`) and passed them via `Task.Run` to generate the mesh with zero frame drops on the Main Thread.

---

## 6. AI & Procedural Spawning

### ⚠️ The Event Race Condition (Chef & Waiter)
*   **The Problem:** The background thread finished the math and fired `NavMeshGeneratedEvent`. Both the Builder (who bakes the pathfinding) and the Spawner (who spawns enemies) listened to it. The Spawner acted a millisecond faster and tried to drop enemies onto a NavMesh that hadn't been built yet, crashing the AI.
*   **The Solution:** Event Chaining. The background thread fires `FloorMathCalculatedEvent` (carrying the heavy array payload) exclusively to the Builder. Once the Builder safely executes the Unity API to bake the mesh, *it* fires a lightweight `NavMeshBakedEvent` to tell the Spawner to start the game.

### ⚠️ The Bounding Box Trap vs. Barycentric Math
*   **The Problem:** To spawn Ant Hills, we originally calculated an Axis-Aligned Bounding Box (AABB) around the room and picked a random X/Z point. If the room was L-shaped, the random point would often land outside the room in empty space, failing the spawn.
*   **The Solution:** Barycentric Coordinates. We used the raw Triangles array generated by the background thread. We pick a random triangle, generate two random floats ($r_1, r_2$), fold them if they exceed 1.0, and calculate a random point mathematically guaranteed to be $100\%$ inside the irregular polygon in $O(1)$ constant time.

### ⚠️ The Cookie Cutter (NavMeshObstacles)
*   Instead of writing insane Boolean Geometry math to cut polygon holes in our floor for coffee tables, we utilized Unity's PhysX pipeline. We spawned invisible `BoxColliders` over the furniture footprints. When `NavMeshSurface.BuildNavMesh()` is called, the C++ engine automatically detects the colliders and carves perfect pathfinding holes with zero C# overhead.

### AI Performance Throttling & Fast Math
*   **The Square Root Trap:** Checking distance between the player and an enemy using `Vector3.Distance` requires calculating a square root, which is highly CPU-intensive. We optimized this by comparing `sqrMagnitude` (squared distance) against a squared threshold, completely bypassing the square root operation.
*   **Tick Throttling:** Calling `NavMeshAgent.SetDestination` triggers the A* (A-Star) search algorithm. Doing this for 50 enemies every frame would melt a mobile CPU. We throttled the AI "brain" using a timer, forcing it to only recalculate its path twice per second, reducing CPU load by $96\%$.