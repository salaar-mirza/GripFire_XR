# Engine-to-Thread Data Transfer_Notes.md

## The Core Concept: The Unity Engine Barrier
Unity's core C++ engine is strictly single-threaded. Background threads (`Task.Run` or Jobs) are mathematically powerful, but they are completely blind to the Unity scene. They cannot read or write to `Transform`, `GameObject`, or `MonoBehaviour`.

## The "Extract and Pack" Pattern
To safely use background threads without crashing the engine or causing Race Conditions:
1. **Extract:** On the Main Thread, extract the raw mathematical data from the Unity objects (e.g., `myTransform.position`, `myTransform.rotation`).
2. **Convert:** Convert highly mutable collections (`List<T>`) into immutable-sized snapshots (`Array`).
3. **Pack:** Wrap these raw value types (`Vector3`, `float`, `int[]`) into a `struct`.
4. **Ship:** Pass the `struct` into the background thread. Because it is a Value Type, the background thread receives a perfect, isolated copy on its own Stack memory.