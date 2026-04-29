namespace ARFps.Core.Services
{
    /// <summary>
    /// Defines the base lifecycle contract for services managed by the GameService locator.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Called after all services are registered. 
        /// Safe to use GameService.Get<T>() here to resolve dependencies.
        /// </summary>
        void OnInit();

        /// <summary>
        /// Called during application shutdown or scene destruction. 
        /// Used to clean up resources, unregister events, and prevent memory leaks.
        /// </summary>
        void OnDispose();
    }
}