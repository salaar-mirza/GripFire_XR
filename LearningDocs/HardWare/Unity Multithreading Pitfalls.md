# Unity Multithreading Pitfalls

## 1. The Transform Problem (The Unity Engine Barrier)

**Question:** Can we pass `Transform origin` into `Task.Run()`?  
**Answer:** Absolutely **NOT**.

**The Why:**  
`Transform` is a Unity API object. Under the hood, Unity is written in C++. The C# `Transform` you use is just a thin wrapper pointing to a block of C++ memory that Unity manages. Unity's C++ engine is strictly **Single‑Threaded**.  

If your background thread tries to read `origin.position`, Unity will panic and throw a massive exception:

