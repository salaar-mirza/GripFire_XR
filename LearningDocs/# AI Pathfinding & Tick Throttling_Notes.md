# AI Pathfinding & Tick Throttling_Notes.md

## The Core Concept: The Cost of A*
Unity's `NavMeshAgent.SetDestination()` triggers the A* pathfinding algorithm. This is a CPU-intensive mathematical search.
*   **The Trap:** Calling `SetDestination()` in an `Update()` or `OnTick()` loop every frame causes exponential CPU spikes as enemy counts increase, leading to severe thermal throttling on mobile devices.

## The Solution: Tick-Rate Throttling (Polling)
AI agents do not need frame-perfect reaction times. You heavily optimize AI by throttling how often they "think".
*   **Implementation:** Give each AI Controller a local timer (e.g., `_pathfindingTimer`). Only execute the expensive `SetDestination()` call when the timer reaches a threshold (e.g., `0.5f` seconds).
*   **The Result:** The AI continues moving physically every frame using its cached path, but only expends CPU power to recalculate the route a few times per second.


Solving the Iron Gate Challenge: Pathfinding Performance
It is completely okay that you weren't sure about this. Pathfinding optimization is usually taught in advanced computer science classes. Let's break down exactly why this happens and how we fix it.
1. Why it destroys the battery (The A* Algorithm)
When you call NavMeshAgent.SetDestination(), Unity doesn't just draw a straight line. It runs the A (A-Star) Pathfinding Algorithm*. This algorithm has to scan the invisible floor, check every obstacle, and mathematically calculate the shortest route around your coffee table. The Time Complexity (Big-O) of A* can be very heavy, often O(E log V) (where E is edges and V is vertices).
If you have 30 enemies, and you call SetDestination inside OnTick() (which runs 60 frames per second): 30 enemies * 60 frames = 1,800 complex pathfinding calculations every single second.
Your mobile phone's CPU will overheat in 3 minutes, the game will stutter, and the battery will drain incredibly fast.
2. The Solution: Tick-Rate Throttling (Cooldowns)
Humans move slowly. If the player takes one step to the left in their living room, the enemies don't need to recalculate their path on that exact millisecond.
If we only recalculate the path twice a second (every 0.5 seconds), the enemy will still look like it's flawlessly chasing the player, but we reduce our calculations from 1,800 per second down to just 60 per second. That is a 96% reduction in CPU load!