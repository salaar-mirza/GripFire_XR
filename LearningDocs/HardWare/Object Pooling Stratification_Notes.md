# Object Pooling Stratification_Notes.md

## The Core Concept: Homogeneous Pools
An Object Pool (`UnityEngine.Pool.ObjectPool<T>`) is designed to be **homogeneous**. Every object inside a specific pool should be structurally identical (instantiated from the exact same Prefab). 

## Why We Don't Mix Prefabs
If you have a `BlueRoomPillar` prefab and a `RedObstaclePillar` prefab, you should never put them in the same `ObjectPool<GameObject>`. 
*   **The Problem:** When you call `pool.Get()`, you don't know which prefab you are going to get. You would have to write expensive `GetComponent` checks or keep track of internal IDs to figure out what you just pulled out of memory, destroying the performance benefits of pooling.

## The Solution: Stratification
Create a separate `ObjectPool` instance for every unique Prefab you need to spawn.
*   `_roomMarkerPool = new ObjectPool(...)`
*   `_obstacleMarkerPool = new ObjectPool(...)`
This guarantees O(1) time complexity when retrieving an object, ensuring you get exactly the visual representation you requested without any expensive casting or type-checking.