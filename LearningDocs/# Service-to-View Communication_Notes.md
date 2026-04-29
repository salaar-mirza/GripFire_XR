# Service-to-View Communication_Notes.md

## The Core Concept: When to use Events vs. Direct Commands
In strict MVC-S architecture, deciding how two scripts talk dictates the performance and cleanliness of your code.

*   **Horizontal Communication (Events):** Used when two equal, independent systems need to react to the same thing (e.g., `WeaponService` and `AudioService` both reacting to `TouchInput`). Use `EventBus<T>`.
*   **Vertical Communication (Direct Command):** Used when a Manager (Service/Controller) strictly owns a "Dumb View". The Manager is allowed to hold a direct reference to the View and call its public methods (e.g., `_view.UpdateText()`, `_view.BakeNavMesh()`). 

## The "Dumb View" Rule
When using Vertical Communication, the View must remain "dumb". 
*   **BAD:** `_view.CalculatePathfindingMath()` (The view is doing logic).
*   **GOOD:** `_view.BakeNavMesh()` (The view is just acting as a wrapper for Unity's built-in `NavMeshSurface.BuildNavMesh()` component execution).