# 🧠 Deep Dive: The "Parent Destruction" Object Pool Trap

## 🏗️ The AR Parenting Necessity
In AR applications, digital objects drift. To solve this, developers attach GameObjects to an `ARAnchor` by setting the anchor as the object's parent (`marker.transform.SetParent(anchor)`). If the AR tracking system adjusts the anchor's position, all children perfectly move with it.

## 🔬 The Trap
When mixing Object Pooling with child hierarchies, a lethal bug can occur:

1. **The Check-out:** You pull a Marker from the pool and parent it to the Anchor.
2. **The Destruction:** A player deletes the Anchor. Unity's C++ backend immediately cascades and destroys all child objects attached to it. The Marker is now dead.
3. **The Return:** Your code, unaware the Marker was destroyed, puts the dead C# reference back into the Object Pool stack.
4. **The Crash:** The next time a script requests an object from the pool, it is handed a dead object. The script attempts to use it, resulting in a `MissingReferenceException`.

## 🛠️ The Architectural Solution
To prevent objects from dying while checked out, we must enforce strict lifecycle rules:

1. **Unparent on Release (`actionOnRelease`):**
Whenever an object is returned to the pool, its `actionOnRelease` delegate MUST cleanly decouple it from the scene hierarchy.
`obj => { obj.SetActive(false); obj.transform.SetParent(null, false); }`
This ensures dormant objects live safely in the root of the scene, immune to hierarchy destruction.

2. **Event Ordering:**
If an EventBus is used to trigger the cleanup (like an `UndoEvent`), you must fire the event **BEFORE** you destroy the parent object. This gives your pooled systems the milliseconds they need to unparent and rescue their objects before the parent object goes down with the ship!