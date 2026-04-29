# MR Room Mapping & Spatial Architecture Guide
**Framework:** Unity 6, AR Foundation, URP
**Architecture:** Strict MVC-S (Model, View, Controller, Service Locator, Event Bus)

## 1. System Overview: The "Hardware-Agnostic" AR Console
High-end mixed reality devices (HoloLens, Quest 3, iPhone Pro) use LiDAR/Depth sensors to automatically mesh a room. However, 90% of mobile phones lack this hardware. 

**Our Solution:** We built a manual, player-driven mapping system. By guiding the player to trace their floor and obstacles using an AR laser pointer, we mathematically construct a perfect 3D topological map of their room. 
**The Payoff:** Because this system outputs pure, generic geometry (a 0,0,0 Origin, and Arrays of X/Z coordinates), it acts as a "Spatial Operating System." You can use this exact mapping data to build *any* game (Shoot House, AR Basketball, Table Tennis) by simply plugging a different Game Service into the final `PlayableAreaDefinedEvent`.

---

## 2. The Architectural Setup (MVC-S)
We avoid monolithic `MonoBehaviours` (which cause spaghetti code) by strictly separating concerns:

*   **The Model (`ManualRoomMappingConfig.cs`):** A `ScriptableObject` holding immutable designer data (Prefabs, required corner counts). Lives in the project folder, not the scene.
*   **The View (`RoomMappingUIView.cs`, `FloorReticleView.cs`):** "Dumb" `MonoBehaviours`. They only hold Unity components (Buttons, Text, LineRenderers) and expose simple methods (`Show()`, `UpdateText()`). They make zero logical decisions.
*   **The Controller (`RoomMappingUIController.cs`):** The bridge. It listens to the Event Bus and tells the UI View what text to display or which buttons to hide based on the game state.
*   **The Services (`ManualRoomMappingService.cs`, `BoundaryVisualsService.cs`):** Pure C# classes managed by a Service Locator. They handle the complex State Machines, AR tracking math, and Object Pooling. They execute on a centralized `OnTick()` loop.

---

## 3. The AR Tracking Pipeline (Find -> Aim -> Lock)
To create a rock-solid, drift-proof origin, we combined three different AR Foundation components:

1.  **FIND (`ARPlaneManager`):** Runs invisibly in the background, analyzing the camera feed to find logical flat surfaces.
2.  **AIM (`ARRaycastManager`):** Shoots a mathematical raycast from the center of the screen against the invisible planes. We use this to draw a live "Laser Pointer" via `FloorReticleView`.
3.  **LOCK (`ARAnchorManager`):** When the player taps "SET ORIGIN", we take the exact coordinate from the raycast and create an `ARAnchor`. This anchor becomes the **Master Gameplay Origin**. ARCore uses the phone's gyroscope and camera to keep this anchor glued to the physical floor permanently.

### ⚠️ LESSON LEARNED: The "AR Drift" Trap
*   **The Mistake:** Initially, we stored the room's corner coordinates as absolute World Space `Vector3` points. If the AR tracking drifted slightly, the `ARAnchor` would correct itself and move, but our saved points stayed behind, causing the virtual walls to "float" away from the real room.
*   **The Fix:** We must **never** store World Space coordinates. When a player drops a corner, we instantly convert it to Local Space relative to the `ARAnchor` (`InverseTransformPoint`). In the `OnTick()` loop, we dynamically sync them back to World Space for the visuals. Now, if the anchor shifts, the entire room math shifts flawlessly with it.

---

## 4. Event-Driven Communication
Services do not hold references to each other. They communicate via a generic, thread-safe `EventBus<T>`.

*   **Struct Payloads:** All events (`FloorOriginSetEvent`, `ObstaclePointAddedEvent`) are defined as `structs` (Value Types). Because they live on the Stack (LIFO memory), passing events generates zero Garbage Collection (GC) overhead.
*   **Decoupling:** `ManualRoomMappingService` handles the math and shouts "A point was added!" into the Event Bus. `BoundaryVisualsService` hears this and draws a pillar. If the visual service crashes or is deleted, the mapping math continues to work perfectly.

### ⚠️ LESSON LEARNED: Generic Delegate Mismatches
*   **The Mistake:** Trying to write a single method `OnEventFired(IGameEvent e)` to handle multiple different struct events.
*   **The Fix:** Because structs are Value Types, C# requires strict signature matching. You must write specific methods for specific events (e.g., `OnFloorOriginUndone(FloorOriginUndoneEvent e)`).

---

## 5. Memory Safety & Performance (Rule 5)
Instantiating and Destroying GameObjects during gameplay causes the Garbage Collector to run, resulting in fatal lag spikes (frame drops) in AR.

*   **Object Pooling:** `BoundaryVisualsService` uses multiple `UnityEngine.Pool.ObjectPool` instances. When the player maps a corner, we `Get()` a marker. When they click "Undo", we `Release()` it back into hidden memory.
*   **The `maxSize` Trap:** If a pool's `maxSize` is 15, and you `Release()` 20 items into it, Unity will permanently `Destroy()` the extra 5 items, causing the exact GC spike we tried to avoid. Always overestimate `maxSize` for dynamic user content.
*   **Animation without Coroutines:** To animate the 3D wireframe boxes extruding upward (the "Lightsaber" effect), we avoided `IEnumerator` (which generates garbage). Instead, we grouped the lines into a `ShapeGroup` class and used `Mathf.Lerp` paired with `Time.deltaTime` inside our centralized `OnTick()` loop. 

---

## 6. Multithreading Preparation (Rule 6)
While the mapping phase runs on the Main Thread (because it heavily relies on Unity API objects like `Transform` and `LineRenderer`), the **output** of this system is perfectly architected for Multithreading.

*   **The Payload:** The final `PlayableAreaDefinedEvent` packages the Obstacles as an array (`Vector3[]`) instead of a `List`.
*   **Why?** Lists are Reference Types and highly mutable. If passed to a background thread (`Task.Run`), a Race Condition could crash the game. By passing a fixed-size Array (a snapshot) and extracting raw `Vector3` coordinates instead of Unity `Transforms`, we guarantee that the upcoming NavMesh Generator can run heavy triangulation math on a background CPU core without locking up the Unity Main Thread.