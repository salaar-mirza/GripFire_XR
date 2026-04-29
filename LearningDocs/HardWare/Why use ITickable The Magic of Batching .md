2. Why use ITickable? (The Magic of Batching)
Instead of putting Update() on 50 different MonoBehaviours, we use ITickable.
•
The C++ Bridge: Unity's engine is C++. Your code is C#. Every time Unity calls Update() on a MonoBehaviour, it has to cross a "bridge" from C++ to C#. Crossing that bridge 50 times per frame is slow.
•
Our Solution: Our GameInitializer is the only thing with an Update() method. It crosses the bridge exactly once per frame, and then loops through a list of our pure C# ITickable services. This is incredibly fast and efficient.