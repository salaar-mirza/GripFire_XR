# Asynchronous Event Dependencies_Notes.md

## The Core Concept: Event Race Conditions
When offloading heavy tasks to a background thread (`Task.Run`), the Main Thread continues executing code instantly. 
If System A (Level Director) listens to the exact same trigger as System B (NavMesh Builder), System A will try to use the environment before System B has finished building it.

## The Solution: Event Chaining
Never assume background tasks are instantaneous. 
Instead of parallel listening, create a **Dependency Chain**:
1. `MappingService` publishes -> `PlayableAreaDefinedEvent`.
2. `NavMeshBuilderService` listens, runs heavy background math, finishes, and publishes -> `NavMeshGeneratedEvent`.
3. `LevelDirectorService` listens ONLY to `NavMeshGeneratedEvent` before starting the game. 
This guarantees the environment is 100% physically and logically constructed before entity spawning begins.