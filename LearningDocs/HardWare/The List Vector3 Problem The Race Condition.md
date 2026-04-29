
That thread will crash immediately.

---

## 2. The List<Vector3> Problem (The Race Condition)

**Question:** Can we pass `List<Vector3> points` into `Task.Run()`?  
**Answer:** **NO** (even though `Vector3` itself is safe).

**The Why:**  
As discussed earlier with the *Iron Gate* challenge, a `List<T>` is a **Reference Type** living on the Heap. Passing the list into a background thread only passes a pointer to the same block of memory.  

Imagine the background thread is looping through that list to do heavy math. At that exact same millisecond, the player on the Main Thread hits the **Undo** button, which removes an item from that list.  

The background thread instantly crashes because the memory it was reading just changed size while it was iterating. This is a fatal **Race Condition**.
