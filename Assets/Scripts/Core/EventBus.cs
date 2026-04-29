using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ARFps.Core.Events
{
    /// <summary>
    /// Manages the lifecycle and thread-safety of all EventBus<T> instances.
    /// </summary>
    public static class EventBus
    {
        private static readonly List<Action> s_clearActions = new List<Action>();
        private static readonly ConcurrentQueue<Action> s_queuedActions = new ConcurrentQueue<Action>();
        private static int s_mainThreadId;

        /// <summary>
        /// Captures the Main Thread ID. Must be called from GameInitializer.Awake().
        /// </summary>
        public static void InitializeMainThreadId() => s_mainThreadId = Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// Checks if the current execution context is the Unity Main Thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == s_mainThreadId;

        internal static void EnqueueMainThreadAction(Action action) => s_queuedActions.Enqueue(action);

        /// <summary>
        /// Flushes the concurrent queue on the main thread. Called by GameInitializer.Update().
        /// </summary>
        public static void ProcessMainThreadActions()
        {
            while (s_queuedActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        /// <summary>
        /// Registers a clear action from a generic EventBus<T> instance.
        /// Internal access prevents outside classes from modifying this list.
        /// </summary>
        internal static void RegisterClearAction(Action clearAction) => s_clearActions.Add(clearAction);

        /// <summary>
        /// Invokes the clear action for every EventBus<T> that has been used,
        /// effectively wiping all listeners from the system.
        /// </summary>
        public static void ClearAllBuses()
        {
            s_clearActions.ForEach(action => action.Invoke());
            s_clearActions.Clear();
            
            // Clear any pending cross-thread actions to avoid zombie invocations
            while (s_queuedActions.TryDequeue(out _)) { }
        }
    }

    /// <summary>
    /// A generic event bus for decoupled communication between systems.
    /// </summary>
    /// <typeparam name="T">The type of event, which must be a struct that implements IGameEvent.</typeparam>
    public static class EventBus<T> where T : IGameEvent
    {
        private static readonly HashSet<Action<T>> s_listeners = new HashSet<Action<T>>();

        static EventBus()
        {
            EventBus.RegisterClearAction(Clear);
        }

        /// <summary>
        /// Subscribes a listener action to the event. The action will be invoked when the event is published.
        /// </summary>
        /// <param name="listener">The action to execute when the event is published.</param>
        public static void Subscribe(Action<T> listener)
        {
            if (listener != null)
            {
                s_listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unsubscribes a listener action from the event.
        /// </summary>
        /// <param name="listener">The action to remove.</param>
        /// <remarks>
        /// IMPORTANT: If you subscribed an anonymous method (a lambda), you must have stored a
        /// reference to it to be able to unsubscribe it later.
        /// Example:
        /// Action<MyEvent> myEventHandler = (e) => { Debug.Log("Event handled!"); };
        /// EventBus<MyEvent>.Subscribe(myEventHandler);
        /// EventBus<MyEvent>.Unsubscribe(myEventHandler);
        /// </remarks>
        public static void Unsubscribe(Action<T> listener)
        {
            if (listener != null)
            {
                s_listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Publishes an event to all subscribed listeners, passing the event data payload.
        /// </summary>
        public static void Publish(T eventData)
        {
            if (!EventBus.IsMainThread)
            {
                EventBus.EnqueueMainThreadAction(() => Publish(eventData));
                return;
            }

            // Create a snapshot of listeners to safely allow them to unsubscribe during the event loop
            // without throwing an InvalidOperationException (Collection modified).
            var currentListeners = new List<Action<T>>(s_listeners);
            foreach (var listener in currentListeners)
            {
                try
                {
                    listener.Invoke(eventData);
                }
                catch (Exception e)
                {
                    // Log the exception to avoid one faulty listener from breaking the entire event chain.
                    Debug.LogError($"[EventBus] Error invoking listener for event {typeof(T).Name}. \n{e}");
                }
            }
        }

        private static void Clear() => s_listeners.Clear();
    }
}