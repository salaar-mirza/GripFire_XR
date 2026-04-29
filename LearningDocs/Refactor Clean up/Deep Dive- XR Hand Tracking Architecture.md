# 🧠 Deep Dive: XR Hand Tracking Architecture

## 🏗️ The Setup: Separation of Concerns (SoC)
Dealing with experimental hardware (like AR Hands) can result in messy code if all logic is crammed into one script. This module strictly separates responsibilities:

1.  **Hardware Interfacing (`ARHandTracker`)**: Its *only* job is to talk to the Unity `XRHandSubsystem`. It doesn't know what a "Pinch" is. It just says "Here is the math coordinate of a thumb."
2.  **Logic & Math (`ARGestureDetector`)**: Its *only* job is to measure distances between two points and decide if a gesture occurred. It doesn't know where the points came from (VR hands, a mouse, or AR camera).
3.  **Data (`HandTrackingConfig`)**: Holds the designer-tweaked thresholds.
4.  **Orchestration (`HandTrackingService`)**: The manager that connects the Tracker to the Detector every frame.

**The Benefit:** If we decide to switch from Unity's XR Hands to Oculus/Meta's custom Hand Tracking SDK, we **only** have to rewrite the `ARHandTracker`. The gesture math and the overarching service remain completely untouched!

---

## 🔬 Key Engineering Concepts

### 1. The "Stuck Input" Hardware Trap
**The Problem:**
When dealing with physical hardware (gamepad disconnects, AR hands leaving the camera bounds), data streams can suddenly stop. 
If a player is holding down a "Pinch" to charge up a laser, and moves their hand off-screen, `TryGetPinchJoints` returns `false`. If we simply `return` out of our update loop when this happens, the gesture detector *never receives the signal that the hand opened*. The laser gets stuck charging infinitely.

**The Solution:**
You must constantly account for hardware dropouts. In the `HandTrackingService`, an `else` block is used: if tracking is lost, we explicitly call `_gestureDetector.CancelGesture()`. This guarantees the internal `_isPinching` boolean is safely reset to `false` and the `PinchEndedEvent` is fired.

### 2. Hysteresis (Preventing Input Flicker)
**The Problem:**
Human hands shake. If we use a single threshold to detect a pinch (e.g., `distance < 0.03m`), a shaking hand resting exactly at `0.03m` will trigger "Pinch Started" and "Pinch Ended" 60 times a second. This causes guns to stutter-fire rapidly and breaks game logic.

**The Solution:**
We use **Hysteresis**—the dependency of the state of a system on its history. 
*   **To start a pinch:** The fingers must get very close (e.g., `< 0.03m`).
*   **To end a pinch:** The fingers must pull significantly further apart (e.g., `> 0.045m`).
*   **The Buffer Zone:** If the distance is hovering at `0.035m`, the system looks at the *previous state*. It requires deliberate, wide movement to change states, entirely eliminating hardware micro-jitter!

*(Note: The `HandTrackingConfig.OnValidate()` method programmatically guarantees that the release threshold is always mathematically higher than the pull threshold, preventing designers from accidentally breaking the Hysteresis logic in the Unity Editor.)*

### 3. Zero-Allocation Subsystem Queries
**The Problem:**
To find the XR Hand tracking hardware, Unity provides `SubsystemManager.GetSubsystems<XRHandSubsystem>()`. If you pass it a standard `List`, Unity creates a brand new List in the C# heap memory every single time it searches for the hardware, triggering the Garbage Collector.

**The Solution:**