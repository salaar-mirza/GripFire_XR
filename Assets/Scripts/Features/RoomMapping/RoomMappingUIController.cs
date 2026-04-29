using ARFps.Core.Events;
using ARFps.Core.Services;
using ARFps.Features.RoomMapping.Events;

namespace ARFps.Features.RoomMapping
{ 
    public class RoomMappingUIController : IService
    {
        private readonly RoomMappingUIView _view;
        private ManualRoomMappingService _mappingService;
        public RoomMappingUIController(RoomMappingUIView view)
        {
            _view = view;
        }

        public void OnInit()
        {
            _mappingService = GameService.Get<ManualRoomMappingService>();
            
            if (_view != null)
            {
                _view.OnFinishClicked += HandleActionButtonClicked;
                _view.OnUndoClicked += _mappingService.UndoLastAction;
                _view.OnSecondaryClicked += HandleSecondaryButtonClicked;
            }
            // Listen for state changes to update UI text
            EventBus<FloorOriginSetEvent>.Subscribe(OnFloorSet);
            EventBus<FloorOriginUndoneEvent>.Subscribe(OnFloorUndone);
            EventBus<CeilingHeightSetEvent>.Subscribe(OnCeilingSet);
            EventBus<CeilingHeightUndoneEvent>.Subscribe(OnCeilingUndone);
            EventBus<ObstacleMappingStartedEvent>.Subscribe(OnObstacleMappingStarted);
            EventBus<ObstacleHeightSetEvent>.Subscribe(OnObstacleHeightSet);
            EventBus<ObstaclePointAddedEvent>.Subscribe(OnObstaclePointAdded);
            EventBus<RoomBoundariesRestoredEvent>.Subscribe(OnRoomBoundariesRestored);
            EventBus<ObstaclePointRemovedEvent>.Subscribe(OnObstaclePointRemoved);
            EventBus<ObstacleRestoredEvent>.Subscribe(OnObstacleRestored);
            EventBus<BoundaryPointAddedEvent>.Subscribe(OnBoundaryPointAdded);
            EventBus<BoundaryPointRemovedEvent>.Subscribe(OnBoundaryPointRemoved);
            EventBus<PlayableAreaDefinedEvent>.Subscribe(OnAreaDefined);
 
            _view?.Show();
            SetUIState("Aim the laser at the center of your floor and tap ACTION.", "SET FLOOR ORIGIN", false);
        }

        private void SetUIState(string instruction, string primaryButtonText, bool showSecondary, string secondaryButtonText = "")
        {
            if (_view == null) return;
            _view.UpdateInstructionText(instruction);
            _view.UpdateButtonText(primaryButtonText);
            _view.SetSecondaryVisibility(showSecondary);
            if (showSecondary && !string.IsNullOrEmpty(secondaryButtonText)) _view.UpdateSecondaryButtonText(secondaryButtonText);
            UpdateUndoVisibility();
        }

        private void HandleActionButtonClicked()
        {
            _mappingService.ProcessPlayerAction();
        }

        private void HandleSecondaryButtonClicked()
        {
            if (_mappingService.CurrentState == MappingState.DefiningBoundaries)
            {
                _mappingService.FinishRoomBoundaries();
            }
            else if (_mappingService.CurrentState == MappingState.SettingObstacleHeight)
            {
                _mappingService.CompleteMapping(); // They are skipping/done with obstacles
            }
            else if (_mappingService.CurrentState == MappingState.DefiningObstacleBoundaries)
            {
                _mappingService.FinishCurrentObstacle();
            }
        }

        private void OnFloorSet(FloorOriginSetEvent e)
        {
            SetUIState("Stand up, hold phone at desired ceiling height, and tap ACTION.", "SET CEILING HEIGHT", false);
        }
        
        private void OnFloorUndone(FloorOriginUndoneEvent e)
        {
            SetUIState("Aim the laser at the center of your floor and tap ACTION.", "SET FLOOR ORIGIN", false);
        }
 
        private void OnCeilingSet(CeilingHeightSetEvent e)
        {
            SetUIState("Aim the laser at the corners of your room. Tap ACTION at each corner.", "ADD CORNER", false);
        }
        
        private void OnCeilingUndone(CeilingHeightUndoneEvent e)
        {
            // Basically same UI state as setting floor
            OnFloorSet(new FloorOriginSetEvent());
        }
           
        private void OnRoomBoundariesRestored(RoomBoundariesRestoredEvent e)
        {
            OnCeilingSet(new CeilingHeightSetEvent());
            OnBoundaryPointAdded(new BoundaryPointAddedEvent()); // Refresh secondary button
        }
        
        private void OnBoundaryPointAdded(BoundaryPointAddedEvent e)
        {
            UpdateUndoVisibility();
            if (_mappingService.BoundaryPoints.Count >= _mappingService.MinCornersRequired)
            {
                _view?.SetSecondaryVisibility(true);
                _view?.UpdateSecondaryButtonText("FINISH ROOM");
            }
        }
 
        private void OnBoundaryPointRemoved(BoundaryPointRemovedEvent e)
        {
            UpdateUndoVisibility();
            // Hide the finish button if the user hit Undo and dropped below the required corner count!
            if (_mappingService.CurrentState == MappingState.DefiningBoundaries && 
                _mappingService.BoundaryPoints.Count < _mappingService.MinCornersRequired)
            {
                _view?.SetSecondaryVisibility(false);
            }
        }
        
        private void OnObstaclePointRemoved(ObstaclePointRemovedEvent e) => UpdateUndoVisibility();
         
        private void OnObstacleMappingStarted(ObstacleMappingStartedEvent e)
        {
            SetUIState("Aim at the TOP of a table/couch and tap ACTION. Or tap DONE MAPPING.", "SET OBST. HEIGHT", true, "DONE MAPPING");
        }
         
        private void OnObstacleRestored(ObstacleRestoredEvent e) => OnObstacleHeightSet(new ObstacleHeightSetEvent());
 
        private void OnObstacleHeightSet(ObstacleHeightSetEvent e)
        {
            SetUIState("Aim at the FLOOR to trace the edges of the obstacle. Tap ACTION at each corner.", "ADD OBST. CORNER", false);
        }
 
        private void OnObstaclePointAdded(ObstaclePointAddedEvent e)
        {
            UpdateUndoVisibility();
            if (_mappingService.CurrentObstaclePoints.Count >= 3)
            {
                _view?.SetSecondaryVisibility(true);
                _view?.UpdateSecondaryButtonText("FINISH OBSTACLE");
            }
        }

        private void UpdateUndoVisibility()
        {
            _view?.SetUndoVisibility(_mappingService.CanUndo);
        }
 
        private void OnAreaDefined(PlayableAreaDefinedEvent e) => _view?.Hide();

        public void OnDispose()
        {
            if (_view != null)
            {
                _view.OnFinishClicked -= HandleActionButtonClicked;
                _view.OnUndoClicked -= _mappingService.UndoLastAction;
                _view.OnSecondaryClicked -= HandleSecondaryButtonClicked;
            }
            EventBus<FloorOriginSetEvent>.Unsubscribe(OnFloorSet);
            EventBus<FloorOriginUndoneEvent>.Unsubscribe(OnFloorUndone);
            EventBus<CeilingHeightSetEvent>.Unsubscribe(OnCeilingSet);
            EventBus<CeilingHeightUndoneEvent>.Unsubscribe(OnCeilingUndone);
            EventBus<ObstacleMappingStartedEvent>.Unsubscribe(OnObstacleMappingStarted);
            EventBus<ObstacleHeightSetEvent>.Unsubscribe(OnObstacleHeightSet);
            EventBus<ObstaclePointAddedEvent>.Unsubscribe(OnObstaclePointAdded);
            EventBus<ObstacleRestoredEvent>.Unsubscribe(OnObstacleRestored);
            EventBus<ObstaclePointRemovedEvent>.Unsubscribe(OnObstaclePointRemoved);
            EventBus<RoomBoundariesRestoredEvent>.Unsubscribe(OnRoomBoundariesRestored);
            EventBus<BoundaryPointAddedEvent>.Unsubscribe(OnBoundaryPointAdded);
            EventBus<BoundaryPointRemovedEvent>.Unsubscribe(OnBoundaryPointRemoved);
            EventBus<PlayableAreaDefinedEvent>.Unsubscribe(OnAreaDefined);
        }
    }
}