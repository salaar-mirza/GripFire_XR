# Core Architecture: The "Active Pool" Memory Leak Trap

## The Misconception
When building game systems that spawn enemies, bullets, or particle effects, it is a common practice to use `UnityEngine.Pool.ObjectPool<T>`. 

When shutting down the game, destroying the scene, or resetting a level, developers often simply write:
`_myPool.Clear();`
and assume that Unity cleans everything up. **This is a dangerous misconception.**

## The Problem
Unity's `ObjectPool.Clear()` method has a very specific and limited scope: **It only loops through and destroys objects that are currently asleep inside the pool's internal stack.**

It has absolutely zero awareness of objects that have been pulled *out* of the pool via `.Get()` and are currently active in the Unity Scene.

**The Leak Scenario:**
1. A player is playing your AR Shooter game.
2. The spawner currently has 20 dormant ants inside `_swarmEnemyPool`.
3. The spawner also has 5 active ants currently running around attacking the player (stored in `_activeEnemies`).
4. The player clicks "Quit to Main Menu", which triggers the teardown sequence `OnDispose()`.
5. Your code calls `_swarmEnemyPool.Clear()`.
6. **Result:** The 20 dormant ants are successfully destroyed. However, the 5 active ants are permanently abandoned. Their references are lost, leaving them orphaned in memory and causing a massive memory leak that degrades performance over time.

## The Architectural Solution
To safely tear down an object pool system, you must act as the manual garbage collector for the items currently "checked out" of the library. 

1. **Track Active Objects:** Always maintain an active list (e.g., `List<SwarmEnemyController> _activeEnemies`). 
2. **Manual Destruction Loop:** In your teardown/dispose method, iterate through your active list and explicitly call `Object.Destroy()` on their GameObjects.
3. **Clear the References:** Clear the active list to unbind the C# references.
4. **Clear the Pool:** *Finally*, call `_myPool.Clear()` to wipe out the dormant objects.

By cleanly disposing of both the *dormant* and the *active* elements, you guarantee 100% memory safety during scene transitions!
