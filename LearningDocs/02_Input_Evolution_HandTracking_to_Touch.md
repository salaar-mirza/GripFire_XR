# Input Evolution: Hand Tracking to Touch Input

## The Original Plan: XR Hand Tracking
We initially built the input system using Unity's `XR Hands` package. The architecture was perfectly decoupled:
1. **ARHandTracker (Reader):** Hooked into `SubsystemManager` to read raw joint positions (Thumb Tip, Index Proximal).
2. **ARGestureDetector (Logic):** Calculated the distance between joints and applied hysteresis (pull/release thresholds).
3. **HandTrackingConfig (Data):** Allowed designers to tweak those thresholds via a ScriptableObject.

## The Hardware Reality (Why we pivoted)
When we built to a standard Android phone, the system silently returned no hand data. 
**The Catch:** Google's native ARCore for standard mobile phones does *not* support articulated 3D hand tracking natively. `XR Hands` is primarily designed for dedicated OpenXR headsets (like Meta Quest, HoloLens, or Apple Vision Pro) which have the hardware to track 26 distinct hand joints.

Since the core mechanic involves holding the phone as a pistol grip, we needed a mobile-friendly input method. We pivoted to Screen Touch.

## The Architectural Win: Agnostic Events
In a tightly coupled game, changing the core input method from "Hand Pinch" to "Screen Tap" would require rewriting the Weapon scripts, the Player scripts, and the UI.

Because of **RULE 3 (The Event Bus)**, we didn't have to touch the Weapon system at all. 

Instead of creating a `ScreenTappedEvent`, we created **Agnostic Events**:
```csharp
public struct PrimaryFireStartedEvent : IGameEvent { }
public struct PrimaryFireEndedEvent : IGameEvent { }
```
By naming it based on the *intent* rather than the *action*, the `WeaponService` just listens for `PrimaryFireStartedEvent`. It has no idea if the event came from a finger pinch, a screen tap, a Bluetooth controller, or a voice command.

## TouchInputService: Coding Choices
We created a pure C# `TouchInputService` to replace the Hand Tracking manager. 

**Choice 1: EnhancedTouch API**
We used `UnityEngine.InputSystem.EnhancedTouch`. This modern API allows us to poll `Touch.activeTouches.Count` directly in C# without needing to set up complex `InputActionAsset` files or use MonoBehaviours.

**Choice 2: Editor Simulation Fallback**
Testing mobile touches on a PC is notoriously annoying. We added a compiler directive fallback specifically for the Unity Editor to map the left mouse button to a touch:
```csharp
#if UNITY_EDITOR
if (Mouse.current != null && Mouse.current.leftButton.isPressed) isTouching = true;
#endif
```
Because this is wrapped in `#if UNITY_EDITOR`, this code is completely stripped out when compiling the APK for Android, ensuring zero performance overhead on the mobile build.

## Summary
We kept the Hand Tracking code in the repository (it just lies dormant and doesn't register). If this game is ever ported to a Meta Quest, we simply swap the `TouchInputService` registration for the `HandTrackingService` registration in `GameInitializer`, and the game instantly becomes gesture-controlled again!