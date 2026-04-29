using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using UnityEngine;
namespace ARFps.Core.UI
{
    public class ModeSelectionController : IService
    {
        private readonly ModeSelectionView _view;

        public ModeSelectionController(ModeSelectionView view)
        {
            _view = view;
        }

        public void OnInit()
        {
            if (_view.CombatModeButton != null) _view.CombatModeButton.onClick.AddListener(() => GameService.Get<GameStateService>().ChangeState(GameState.Playing));
            if (_view.SandboxModeButton != null) _view.SandboxModeButton.onClick.AddListener(() => GameService.Get<GameStateService>().ChangeState(GameState.Sandbox));
            if (_view.ExitButton != null) _view.ExitButton.onClick.AddListener(() => Application.Quit());
            
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            _view.gameObject.SetActive(false); // Hide on boot
        }

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            // Only show this UI when the room finishes mapping and we are waiting for a choice
            _view.gameObject.SetActive(e.CurrentState == GameState.ModeSelection);
        }

        public void OnDispose()
        {
            if (_view.CombatModeButton != null) _view.CombatModeButton.onClick.RemoveAllListeners();
            if (_view.SandboxModeButton != null) _view.SandboxModeButton.onClick.RemoveAllListeners();
            if (_view.ExitButton != null) _view.ExitButton.onClick.RemoveAllListeners();
            
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
        }
    }
}