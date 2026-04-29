# 🧠 Deep Dive: Core Architecture (Service Locator & Event Bus)

## 🏗️ The Setup: What is this Architecture?
This game uses a **Decoupled, Pure C# Architecture** heavily relying on two design patterns:
1.  **The Service Locator Pattern (`GameService`)**: A central registry that holds all our manager classes.
2.  **The Event-Driven / Observer Pattern (`EventBus`)**: A global messaging system.

Instead of having 50 `MonoBehaviours` attached to GameObjects in the Unity scene (which use expensive `Update()` calls and `GameObject.Find()`), we have exactly **one** main MonoBehaviour (`GameInitializer`). This acts as the "Conductor," booting up pure C# classes (`Services`) and letting them talk to each other via Events.

---

## ⚖️ Pros, Cons, and the Version Control Superpower

### Why did we choose this setup?
Unity's default way of doing things (drag-and-drop in the inspector, heavy use of Singletons) is great for game jams but terrible for large-scale production. This architecture was chosen for **scalability, performance, and team collaboration**.

### 🌟 The Version Control (Git) Benefit
In standard Unity development, if Developer A links a UI script to a button in the scene, and Developer B adds an audio script to a camera in the scene, Git tries to merge the `.unity` scene file. Scene files are massive, unreadable YAML text files. **Merging them almost always results in a corrupted scene.**
*   **Our Architecture's Solution:** By instantiating and connecting our services purely in C# (`GameInitializer.Awake`), we completely bypass the Unity Inspector. C# scripts are easily merged in Git. Two developers can work on the same systems simultaneously without ever causing a Scene conflict.

### Pros
*   **Git-Friendly:** Zero scene-merge conflicts for logic updates.
*   **High Performance:** Replaces 50 native Unity `Update()` loops with one central loop (`GameInitializer.Update`).
*   **Modular & Decoupled:** Systems don't know about each other. You can delete the `AudioService` and the game won't crash; the `WeaponService` will still fire its events, it just won't be heard.
*   **No Singleton Hell:** We don't have messy `AudioManager.Instance.Play()` calls scattered everywhere.

### Cons
*   **Boilerplate:** It takes more code to set up (Interfaces, Bootstrappers, Event Structs) than just writing a simple Unity script.
*   **No Inspector Debugging:** Because services are pure C# classes (not MonoBehaviours), you can't click on them in the Unity Editor to see their variables in real-time.

---

## 🔍 Detailed Code & Concept Breakdown

### 1. The Main Thread Problem (`EventBus.cs`)
**What is this "Main Thread" thing?**
Unity's engine is fundamentally single-threaded. Things like moving a Transform, instantiating a GameObject, or changing a UI text *must* happen on the "Main Thread". 

However, in AR (ARFoundation) and Networking, callbacks often come from background threads (e.g., the device's camera processor finding a plane). If an AR background thread publishes an `ARPlaneFoundEvent`, and your UI tries to update the screen instantly, **Unity will crash**.

**How our code fixes it:**
1.  `InitializeMainThreadId()`: In `Awake`, we record the ID of Unity's main thread.
2.  `if (!EventBus.IsMainThread)`: When `Publish()` is called, we check if the caller is on the main thread.
3.  `EnqueueMainThreadAction()`: If it's a background thread, we **do not execute the event**. Instead, we put it in a box (a `ConcurrentQueue`).
4.  `ProcessMainThreadActions()`: Every single frame, `GameInitializer.Update()` looks in the box, takes out any waiting events, and fires them safely on the main Unity thread.

### 2. The Interfaces (`IGameEvent`, `IService`, `ITickable`)
These define the "Contracts" of our game.
*   `IGameEvent`: An empty "marker" interface. It stops people from accidentally sending random data types (like an `int` or a `string`) through the EventBus. It forces them to make a dedicated `struct` (like `PlayerDiedEvent`).
*   `IService`: Forces every manager class to have an `OnInit()` and `OnDispose()` method. This ensures they can all be started and stopped cleanly without memory leaks.
*   `ITickable`: Since our services aren't MonoBehaviours, they don't have Unity's `Update()`. If a service *needs* an update loop (like calculating swarm paths), it implements `ITickable` and gets ticked by the `GameInitializer`.

### 3. The Service Locator (`GameService.cs`)
*   `Dictionary<Type, IService> s_services`: This is our internal vault. It maps a type (e.g., `typeof(AudioService)`) to the actual instance of the object in memory.
*   **Fail-Fast Retrieval:** In `Get<T>()`, if a service isn't found, we do *not* return `null`. We explicitly `throw new InvalidOperationException`. Why? Because returning null causes bugs 10 steps down the line. Throwing an exception stops the game instantly and points you exactly to the missing registration.

### 4. The EventBus (`EventBus.cs`)
*   `public static class EventBus<T>`: By making the class generic, C# creates a brand new, completely separate instance of this class for every event type in the game. `EventBus<PlayerDiedEvent>` is physically separated in memory from `EventBus<GunFiredEvent>`. This makes event lookups extremely fast (O(1) time complexity) because there's no dictionary lookup required!
*   `HashSet<Action<T>> s_listeners`: We use a `HashSet` instead of a `List`. If a developer accidentally calls `Subscribe(MyMethod)` twice, a `List` would fire the event twice. A `HashSet` naturally guarantees uniqueness, ignoring the duplicate subscription.
*   **The Snapshot Fix:** 
    