using UnityEngine;
using UnityEngine.UI;
using Steamworks;
namespace TronicSoft.Lobby.Runtime
{
    public class SocialWindowInvitePanel : SocialPanelItem
    {
        [SerializeField] private Button _acceptButton;
        private SteamInviteDefenition _invite;

        private void Awake() => _acceptButton.onClick.AddListener(AcceptInvite);

        private void OnDestroy() => _acceptButton.onClick.RemoveAllListeners();

        public void SetInvite(SteamInviteDefenition i) => _invite = i;
        private void AcceptInvite()
        {
            LobbyDataManager.Instance.AcceptLobbyInvite(_invite);
        }


    }
}
