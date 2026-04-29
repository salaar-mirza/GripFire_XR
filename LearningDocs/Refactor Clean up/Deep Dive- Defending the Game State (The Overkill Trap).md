# 🧠 Deep Dive: Defending the Game State (The Overkill Trap)

## 🛡️ The Scenario
When dealing with health systems in a highly decoupled, event-driven game, multiple systems can inflict damage at the exact same time. For example:
1. An enemy ant bites the player.
2. A lingering poison effect ticks.
3. The player steps out of bounds.

If the player has `5 HP` left, and all three of these hit the player on the exact same frame, the damage events are processed sequentially in a fraction of a millisecond. 

## 💥 The "Overkill" Bug
If your Health Service simply looks like this:

In the scenario above, the health drops to `0`, then `-5`, then `-15`. 
Because `-5` and `-15` are also `<= 0`, the `PlayerDiedEvent` gets published **three separate times**. 

If your Level Director, UI Service, and Audio Service are listening to `PlayerDiedEvent`, the UI will try to spawn three "Game Over" screens, the Audio system will play the death sound three times concurrently (causing painful audio clipping), and the Level Director might crash trying to reset the level while it's already resetting.

## 🛠️ The Solution: The Guard Clause
To protect your game state, always assume a dead entity can still take damage. 
