# Thread-Safe Data Passing & Memory Allocation_Notes.md

## The Core Concept: Safe Payloads across Threads
When passing data from the Unity Main Thread to a background worker thread (`Task.Run` or Job System), you must prevent **Race Conditions** (two threads trying to read/write the same memory simultaneously).

## Value Types vs. Reference Types
*   **Value Types (`struct`, `int`, `float`, `Vector3`):** Passed by *copy*. The background thread gets its own isolated clone of the data. Allocated on the **Stack** (LIFO), meaning zero Garbage Collection (GC) overhead.
*   **Reference Types (`class`, `Array`):** Passed by *reference* (a memory pointer). Both threads point to the exact same object on the **Heap**. 

## The Rule of Collections (Arrays vs. Lists)
When sending a collection of data to a background thread, NEVER send a `List<T>`. `List<T>` is a highly mutable Reference Type. 
**The Solution:** Convert the List to a fixed-size Array (`myList.ToArray()`) before passing it. While an Array is still a Reference Type (it lives on the Heap), its *size* is immutable. As long as you design your Main Thread to "forget" about that array after passing it, the background thread can read it safely as a read-only snapshot.

You correctly identified that an Array (Vector3[]) ALWAYS lives on the Heap, even if it is declared inside a struct.
Let's clarify one tiny, high-level nuance regarding your statement: "the main thread drops its reference when passing to the background core." Because the array is on the heap, when you pass the struct, the struct is copied (this is called a Shallow Copy). The background thread gets a new struct, but the array reference inside that new struct still points to the exact same array on the Heap. This means the Main Thread doesn't automatically drop it. You, the programmer, must intentionally write your code so the Main Thread stops using that array after dispatching it to the Event Bus.