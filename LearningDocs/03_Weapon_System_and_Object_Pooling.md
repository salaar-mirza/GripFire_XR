# The Weapon System & Memory Safety

## The MVC-S Breakdown
To adhere to **RULE 1**, we split the concept of a "Gun" into distinct parts:

1. **The Model (`WeaponConfig`):** A `ScriptableObject` holding `Damage`, `FireRateRPM`, `BulletSpeed`, and Pool Sizes. 
   * *Why?* Designers can create a "Pistol Config" and a "Machine Gun Config" by just right-clicking in the editor. They can tweak fire rates while the game is running without recompiling C# code.
2. **The View (`WeaponView` & `BulletView`):** "Dumb" MonoBehaviours. `WeaponView` just holds the `Transform` of the barrel. `BulletView` holds the `TrailRenderer`.
   * *Why?* Pure C# scripts cannot naturally see GameObjects in the scene. Views act as bridges, passing Unity components to the C# logic.
3. **The Manager (`WeaponService`):** The Brain. It handles the fire-rate timers, listens to input, and moves the bullets.

## RULE 5: Object Pooling (Zero Allocation Combat)
Instantiating and Destroying GameObjects during gameplay causes Memory Garbage Collection (GC). On mobile AR devices, GC spikes cause the frame rate to stutter and the AR tracking to drift.

We used `UnityEngine.Pool.ObjectPool<BulletView>` to solve this.

**Coding Choices in the Pool:**
* **Pre-warming:** In `OnInit()`, we instantly spawn `InitialPoolSize` (e.g., 20) bullets and immediately release them into the pool. This moves the instantiation cost to the loading screen, meaning the *first* shot fired in combat is perfectly smooth.
* **ActionOnRelease:** When a bullet hits a wall or flies too far, we don't destroy it; we `Release()` it.
  ```csharp
  actionOnRelease: (b) =>
  {
      if (b != null && b.Trail != null) b.Trail.Clear(); // Clear BEFORE deactivating
      if (b != null) b.gameObject.SetActive(false);
  }
  ```
  *The Trail Hack:* If you don't clear a `TrailRenderer` before moving a recycled bullet back to the barrel, it will draw a massive ugly line across the entire map.

## High-Performance Projectiles (Raycast vs. Rigidbody)
We decided to track bullets purely in C# using a helper class (`ActiveBullet`) and move them manually using `Physics.Raycast`, rather than attaching `Rigidbody` components to the prefabs.

**Why Raycasts?**
1. **The Tunneling Problem:** If a bullet moves at 100 meters per second, a standard Unity Rigidbody will teleport right through thin walls between frames. 
2. **Performance:** Calculating physics for 50 rigidbodies is heavy. Doing a simple math calculation (`Speed * Time.deltaTime`) and shooting a raycast forward is incredibly cheap.

**The Implementation:**
Every frame (`OnTick`), the `WeaponService` iterates backward through the active bullets list:
```csharp
float step = _config.BulletSpeed * Time.deltaTime;
if (Physics.Raycast(bullet.Position, bullet.Direction, out RaycastHit hit, step))
{
    // Hit! Release back to pool.
}
else
{
    // Move forward safely.
    bullet.Position += bullet.Direction * step;
}
```
*Note on Backwards Iteration:* We loop backwards (`for (int i = list.Count - 1; i >= 0; i--)`) so that if a bullet hits something and we `RemoveAt(i)` from the list, it doesn't shift the indices of the remaining bullets we haven't processed yet.

## Outputting to the Void (Event Bus)
When the `WeaponService` fires a bullet, it doesn't play an audio clip or spawn a muzzle flash. It strictly publishes an event:
```csharp
EventBus<WeaponFiredEvent>.Publish(new WeaponFiredEvent(Position, Direction));
```
In the future, an `AudioService` and a `VFXService` will listen to this event. The Weapon System's only job is the math of shooting.