# 🧠 Deep Dive: The Enemy Swarm Architecture (AI & Spawning)

## 🏗️ The Setup: MVC in a Game Engine
Most Unity developers put all their enemy logic (health, AI, animations, data) into a single giant `Enemy.cs` MonoBehaviour attached to a prefab. We explicitly **do not** do this. 

Our Swarm System uses a Model-View-Controller (MVC) approach to separate data, physics, and logic. This prevents spaghetti code and drastically improves performance.

1. **The Model (`SwarmConfig.cs`)**: A Unity `ScriptableObject` that holds pure data (Health, Speed, Damage).
2. **The View (`SwarmEnemyView.cs`)**: A "dumb" Unity `MonoBehaviour` attached to the prefab. It has zero logic. It only holds references to the physical `NavMeshAgent`, `Collider`, and `MeshRenderer`.
3. **The Controller (`SwarmEnemyController.cs`)**: A pure C# class (The "Brain") that reads the Model, commands the View, and runs the AI logic.
4. **The Manager (`TargetSpawningService.cs`)**: The central system that pools, spawns, and ticks the controllers.

---

## 🔍 System-by-System Breakdown & "The Why"

### 1. Data-Driven Design (`SwarmConfig.cs`)
**How it works:** 
Instead of hardcoding `health = 100` inside an enemy script, we use a `ScriptableObject` asset. 
**Why we chose this:**
* **Designer Friendly:** A game designer can tweak enemy speed, spawn rates, and damage in the Unity Inspector without ever opening a C# script or asking a programmer to recompile the game.
* **Memory Efficiency:** If we spawn 100 ants, we don't copy the variables (max health, speed, damage) 100 times in memory. All 100 ants reference the exact same single `SwarmConfig` file.

### 2. The Smart Brain (`SwarmEnemyController.cs`)
Because this is a pure C# class, it does not have Unity's `Update()` method. It is manually "ticked" by the `TargetSpawningService`. 

**Performance Superpower 1: Tick Throttling**
* **The Problem:** Unity's NavMesh A* Pathfinding is highly CPU-intensive. If you have 50 ants calculating their path to the player at 60 Frames Per Second, a mobile AR device will overheat and lag.
* **The Solution:** We throttle the logic using `_pathfindingTimer`. The ants only run `_view.Agent.SetDestination()` **twice per second** (`PathfindingInterval = 0.5f`). In between those half-seconds, the internal C++ NavMesh system smoothly moves the ant. The player cannot tell the AI is "thinking" slowly, but the CPU usage drops by 98%.

**Performance Superpower 2: `sqrMagnitude` over `Vector3.Distance`**
* **The Problem:** To check if an ant is close enough to bite the player, you need distance. `Vector3.Distance` uses the Pythagorean theorem ($a^2 + b^2 = c^2$), which requires calculating a Square Root ($\sqrt{c}$). Square roots are famously slow operations for CPUs.
* **The Solution:** We use `distanceToPlayer.sqrMagnitude`. This gives us the distance *squared* (e.g., instead of checking if distance is `< 0.5`, we check if the squared distance is `< 0.25`). We get the exact same result while completely bypassing the expensive square root math!

### 3. The Central Manager (`TargetSpawningService.cs`)
This service owns the object pools and acts as the puppeteer for every ant in the room.

**The AR Spawning Problem (Barycentric Math):**
* **The Problem:** In a normal game, you place spawn points in the Unity editor. In AR, the player's living room floor is mapped dynamically. We have to spawn Ant Hills on a floor that didn't exist until 5 seconds ago!
* **The Solution:** When the room is mapped, the service receives an array of floor Triangles and Vertices. To safely place an Ant Hill, the system picks a random triangle, and uses **Barycentric Coordinates** (`localRandomPos = a + r1 * (b - a) + r2 * (c - a)`) to guarantee the generated X/Z point falls mathematically *inside* the bounds of that specific triangle. We then project it onto the NavMesh.

**The O(1) Damage Routing Architecture:**
* **The Problem:** When the player shoots a gun, the bullet hits a Unity `Collider`. How does the game know *which* C# `SwarmEnemyController` owns that specific collider to apply damage? Traditionally, people use `hit.collider.GetComponent<Enemy>().TakeDamage()`. `GetComponent` is a slow, string-matching lookup that hurts performance.
* **The Solution:** We use a `Dictionary<Collider, SwarmEnemyController> _enemyLookup`. 
    * When an ant spawns, we add it to the dictionary: `_enemyLookup.Add(enemyView.Collider, controller);`
    * When a bullet hits a collider, we instantly look up the brain: `_enemyLookup.TryGetValue(hitCollider, out var controller)`
    * This is an **O(1) operation** (instantaneous). The bullet tells the brain it was hit, the brain subtracts health, and if health is 0, the brain publishes the `SwarmEnemyDestroyedEvent`.

### 4. Decoupled Communication (`EnemyEvents.cs`)
* **How it works:** When an enemy dies, it does NOT tell the UI to update the score, and it does NOT tell the Audio system to play a squish sound. It simply shouts into the void: `EventBus<SwarmEnemyDestroyedEvent>.Publish(...)`.
* **Why this is critical:** The enemy system is completely blind to the rest of the game. You could completely delete the UI and Audio systems, and the Enemy system wouldn't throw a single `NullReferenceException`. 
* **Struct Optimization:** The event is a `readonly struct`. Structs are allocated on the Stack, not the Heap. By making it `readonly`, we prevent C# from making hidden copies of it in memory, avoiding Garbage Collection overhead when millions of events are fired.