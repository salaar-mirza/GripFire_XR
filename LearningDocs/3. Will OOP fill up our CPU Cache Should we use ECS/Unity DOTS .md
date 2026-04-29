3. Will OOP fill up our CPU Cache? Should we use ECS/Unity DOTS?
You are talking about Data Locality and CPU Cache Misses. You are 100% correct that standard Object-Oriented Programming scatters data randomly across the Heap, making the CPU fetch data slowly.
•
Do we need Unity DOTS (ECS)? NO. DOTS is designed for games with 10,000 to 100,000 entities moving simultaneously (like a massive city builder). For an AR game with a single player and a Swarm of 30-50 enemies, DOTS is severe over-engineering. The time it would take to write it isn't worth the microsecond of performance gained.
•
How our architecture solves this: Our Object Pools actually solve a lot of the cache miss problems! Because we instantiate all our bullets and enemies at startup and keep them in a contiguous pool, they end up packed much closer together in memory than if we instantiated them randomly during gameplay.