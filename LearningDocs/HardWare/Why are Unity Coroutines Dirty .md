1. Why are Unity Coroutines "Dirty"?
In standard Unity development, people use StartCoroutine() to handle things over time (like our Lightsaber animation).
•
The Dependency Problem: A Coroutine requires a MonoBehaviour to run. Our Services are pure C# classes. To use a Coroutine, we would have to create a fake, empty GameObject, attach a dummy script to it, and tell it to run our logic. That violates our decoupled architecture.
•
The Memory Problem (Garbage Collection): Under the hood, a Coroutine returns an IEnumerator. Every time you yield return new WaitForSeconds(1f), C# creates a tiny object on the Heap. When it finishes, that object becomes garbage. Doing this constantly triggers the Garbage Collector, causing fatal frame-drops (lag spikes) in mobile AR.