# Decoupling Health & Game Flow_Notes.md

## The Core Concept: The "Dumb" Health System
In strict MVC-S, a Health Service should *only* do math. It adds or subtracts integers. It should NEVER trigger UI screens, load scenes, or change global game states.

## The Chain of Consequence
To maintain the Single Responsibility Principle (Rule 4), consequence must be chained via the Event Bus:
1.  **The Aggressor:** `SwarmEnemyController` evaluates distance. If close, it publishes `PlayerDamagedEvent(10)`.
2.  **The Calculator:** `PlayerHealthService` listens, subtracts 10. If health <= 0, it publishes `PlayerDiedEvent()`.
3.  **The Director:** `LevelDirectorService` listens to the death event and triggers `GameState.GameOver`.

## Big-O & Architecture Trade-offs
*   **Pros:** Total isolation. If you want to add a "Shield" system later, you only modify the Calculator. If you want to add a "Revive" mechanic, you only modify the Director. The Aggressor code never changes.
*   **Cons:** You must track your event chains carefully so you don't lose track of how a player dies.


1.
Data Ownership: _currentHealth lives perfectly inside the pure C# PlayerHealthService. The MaxHealth lives in a designer-friendly ScriptableObject.
2.
The Attack (Decoupling): The Swarm broadcasts a generic PlayerDamagedEvent carrying the damage integer. It doesn't know who the player is or how health works.
3.
The Game Over Flow: PlayerHealthService only manages health. When health hits 0, it announces PlayerDiedEvent. The LevelDirectorService (which manages the flow of the game) hears that and switches the macro state to GameOver.