# 🧠 Deep Dive: Procedural Virtual Rooms & AR Pathfinding

## 🌍 The Goal: Bridging the Real and Virtual Worlds
When the player finishes manually mapping their living room, the game has a list of 2D points (the floor corners) and a ceiling height. But AI enemies (like our Swarm Ants) cannot walk on a "list of points." Unity's pathfinding system requires physical 3D geometry (a NavMesh).

**The massive technical feat we are accomplishing here is taking human-plotted AR coordinates and procedurally generating a fully enclosed, physical 3D room at runtime.** 

Yes, we are literally building a complete 3D room—complete with a generated floor mesh, invisible walls, and obstacle blocks. We simply choose not to *render* the graphics so the player only sees their real-world living room.

---

## 🚀 Phase 1: The Data Handoff & Drift Prevention
The process begins in the `NavMeshBuilderService`, which listens for the `PlayableAreaDefinedEvent` from the Room Mapping system.

**The Local Space Math:**
Before we do any complex math, we must protect the data from "AR Drift."

We convert the World Space boundary points into **Local Space** relative to the `MasterOrigin` AR Anchor. This ensures that when we build the 3D room, it is mathematically glued to the physical floor, even if the device's tracking shifts.

---

## 🧮 Phase 2: Procedural Triangulation (The Math)
To build a custom 3D floor, we have to create a Unity `Mesh`. A mesh requires two things:
1. **Vertices:** The corners of the room.
2. **Triangles:** An array of integers connecting those dots into solid 3D faces.

**The Main Thread Bottleneck:**
Calculating 3D geometry is highly CPU-intensive. If we run this on Unity's main thread, the AR camera feed will freeze and stutter. 
To fix this, we use `Task.Run(() => GenerateFloorMeshAsync(...))` to offload the heavy math to a background processor thread.

**Double-Sided Fan Triangulation:**
A major issue in procedural generation is "Winding Order." In 3D graphics, if you draw a triangle Clockwise, it faces UP. If you draw it Counter-Clockwise, it faces DOWN (and becomes invisible from above). 
Because we don't know if the human player mapped their room Clockwise or Counter-Clockwise, we use a brilliant math trick: **We draw both!**
 
 
 
 This loops through the vertices like a fan, connecting point 0 to point 1 to point 2, then point 0 to 2 to 3, etc. This guarantees a solid floor no matter how the player walked around their room.
 
 ---
 
 ## 🏗️ Phase 3: Forging the 3D Room (`VirtualRoomView`)
 Once the background thread finishes the math, it fires the `FloorMathCalculatedEvent` back to the main thread. The `VirtualRoomView` takes over to physically build the arena.
 
 ### 1. The Floor
 The script takes our generated vertices and triangles and injects them into a `MeshFilter` and a `MeshCollider`. We also add a `_floorOffset` (e.g., lifting it by 5 centimeters) so our digital floor doesn't graphically "Z-fight" or clip into the physical AR floor.
 
 ### 2. The Invisible Walls
 Unity's NavMesh system needs to know where the edge of the world is, otherwise, enemies will try to walk out of your living room and fall into the void. **We programmatically construct invisible walls along the perimeter.**
 
 **The Wall Math:**
 To build a wall between Point A and Point B, we need its center, its length, and its rotation.
 1. **Midpoint:** `(pointA + pointB) / 2f` (This is where we spawn the wall).
 2. **Distance:** `Vector3.Distance(pointA, pointB)` (This becomes the wall's scale/length).
 3. **Rotation:** `Quaternion.LookRotation(pointB - pointA)` (This rotates the wall to perfectly align with the line connecting the two corners).
 
 We spawn an empty `GameObject`, apply these transform values, and attach a `BoxCollider`. The room is now physically boxed in!
 
 ### 3. The Obstacle Blockers
 If the player mapped a coffee table, we don't want ants walking through it. 
 The script takes the `ObstacleData` and calculates a **Bounding Box**. It finds the lowest and highest X and Z coordinates (`minX`, `maxX`, `minZ`, `maxZ`) of the traced shape. It then spawns a `BoxCollider` directly over the physical table, creating a solid digital barrier.
 
 ---
 
 ## 🤖 Phase 4: The AI NavMesh Bake
 At this point, we have a fully functioning, physics-based 3D room. It has a floor, walls, and obstacle blocks, but **it is completely invisible** because we only added `BoxColliders`, not `MeshRenderers`.
 
 Finally, we tell Unity's `NavMeshSurface` to look at our creation and bake the pathfinding data: