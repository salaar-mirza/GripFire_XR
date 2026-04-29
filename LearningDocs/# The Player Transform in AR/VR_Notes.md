# The Player Transform in AR/VR_Notes.md

## The Core Concept: The Camera is the Body
In traditional FPS games, you have a `PlayerController` GameObject with a CharacterController component moving through the world. 
In AR/VR (Spatial Computing), the physical human *is* the controller. 

*   **The Tracking Proxy:** `Camera.main.transform` is the ultimate proxy for the user. Its position and rotation are hardware-locked to the physical device via the gyroscope and camera sensors.
*   **AI Targeting:** To make an AI aggressively chase the user in their real-world environment, you simply feed `Camera.main.transform.position` into the AI's pathfinding destination.