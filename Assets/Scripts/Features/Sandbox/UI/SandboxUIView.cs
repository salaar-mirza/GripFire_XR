using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARFps.Features.Sandbox
{
    /// <summary>
    /// Dumb view holding references to the Sandbox UI elements.
    /// </summary>
    public class SandboxUIView : MonoBehaviour
    {
        public Button ToggleWeaponModeButton;
        public Button ContextActionButton;
        public TextMeshProUGUI ContextButtonText; // The text INSIDE the context button
        public TextMeshProUGUI WeaponModeText;
        public TextMeshProUGUI ContextActionText;
         
        public Button BackButton;
    }
}