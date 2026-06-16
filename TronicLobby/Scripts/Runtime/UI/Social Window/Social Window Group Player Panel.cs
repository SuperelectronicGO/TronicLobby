using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace TronicSoft.Lobby.Runtime
{
    public class SocialWindowGroupPlayerPanel : SocialPanelItem
    {
        [SerializeField] private Button _promoteButton;
        [SerializeField] private Button _kickButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private GameObject _ownerIcon;
        [SerializeField] private RectTransform _horizontalLayoutGroup;
        private CSteamID _userID;
        private void Awake()
        {
            SetPermissions(false, false);
        }
        public void SetUserID(CSteamID id) => _userID = id;
        public override void UpdatePanelInfo(string name, string activity, Texture2D icon)
        {
            base.UpdatePanelInfo(name, activity, icon);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_horizontalLayoutGroup);
        }
        public void SetPermissions(bool isLobbyOwner, bool isOwnerProfile)
        {
            if (isOwnerProfile)
            {
                _promoteButton.gameObject.SetActive(false);
                _kickButton.gameObject.SetActive(false);
                return;
            }
            if (isLobbyOwner)
            {
                _promoteButton.gameObject.SetActive(true);
                _kickButton.gameObject.SetActive(true);
                _promoteButton.onClick.AddListener(() => PromotePlayer());
                _kickButton.onClick.AddListener(() => KickPlayer());
            }
            else
            {
                _promoteButton.onClick.RemoveAllListeners();
                _kickButton.onClick.RemoveAllListeners();
                _promoteButton.gameObject.SetActive(false);
                _kickButton.gameObject.SetActive(false);
            }
        }
        public void SetIfAllowedLeave(bool canLeave)
        {
            _leaveButton.gameObject.SetActive(canLeave);
            if (!canLeave) return;
            _leaveButton.onClick.AddListener(() => LeaveLobby());
        }
        public void SetOwnerIcon(bool isProfileForLobbyOwner)
        {

            _ownerIcon.SetActive(isProfileForLobbyOwner);
        }
        private void PromotePlayer()
        {
            LobbyDataManager.Instance.PromoteUserToLobbyOwner(_userID);
        }
        private void KickPlayer()
        {
            LobbyDataManager.Instance.KickUserFromLobby(_userID);
        }
        private void LeaveLobby()
        {
            LobbyDataManager.Instance.LeaveCurrentLobby();
        }
    }
}
