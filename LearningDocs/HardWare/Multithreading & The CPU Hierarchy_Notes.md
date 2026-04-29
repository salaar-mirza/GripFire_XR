# Multithreading & The CPU Hierarchy_Notes.md

## The Core Concept
Processors hit a "Thermal Wall" around 2005—increasing clock speed (GHz) further caused silicon to melt. The hardware solution was adding multiple physical "Cores" to a single chip. The software solution to utilize this hardware is Multithreading.

* **Core (Hardware):** A physical processing unit on the silicon chip.
* **Thread (Software):** A sequence of programmed instructions.
* **The Main Thread:** The primary execution loop of a game engine (Unity/Unreal). It is a strict dictator that exclusively owns the graphics rendering and Engine API (Transforms, GameObjects).

## Thread Safety & Data Transfer
Background Worker Threads must never touch Engine API objects, or a fatal Race Condition will occur.
* **The Solution:** Worker threads calculate raw data (Structs, Vector3s, ints) and place them in a concurrent queue (Event Bus / Mailbox). The Main Thread reads this mailbox during its `Update()` loop and physically moves the 3D objects.

## Context Switching & Thrashing
If you give the OS 100 threads for 8 cores, the OS Scheduler will rapidly pause and swap threads (Time Slicing).
* **Context Switch:** The expensive process of saving one thread's state to RAM and loading another's.
* **Thread Thrashing:** When the CPU is given too many threads and spends more computational power context-switching than actually executing code, resulting in massive frame drops.

## Key Terminology for Interviews
* **Race Condition:** When two threads try to modify the same memory address simultaneously, corrupting data.
* **Time Slicing:** How an OS creates the illusion of running 100 tasks simultaneously on limited hardware.
* **Task / Thread Pool:** A software manager that queues tasks and assigns them efficiently to available cores to prevent Thread Thrashing.