# AR FPS: Core Architecture Fundamentals

This document serves as a cheat sheet and learning guide for the custom MVC-S architecture used in this project.

## 1. Feature-Based Folder Structure
Instead of grouping files by type (e.g., all services in one folder, all events in another), we group by **Feature**. 
This keeps related logic together and makes the project highly scalable.

**Example Structure:**
`/Features/Weapon/` -> Contains `WeaponService.cs`, `WeaponEvents.cs`, `WeaponController.cs`
`/Features/Audio/` -> Contains `AudioService.cs`, `AudioData.asset`

---

## 2. The "Vending Machine" (Service Locator)
**File:** `GameService.cs`
**Rule:** NO Singletons. NO `GameObject.Find()`. 

All global managers are pure C# classes implementing `IService`. They are stored in a central dictionary.

### The Two-Phase Boot Sequence
Controlled by `GameInitializer.Awake()`, preventing Null Reference Exceptions.

*   **Phase 1 (Register):** Services are instantiated and added to the dictionary. *Do NOT get other services here.*
    ```csharp
    GameService.Register(new AudioService());
    ```
*   **Phase 2 (Init):** `OnInit()` is called on all services. *Safe to get other services here.*
    ```csharp
    public void OnInit() 
    {
        var audio = GameService.Get<AudioService>();
    }
    ```

### Clean Teardown
To prevent static memory leaks between scenes, `GameInitializer.OnDestroy()` loops through all services and calls `OnDispose()`, then clears the `GameService` dictionary.

---

## 3. The "Radio Station" (Event Bus)
**File:** `EventBus.cs`
**Rule:** Systems must not talk directly to each other. They communicate via broadcasted struct payloads.

### Defining an Event
Events must be structs (for zero heap allocation) and implement `IGameEvent`.
```csharp
public struct PlayerDamagedEvent : IGameEvent 
{
    public readonly int Amount;
    public PlayerDamagedEvent(int amount) => Amount = amount;
}
```

### Publishing & Subscribing
```csharp
// Subscribing (e.g., in a UI script)
EventBus<PlayerDamagedEvent>.Subscribe(OnPlayerDamaged);

// Publishing (e.g., in an Enemy script)
EventBus<PlayerDamagedEvent>.Publish(new PlayerDamagedEvent(10));

// Unsubscribing (CRITICAL to prevent memory leaks)
EventBus<PlayerDamagedEvent>.Unsubscribe(OnPlayerDamaged);
```

### Thread Safety (Rule 6 Compliance)
In AR, heavy lifting (like tracking math or audio processing) runs on background threads to save mobile frame rates. Unity APIs crash if called from background threads. 

Our `EventBus` handles this automatically. If `Publish()` is called from a background thread, it captures the event in a Concurrent Queue and waits for the `GameInitializer` to flush it on the Main Unity Thread during the next `Update()`.

---

## Next Steps / Upcoming Features
- [ ] AR Hand Tracking integration (XR Hands)
- [ ] Gesture Detection (Pistol Grip / Trigger Pull)
- [ ] Aim Assist Service (Gyroscope Stabilization)