# Transform Scale vs. Physics/AI Scale_Notes.md

## The Core Concept: The Runtime Scaling Trap
In a Data-Driven architecture, it is tempting to control an entity's 3D scale via a Configuration file (e.g., `_transform.localScale = config.Scale`).

*   **The Trap:** Unity Physics (`Colliders`, `Rigidbodies`) and Unity AI (`NavMeshAgent`) do not always gracefully adapt to runtime scale changes via code. Specifically, a `NavMeshAgent`'s `Radius` and `Height` are absolute values, completely decoupled from `transform.localScale`.
*   **The Solution:** Physical scale and pathfinding radius should be baked directly into the **Prefab asset**. Use Data Configurations for pure logic (Speed, Acceleration, Health), but rely on Prefab composition for spatial/physics properties to ensure zero runtime overhead and perfect pathfinding alignment.


While Option B (The Config Way) is the right choice for logic (health, damage, speed), Option A (Scaling the Prefab directly) is actually the correct choice for physical scale in this specific scenario.
Here is exactly why this Unity trap exists:
The "NavMeshAgent Radius" Trap
In Unity, scale isn't just visual. We added a NavMeshAgent component to our SwarmEnemyView. The NavMeshAgent has its own internal variables for Radius and Height, which determine how close it can get to a wall or a couch before it gets stuck.
If we spawn a giant enemy and use C# code to shrink its transform.localScale = new Vector3(0.1f, 0.1f, 0.1f), the visual model shrinks, but the NavMeshAgent's internal pathfinding radius does NOT shrink automatically. You would end up with a tiny 2-inch Smurf that gets stuck 3 feet away from your coffee table because its invisible pathfinding brain still thinks it's a giant!
By scaling the Prefab directly in the Editor (Option A), the designer can visually adjust the NavMeshAgent radius to perfectly match the tiny 3D model, saving us from writing tedious C# code to recalculate physics radii at runtime.