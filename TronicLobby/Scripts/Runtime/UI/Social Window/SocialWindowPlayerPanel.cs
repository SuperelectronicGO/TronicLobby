using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace TronicSoft.Lobby.Runtime
{
    public class SocialWindowPlayerPanel : SocialPanelItem
    {
        private SocialWindowInviteTimer _inviteTimer;
        [SerializeField] private Button _profileButton;
        [SerializeField] private Button _inviteButton;
        private CSteamID _panelID;

        public bool HasInviteButton => _inviteButton.gameObject.activeInHierarchy;
        private void Awake()
        {
            _inviteButton.onClick.AddListener(() => InvitePlayer());
            _profileButton.onClick.AddListener(() => OpenPlayerProfile());
        }
        public void OpenPlayerProfile()
        {
            SteamDataManager.Instance.OpenSteamProfileForUser(_panelID);
        }
        public void SetInviteTimer(SocialWindowInviteTimer timer)
        {
            _inviteTimer = timer;
        }
        public void SetPanelID(CSteamID id)
        {
            _panelID = id;
        }
        public void RemoveInviteButton()
        {
            _inviteButton.gameObject.SetActive(false);
        }
        public void InvitePlayer()
        {
            if(LobbyDataManager.Instance.GetCurrentLobby == null)
            {
                Debug.LogError("NOT IN LOBB");
                return;
            }
            LobbyDataManager.Instance.InviteUserToLobby(_panelID);
            //SteamClientManager.Instance.InviteFriendToLobby(panelFriendID);
            _inviteTimer.StartButtonCooldown(_inviteButton, 10f);
        }
    }
}
