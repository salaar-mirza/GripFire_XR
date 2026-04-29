using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.PlayerInput.Events;
using ARFps.Core.State;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ARFps.Features.PlayerInput
{
    /// <summary>
    /// Service responsible for polling screen touches and broadcasting player input events.
    /// </summary>
    public class TouchInputService : IService, ITickable
    {
        private bool _isFiring;
        private GameStateService _gameStateService;

        public void OnInit()
        {
            _gameStateService = GameService.Get<GameStateService>();
            // Required to use the modern Touch.activeTouches API
            EnhancedTouchSupport.Enable();
        }

        public void OnTick()
        {
            // Do not process any touch input if we are not in the 'Playing' or 'Sandbox' state.
            if (_gameStateService.CurrentState != GameState.Playing && _gameStateService.CurrentState != GameState.Sandbox)
            {
                // If we were firing and the state changed, force stop firing.
                if (_isFiring)
                {
                    _isFiring = false;
                    EventBus<PrimaryFireEndedEvent>.Publish(new PrimaryFireEndedEvent());
                }
                return;
            }
            
            // If there is at least one finger on the screen, consider the trigger "held"
            bool isTouching = false;
            if (Touch.activeTouches.Count > 0)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(Touch.activeTouches[0].finger.index))
                {
                    isTouching = true;
                }
            }

#if UNITY_EDITOR
            // Bulletproof fallback for Editor testing with the mouse
            if (Mouse.current != null && Mouse.current.leftButton.isPressed && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) isTouching = true;
#endif

            if (isTouching && !_isFiring)
            {
                _isFiring = true;
                EventBus<PrimaryFireStartedEvent>.Publish(new PrimaryFireStartedEvent());
            }
            else if (!isTouching && _isFiring)
            {
                _isFiring = false;
                EventBus<PrimaryFireEndedEvent>.Publish(new PrimaryFireEndedEvent());
            }
        }

        public void OnDispose()
        {
            // Always clean up to prevent memory leaks when the scene unloads
            if (_isFiring)
            {
                _isFiring = false;
                EventBus<PrimaryFireEndedEvent>.Publish(new PrimaryFireEndedEvent());
            }
            
            EnhancedTouchSupport.Disable();
        }
    }
}