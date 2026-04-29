using ARFps.Core.Events;

namespace ARFps.Features.PlayerInput.Events
{
    /// <summary>
    /// Published when the player initiates the primary fire action (e.g., touches the screen).
    /// </summary>
    public readonly struct PrimaryFireStartedEvent : IGameEvent
    {
    }

    /// <summary>
    /// Published when the player stops the primary fire action (e.g., releases the screen).
    /// </summary>
    public readonly struct PrimaryFireEndedEvent : IGameEvent
    {
    }

    /// <summary>
    /// Published when the microphone detects a loud noise, such as the player blowing.
    /// </summary>
    public readonly struct BlowDetectedEvent : IGameEvent
    {
        public readonly float Volume;
        public BlowDetectedEvent(float volume) { Volume = volume; }
    }
}