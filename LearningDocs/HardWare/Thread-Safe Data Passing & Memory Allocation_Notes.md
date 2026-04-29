# Thread-Safe Data Passing & Memory Allocation_Notes.md

## The Core Concept: Safe Payloads across Threads
When passing data from the Unity Main Thread to a background worker thread (`Task.Run` or Job System), you must prevent **Race Conditions** (two threads trying to read/write the same memory simultaneously).

## Value Types vs. Reference Types
*   **Value Types (`struct`, `int`, `float`, `Vector3`):** Passed by *copy*. The background thread gets its own isolated clone of the data. Modification is 100% thread-safe. Allocated on the **Stack** (LIFO), meaning zero Garbage Collection (GC) overhead.
*   **Reference Types (`class`):** Passed by *reference* (a memory pointer). Both threads point to the exact same object on the **Heap**. If the Main Thread alters it while the background thread reads it, the game will crash.

## The Rule of Collections (Arrays vs. Lists)
When sending a collection of data to a background thread, NEVER send a `List<T>`. `List<T>` is a highly mutable Reference Type. 
**The Solution:** Convert the List to a fixed-size Array (`myList.ToArray()`) before passing it. While an Array is still a Reference Type (it lives on the Heap), its *size* is immutable. As long as the Main Thread drops its reference to that specific array after passing it, the background thread can read it safely as a read-only snapshot.

## Interview Terminology
*   **Race Condition:** When the behavior of software depends on the unpredictable timing of threads.
*   **Immutability:** An object whose state cannot be modified after it is created.
*   **LIFO (Last-In, First-Out):** The behavior of the Call Stack memory. Fast, auto-clearing, no GC.