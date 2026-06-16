using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace TronicSoft.Lobby.Runtime
{
    public class SocialWindowInviteTimer : MonoBehaviour
    {
        public void StartButtonCooldown(Button button, float duration)
        {
            _ = Cooldown(button, duration);
        }
        private async UniTaskVoid Cooldown(Button button, float duration)
        {
            await UniTask.WaitForSeconds(duration);

            if (button = null)
                button.interactable = true;
        }
    }
}
