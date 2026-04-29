# Tick-Based Timers & Object Pooling_Notes.md

## The Core Concept: The Service Timer
In a pure MVC-S architecture, Services do not inherit from `MonoBehaviour`, so they cannot use `Update()` or `StartCoroutine()`. 

To execute logic over time (like spawning an enemy every 2 seconds):
1. The Service implements `ITickable`.
2. Declare `private float _spawnTimer;` and `private float _spawnInterval = 2.0f;`.
3. Inside `OnTick()`:

1.
The Prefabs: You correctly identified that the GameInitializer will inject a Config (ScriptableObject) containing our Prefabs into the Service during Phase 1 Boot.
2.
Memory Management: You correctly chose UnityEngine.Pool.ObjectPool to pre-warm and recycle our GameObjects instead of destroying them.
3.
Time Management: You correctly chose ITickable to manage the spawn timer. By simply adding Time.deltaTime to a _spawnTimer float inside OnTick(), we completely avoid dirty MonoBehaviour Coroutines and GC lag spikes!
   
   
   
## Pre-Warming the Object Pool
When using `UnityEngine.Pool.ObjectPool<GameObject>`, always "pre-warm" the pool during `OnInit()`. 
If you expect 20 enemies on screen, instantiate 20 deactivated enemies during the loading screen. This prevents the engine from freezing to load memory when the player is in the middle of a frantic firefight.


When using UnityEngine.Pool.ObjectPool<GameObject>, always "pre-warm" the pool during OnInit(). If you expect 20 enemies on screen, instantiate 20 deactivated enemies during the loading screen. This prevents the engine from freezing to load memory when the player is in the middle of a frantic firefight.