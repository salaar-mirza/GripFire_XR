# Procedural VRAM & Thread-Safe Mesh Data_Notes.md

## The Anatomy of a 3D Mesh
To the GPU, a 3D model is not a physical object; it is just two mathematical arrays:
1. `Vector3[] Vertices`: Points in 3D space.
2. `int[] Triangles`: A list of indices (in groups of 3) that connect the vertices to draw flat surfaces.
Because these are Value Types and Arrays, they are 100% Thread-Safe and can be generated entirely on a background CPU core.

## The Procedural VRAM Exception (Rule 5)
Standard objects (bullets, enemies) use **Object Pooling** to save memory. 
Procedurally generated objects (custom room meshes) CANNOT be pooled because every room is a completely different size and shape. 
*   **The Trap:** If you overwrite a `MeshFilter.mesh` reference, C#'s Garbage Collector will clear the CPU pointer, but it **will not** clear the actual geometry data from the GPU's Video RAM (VRAM).
*   **The Fix:** You must explicitly call `UnityEngine.Object.Destroy(oldMesh)` before assigning the new one to prevent fatal memory leaks on mobile devices.


Challenge 1: The Return Payload (RULE 6) When the background thread finishes calculating the complex shape of the floor, it needs to send that data back to the Main Thread to build the UnityEngine.Mesh. According to the Payload Rule, what specific C# data types will the background thread pack into its return struct? (Hint: A 3D model in a video game is fundamentally made of two specific arrays of numbers).
Challenge 2: The Procedural VRAM Exception (RULE 5) Let's say the Main Thread receives that data, successfully builds a UnityEngine.Mesh, and the player plays the game. Then, the player decides they want to map a different room, so they hit "Restart". According to our newly added Procedural VRAM Exception, what exact C# method MUST our NavMeshBuilderService call on that old UnityEngine.Mesh before creating the new one, and why can't we just put it in an ObjectPool?

Concept 1: What is a 3D Mesh, really?
When you look at a 3D model in a video game, the GPU doesn't see a "floor" or a "wall". It only sees two massive lists of numbers:
1.
Vertices: A list of 3D coordinates in space (Vector3[]).
2.
Triangles: A list of integers (int[]). Every three integers connect three vertices together to draw a solid, flat triangle.
Because Vector3[] and int[] are just raw data arrays (Value Types), they are 100% thread-safe! The background thread can calculate thousands of these numbers, pack them into a simple struct, and safely ship them to the Main Thread. The Main Thread takes those raw numbers and feeds them into the Unity API (new Mesh()) to actually render the object.
Concept 2: The Procedural VRAM Exception (Rule 5)
Normally, we use Object Pooling (like we did for our markers and bullets) to save memory. We just turn the same objects off and on.
But Procedural Meshes are different. If you map a square room, that generates a specific Mesh file that lives in your phone's Video RAM (VRAM). If you remap your room to be a giant L-shape, you generate a completely different Mesh. If you just drop the old Mesh and say meshFilter.mesh = newMesh, Unity's Garbage Collector (which only manages regular CPU RAM) will not delete the old Mesh from the GPU. If the player remaps their room 10 times, the VRAM fills up with 10 ghost meshes, and the phone crashes.

1.
The Payload: You correctly identified that raw arrays of Value Types (Vector3[] for vertices, int[] for triangles) are the absolute safest way to pass 3D geometry across threads without causing Race Conditions.
2.
The VRAM Leak: You correctly identified that calling UnityEngine.Object.Destroy(mesh) is mandatory. Because procedural meshes are unique per room, pooling them is impossible. If we don't explicitly destroy them, they will permanently live in the GPU's VRAM until the app crashes.