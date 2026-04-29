# 🧠 Deep Dive: Wave Director Architecture & State Synchronization

## 🏗️ The Setup: Event-Driven Level Progression
In our architecture, the `LevelDirectorService` acts as the pacing manager for the game. Because we use a decoupled Event Bus, the Director doesn't need to tightly monitor the `TargetSpawningService` or check `enemy.IsDead` every frame.

**The Lifecycle:**
1. The Room Mapping finishes and fires `NavMeshBakedEvent`.
2. The Director hears this and queues Wave 1.
3. The Director uses `OnTick()` to count down the wave delay, then tells the Spawner to start.
4. As enemies die, they fire `SwarmEnemyDestroyedEvent`. The Director simply counts down an internal integer (`_activeEnemiesInWave`). When it hits 0, the next wave is queued.

---

## 🔬 Key Engineering Concepts

### 1. The "Ghost Kill" Synchronization Trap
**The Problem:**
In wave-based shooters, there is often a grace period (e.g., 3 seconds) between clearing Wave 1 and starting Wave 2. 

If you set `_activeEnemiesInWave = 10` the *moment* Wave 1 is cleared, you open a dangerous 3-second window. If an enemy from Wave 1 dies late (e.g., from lingering fire damage, or falling out of bounds), it fires a `SwarmEnemyDestroyedEvent`. Your Director will subtract 1 from your new Wave 2 count *before Wave 2 even spawns*. 

When Wave 2 finally spawns 10 enemies, your counter is at 9. The wave will complete prematurely, leaving 1 invincible/orphaned enemy roaming the map.

**The Solution:**
State variables must accurately reflect the *physical reality* of the game.
1. **Delay Assignment:** Do not set `_activeEnemiesInWave` when the wave is *queued*. Set it inside `OnTick()` at the exact millisecond the `targetSpawningService` is commanded to spawn them.
2. **Defensive Event Handling:** Inside the event listener (`OnTargetDestroyed`), add a guard clause: `if (_waveDelayTimer >= 0) return;`. This explicitly tells the system: *"If we are currently waiting for the next wave to start, ignore any random death events."*

### 2. Tick Optimization (Early Exits)
The `LevelDirectorService` implements `ITickable`, meaning its `OnTick()` runs every single frame. 

**The Optimization:**