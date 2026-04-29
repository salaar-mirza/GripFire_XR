# Linear Interpolation & State Animation_Notes.md

## The Core Concept: Lerp
Linear Interpolation (`Lerp`) finds a specific point between two values based on a percentage (0.0 to 1.0).
*   `Mathf.Lerp(0f, 10f, 0.5f)` returns `5f`.
*   It is the cheapest, most efficient way to handle smooth transitions, UI sliders, and procedural animations.

## State-Based Animation (No Coroutines)
To animate an object over time without allocating Heap memory (Garbage Collection):
1.  Define a float `_animationProgress = 0f;` and a `_animationDuration = 1.0f;`
2.  Inside an `ITickable.OnTick()` loop, add time: `_animationProgress += Time.deltaTime / _animationDuration;`
3.  Clamp the progress so it doesn't exceed 1.0: `_animationProgress = Mathf.Min(_animationProgress, 1.0f);`
4.  Feed `_animationProgress` into your `Mathf.Lerp()` function to update the object's scale, position, or color.