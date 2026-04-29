using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State.Events;

namespace ARFps.Core.State
{
    /// <summary>
    /// The centralized manager for the application's Macro State.
    /// </summary>
    public class GameStateService : IService
    {
        public GameState CurrentState { get; private set; } = GameState.Booting;

        public void OnInit() { }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState previousState = CurrentState;
            CurrentState = newState;

            EventBus<GameStateChangedEvent>.Publish(new GameStateChangedEvent(previousState, CurrentState));
        }

        public void OnDispose() { }
    }
}