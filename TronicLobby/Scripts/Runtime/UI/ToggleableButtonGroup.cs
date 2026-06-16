using UnityEngine;
using UnityEngine.UI;

//Script by Ben
namespace TronicSoft.Lobby.Runtime
{
    public class ToggleableButtonGroup : MonoBehaviour
    {
        private Button[] _buttons;

        private void Awake()
        {
            _buttons = GetComponentsInChildren<Button>();

            foreach (var button in _buttons)
            {
                button.onClick.AddListener(() => OnButtonClick(button));
            }
        }

        private void OnEnable()
        {
            if (_buttons.Length > 0)
            {
                _buttons[0].Select();
                OnButtonClick(_buttons[0]);
            }
        }

        private void OnDestroy()
        {
            foreach (var button in _buttons)
            {
                button.onClick.RemoveListener(() => OnButtonClick(button));
            }
        }

        public void ResetGroup()
        {
            foreach (var button in _buttons)
            {
                button.interactable = true;
            }
        }

        private void OnButtonClick(Button button)
        {
            foreach (var b in _buttons)
            {
                b.interactable = true;
            }
            button.interactable = false;
        }
    }
}
