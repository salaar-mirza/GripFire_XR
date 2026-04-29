An average (start + end) / 2 gives you the exact middle, which works perfectly when your _animationProgress is exactly 0.5f.
But what happens when the animation is only 10% done (0.1f), or 90% done (0.9f)? An average won't work anymore. We need a mathematical function that dynamically scales based on that percentage.
In game engine architecture and 3D math, moving smoothly from a Start value to an End value based on a percentage (0.0 to 1.0) is a fundamental concept called Linear Interpolation, commonly referred to as Lerp.
The Raw Math
If you were writing a custom game engine in C++ without any libraries, the raw algebraic formula to find your current position looks like this: currentValue = startValue + ((endValue - startValue) * progress)
If you plug our numbers in at 50% progress: 0 + ((2.5 - 0) * 0.5) = 1.25. (Exactly the average!) If you plug it in at 10% progress: 0 + ((2.5 - 0) * 0.1) = 0.25.
The Unity Engine Shortcut
Because this math is executed millions of times per second in video games, Unity provides a highly optimized, built-in function for this: Mathf.Lerp().
It takes three arguments: Mathf.Lerp(float startValue, float endValue, float progress)