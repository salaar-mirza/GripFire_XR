using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARFps.Features.RoomMapping
{ 
    public class RoomMappingUIView : MonoBehaviour
    {
        public event Action OnFinishClicked;
        public event Action OnUndoClicked;
        public event Action OnSecondaryClicked;

        [Header("UI References")]
        [SerializeField] private Button _finishScanButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _secondaryButton;
        [SerializeField] private TextMeshProUGUI _instructionText;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private TextMeshProUGUI _secondaryButtonText;
        [SerializeField] private GameObject _uiContainer; // The parent panel to toggle on/off

        private void OnEnable()
        {
            if (_finishScanButton != null)
                _finishScanButton.onClick.AddListener(HandleFinishClicked);
            if (_undoButton != null) _undoButton.onClick.AddListener(HandleUndoClicked);
            if (_secondaryButton != null)
                _secondaryButton.onClick.AddListener(HandleSecondaryClicked);
        }

        private void OnDisable()
        {
            if (_finishScanButton != null)
                _finishScanButton.onClick.RemoveListener(HandleFinishClicked);
            if (_undoButton != null) _undoButton.onClick.RemoveListener(HandleUndoClicked);
            if (_secondaryButton != null) _secondaryButton.onClick.RemoveListener(HandleSecondaryClicked);
        }

        private void HandleFinishClicked() => OnFinishClicked?.Invoke();
        
        public void UpdateInstructionText(string text) => _instructionText.text = text;
        public void UpdateButtonText(string text) => _buttonText.text = text;
        public string GetButtonText() => _buttonText.text;
        
        public void SetUndoVisibility(bool showUndo)
        {
            if (_undoButton != null) _undoButton.gameObject.SetActive(showUndo);
        }

        public void UpdateSecondaryButtonText(string text)
        {
            if (_secondaryButtonText != null) _secondaryButtonText.text = text;
        }
        public void SetSecondaryVisibility(bool show)
        {
            if (_secondaryButton != null) _secondaryButton.gameObject.SetActive(show);
        }


        public void Show() => _uiContainer.SetActive(true);
        public void Hide() => _uiContainer.SetActive(false);

        private void HandleUndoClicked() => OnUndoClicked?.Invoke();
        private void HandleSecondaryClicked() => OnSecondaryClicked?.Invoke();
    }
}