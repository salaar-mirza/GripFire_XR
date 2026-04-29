# Data Encapsulation & Single Responsibility_Notes.md

## The Core Concept: Information Hiding
In strict MVC-S architecture, Services must practice "Information Hiding". A Service should only possess the exact data it needs to do its specific job, and nothing more.

## The DRY Principle (Don't Repeat Yourself)
If `Service A` (Spawner) already listens to an event to cache the environment boundaries, `Service B` (Level Director) should NEVER cache those same boundaries just to pass them to `Service A` later.
*   **The Trap:** Passing redundant data creates tight coupling. If the mapping data structure changes later, you have to rewrite both services.
*   **The Solution:** The Level Director should only command the *flow* of the game (e.g., `StartWave(enemyCount)`). The Spawner handles the *spatial logic* entirely internally.



1. Memory Ownership (Correct!)
You correctly identified that the TargetSpawningService (a pure C# class) MUST own the ObjectPool<SwarmEnemyView> and the OnTick() timer. If we put the timer or the pool inside an AntHillView MonoBehaviour, we would violate RULE 1 (Dumb Views) and lose centralized control of our memory.
2. The Wave Trigger Data (The Trap!)
You said: "we need the position data like the boundry area is the absolute minimum data parameter"
Architect's Correction: Look closely at the TargetSpawningService code we just wrote in the previous step. It already subscribes to PlayableAreaDefinedEvent. It already calculates and caches _minX, _maxX, _minZ, and _maxZ.
If the LevelDirectorService passes the boundary points into StartWave(), we are violating RULE 4 (Single Responsibility) and the DRY Principle (Don't Repeat Yourself). The Level Director shouldn't even know what a boundary point is!
The absolute minimum data needed is just the number of enemies: StartWave(int enemyCount). The Spawner already knows where the room is and where the Ant Hills are. It just needs the Director to tell it how many Swarm enemies to spit out!