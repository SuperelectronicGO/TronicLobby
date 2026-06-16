using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TronicSoft.Lobby.Runtime
{
    [RequireComponent(typeof(Button))]
    public class SocialWindowTooltipButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _tooltipText;
        private Button _button;

        public void Check() => _tooltipText.enabled = _button.interactable;
        public void Hide() => _tooltipText.enabled = false;
        private void Awake() => _button = GetComponent<Button>();
    }
}
