using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Core.State;
using ARFps.Core.State.Events;

namespace ARFps.Features.Sandbox
{
    public class SandboxUIController : IService
    {
        private readonly SandboxUIView _view;
        private SandboxWeaponMode _currentMode = SandboxWeaponMode.Bullets;
        private SandboxLaserMode _laserMode = SandboxLaserMode.Destroy;
        private SandboxHoldAction _holdAction = SandboxHoldAction.Drop;
        private bool _isHoldingObject = false;

        public SandboxUIController(SandboxUIView view)
        {
            _view = view;
          
        }

         
        public void OnInit()
        {
            if (_view.ToggleWeaponModeButton != null) _view.ToggleWeaponModeButton.onClick.AddListener(ToggleMode);
            if (_view.ContextActionButton != null) _view.ContextActionButton.onClick.AddListener(ToggleContextAction);
            if (_view.BackButton != null) _view.BackButton.onClick.AddListener(GoBackToMenu);
            UpdateUI();
            
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
            EventBus<ObjectGrabbedStateChangedEvent>.Subscribe(OnObjectGrabbed);
            _view.gameObject.SetActive(false); // Hide the UI immediately on boot
        }
        
        private void GoBackToMenu()
        {
            GameService.Get<GameStateService>().ChangeState(GameState.ModeSelection);
        }

         
        private void OnGameStateChanged(GameStateChangedEvent e)
        {
            _view.gameObject.SetActive(e.CurrentState == GameState.Sandbox);
        }
 
        public void OnDispose()
        {
            if (_view.ToggleWeaponModeButton != null) _view.ToggleWeaponModeButton.onClick.RemoveListener(ToggleMode);
            if (_view.ContextActionButton != null) _view.ContextActionButton.onClick.RemoveListener(ToggleContextAction);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EventBus<ObjectGrabbedStateChangedEvent>.Unsubscribe(OnObjectGrabbed);
            if (_view.BackButton != null) _view.BackButton.onClick.RemoveListener(GoBackToMenu);
        }

        
        private void ToggleMode()
        {
            if (_currentMode == SandboxWeaponMode.Bullets) _currentMode = SandboxWeaponMode.BouncingBalls;
            else if (_currentMode == SandboxWeaponMode.BouncingBalls) _currentMode = SandboxWeaponMode.Laser;
            else if (_currentMode == SandboxWeaponMode.Laser) _currentMode = SandboxWeaponMode.Balloons;
            else if (_currentMode == SandboxWeaponMode.Balloons) _currentMode = SandboxWeaponMode.Smoke;
            else _currentMode = SandboxWeaponMode.Bullets;
            
            UpdateUI();
            EventBus<SandboxWeaponModeChangedEvent>.Publish(new SandboxWeaponModeChangedEvent(_currentMode));
        }

        private void ToggleContextAction()
        {
            if (_currentMode != SandboxWeaponMode.Laser) return;
            
            if (_isHoldingObject)
            {
                _holdAction = _holdAction == SandboxHoldAction.Drop ? SandboxHoldAction.Launch : SandboxHoldAction.Drop;
                EventBus<HoldActionToggledEvent>.Publish(new HoldActionToggledEvent(_holdAction));
            }
            else
            {
                _laserMode = _laserMode == SandboxLaserMode.Destroy ? SandboxLaserMode.TractorBeam : SandboxLaserMode.Destroy;
                EventBus<LaserModeToggledEvent>.Publish(new LaserModeToggledEvent(_laserMode));
            }
            UpdateUI();
        }
        
        private void OnObjectGrabbed(ObjectGrabbedStateChangedEvent e)
        {
            _isHoldingObject = e.IsGrabbing;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_view.WeaponModeText != null) _view.WeaponModeText.text = $"Mode: {_currentMode}";
            
            if (_view.ContextActionText != null)
            {
                if (_currentMode != SandboxWeaponMode.Laser) 
                {
                    _view.ContextActionText.text = _currentMode == SandboxWeaponMode.Balloons ? "Blow into Mic!" : "";
                }
                else if (_isHoldingObject) 
                {
                    _view.ContextActionText.text = $"ANTI-GRAVITY HOLD\nTap Screen to: {_holdAction.ToString().ToUpper()}";
                }
                else 
                {
                    _view.ContextActionText.text = $"Laser Mode: {_laserMode.ToString().ToUpper()}";
                }
            }

            if (_view.ContextActionButton != null)
            {
                // Only show the Context button if we are using the Laser!
                _view.ContextActionButton.gameObject.SetActive(_currentMode == SandboxWeaponMode.Laser);
                
                // Update the text INSIDE the button so the player knows what clicking it does!
                if (_view.ContextButtonText != null && _currentMode == SandboxWeaponMode.Laser)
                {
                    if (_isHoldingObject) _view.ContextButtonText.text = _holdAction == SandboxHoldAction.Drop ? "Switch to LAUNCH" : "Switch to DROP";
                    else _view.ContextButtonText.text = _laserMode == SandboxLaserMode.Destroy ? "Switch to TRACTOR" : "Switch to DESTROY";
                }
            }
        }
    }
}