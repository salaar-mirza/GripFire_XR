# Unity Engine Architecture & ITickable_Notes.md

## The C++ / C# Bridge
Unity's core is C++, but game logic is C#. Calling native Unity "Magic Methods" like `Update()`, `Start()`, or `Awake()` requires crossing a memory bridge. Having thousands of MonoBehaviours with `Update()` causes massive overhead.
**Solution:** Use a centralized Manager (like `GameInitializer`) with a single `Update()` method that iterates through pure C# interfaces (`ITickable`). This crosses the bridge only once per frame.

## Why Coroutines are "Dirty" in Enterprise Architecture
1. They strictly require a `MonoBehaviour` to execute, breaking pure C# decoupled designs.
2. They rely on `IEnumerator` state machines which generate Heap allocations (`yield return new...`), forcing the Garbage Collector to run and causing frame spikes on mobile devices.

## ECS/DOTS vs. Object Pooling
Data-Oriented Technology Stack (DOTS) ensures Data Locality (preventing CPU Cache Misses). However, it is severe over-engineering for games with < 1,000 active entities. Pre-warmed **Object Pools** combined with `ITickable` batching provides 80% of the performance benefits of ECS with a fraction of the development complexity.