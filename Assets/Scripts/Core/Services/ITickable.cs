namespace ARFps.Core.Services
{
    /// <summary>
    /// Contract for pure C# classes that require a per-frame update loop.
    /// </summary>
    public interface ITickable
    {
        void OnTick();
    }
}