# Memory Hierarchy & Data-Oriented Design (ECS)_Notes.md

## The Core Concept: The Memory Bottleneck
Modern CPUs process instructions (1 nanosecond) much faster than RAM can provide data (100 nanoseconds). To prevent the CPU from constantly waiting, hardware uses a Memory Hierarchy of ultra-fast, temporary storage built directly into the processor chip:
* **Registers:** Instantaneous math workspace.
* **L1 Cache:** Extremely fast (~1ns), very small.
* **L2/L3 Cache:** Fast (~10ns), shared across cores.
* **RAM:** Slow (~100ns), massive storage.

## The Cache Miss (The Frame Drop Killer)
When the CPU needs data, it checks the L1 Cache first (Cache Hit). If the data isn't there (Cache Miss), the CPU halts execution to fetch it from slow RAM. Standard Object-Oriented Programming (OOP) causes massive Cache Misses because pulling a bulky Object (containing strings, meshes, and floats) fills up the L1 Cache instantly with irrelevant data.

## The Solution: Data-Oriented Design (ECS)
Instead of storing data inside Objects (Array of Structures - AoS), we separate the data by type into tightly packed arrays (Structure of Arrays - SoA). 
* Example: `int[] healths`, `Vector3[] positions`.
When the CPU fetches the `healths` array, the L1 Cache is filled entirely with relevant integer data, allowing the CPU to process thousands of entities sequentially with zero Cache Misses.

## Key Terminology for Interviews
* **Data-Oriented Design (DOD):** Writing code optimized for the CPU's physical memory layout rather than human-readable objects.
* **ECS (Entity Component System):** A software architectural pattern that strictly separates Data (Components) from Logic (Systems) to achieve DOD.
* **Cache Miss:** A costly hardware penalty when the CPU must wait for slow RAM.