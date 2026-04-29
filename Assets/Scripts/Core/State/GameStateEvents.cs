using ARFps.Core.Events;

namespace ARFps.Core.State.Events
{
    /// <summary>
    /// Published universally whenever the Macro Game State transitions.
    /// </summary>
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly GameState PreviousState;
        public readonly GameState CurrentState;

        public GameStateChangedEvent(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}