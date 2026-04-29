using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State;
using ARFps.Core.State.Events;
using ARFps.Features.Player.Events;

namespace ARFps.Features.Player.UI
{
    public class CombatUIController : IService
    {
        private readonly CombatUIView _view;

        public CombatUIController(CombatUIView view)
        {
            _view = view;
        }

        public void OnInit()
        {
            if (_view.BackButton != null) _view.BackButton.onClick.AddListener(ReturnToMenu);
            if (_view.MainMenuButton != null) _view.MainMenuButton.onClick.AddListener(ReturnToMenu);

            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<PlayerHealthChangedEvent>.Subscribe(OnHealthChanged);

            if (_view.HUDPanel != null) _view.HUDPanel.SetActive(false);
            if (_view.GameOverPanel != null) _view.GameOverPanel.SetActive(false);
        }

        private void ReturnToMenu()
            => GameService.Get<GameStateService>().ChangeState(GameState.ModeSelection);

        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            if (_view.HUDPanel != null) _view.HUDPanel.SetActive(e.CurrentState == GameState.Playing);
            if (_view.GameOverPanel != null) _view.GameOverPanel.SetActive(e.CurrentState == GameState.GameOver);
        }

        private void OnHealthChanged(PlayerHealthChangedEvent e)
        {
            if (_view.HealthText != null) _view.HealthText.text = $"Health: {e.CurrentHealth} / {e.MaxHealth}";
        }

        public void OnDispose()
        {
            if (_view.BackButton != null) _view.BackButton.onClick.RemoveListener(ReturnToMenu);
            if (_view.MainMenuButton != null) _view.MainMenuButton.onClick.RemoveListener(ReturnToMenu);

            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<PlayerHealthChangedEvent>.Unsubscribe(OnHealthChanged);
        }
    }
}