using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARFps.Features.Player.UI
{
    public class CombatUIView : MonoBehaviour
    {
        [Header("HUD")]
        public GameObject HUDPanel;
        public TextMeshProUGUI HealthText;
        public Button BackButton;

        [Header("Game Over")]
        public GameObject GameOverPanel;
        public Button MainMenuButton;
    }
}