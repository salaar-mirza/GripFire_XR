using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARFps.Core.Services
{
    /// <summary>
    /// A central Service Locator that acts as a registry for all IService implementations.
    /// Provides decoupled access to game systems without relying on singletons.
    /// </summary>
    public static class GameService
    {
        /// <summary>
        /// The internal dictionary that stores registered services, mapping their Type to the instance.
        /// </summary>
        private static readonly Dictionary<Type, IService> s_services = new Dictionary<Type, IService>();

        /// <summary>
        /// Registers a service instance with the locator. This is called during Phase 1 of the boot sequence.
        /// </summary>
        /// <typeparam name="T">The type of the service being registered.</typeparam>
        /// <param name="service">The service instance.</param>
        public static void Register<T>(T service) where T : IService
        {
            var type = typeof(T);
            if (s_services.ContainsKey(type))
            {
                Debug.LogError($"[GameService] Service of type {type.Name} is already registered.");
                return;
            }
            s_services[type] = service;
            Debug.Log($"[GameService] Registered: {type.Name}");
        }

        /// <summary>
        /// Retrieves a registered service instance. This should only be called during or after Phase 2 (OnInit).
        /// </summary>
        /// <typeparam name="T">The type of the service to retrieve.</typeparam>
        /// <returns>The requested service instance, or default(T) if not found.</returns>
        public static T Get<T>() where T : class, IService
        {
            var type = typeof(T);
            if (!s_services.TryGetValue(type, out var service))
            {
                // Returning null for a failed Get is a critical failure.
                throw new InvalidOperationException($"[GameService] Service of type {type.Name} not found. Ensure it is registered before being accessed.");
            }
            return service as T;
        }

        /// <summary>
        /// Clears the service dictionary. Called during shutdown by the GameInitializer.
        /// </summary>
        public static void UnregisterAll()
        {
            s_services.Clear();
        }

        /// <summary>
        /// Provides read-only access to all registered service instances for iteration (e.g., for ticking).
        /// </summary>
        public static IEnumerable<IService> GetAllServices() => s_services.Values;
    }
}