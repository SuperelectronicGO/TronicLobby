using Cysharp.Threading.Tasks;
using LoogaSoft.Tools.Runtime;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZLinq;
namespace TronicSoft.Lobby.Runtime
{
    public class SocialWindow : MonoBehaviour
    {
        public event Action<List<SteamInviteDefenition>> OnInvitesRefreshed;
        public event Action<List<SteamInviteDefenition>> OnInvitesOpened;
        public event Action OnWindowEnabled;
        public event Action OnWindowDisabled;

        private int _page = -1;

        [SerializeField] private Transform _groupTransform;
        [SerializeField] private Transform _friendsScrollTransform;
        [SerializeField] private Transform _invitesScrollTransform;
        [SerializeField] private SocialWindowInviteTimer _timer;
        [SerializeField] private GameObject _lobbyGroup;
        [SerializeField] private GameObject _friendsGroup;
        [SerializeField] private GameObject _invitesGroup;
        [SerializeField] private GameObject _groupDisplayPrefab;
        [SerializeField] private GameObject _friendTemplatePrefab;
        [SerializeField] private GameObject _inviteTemplatePrefab;
        [SerializeField] private Button _lobbyButton;
        [SerializeField] private TextMeshProUGUI _lobbyButtonText;
        [SerializeField] private Button _friendsButton;
        [SerializeField] private Button _invitesButton;
        [SerializeField] private TextMeshProUGUI _inviteHeader;

        private List<SocialWindowPlayerPanel> _friendPanels = new List<SocialWindowPlayerPanel>();
        private List<SocialWindowInvitePanel> _invitePanels = new List<SocialWindowInvitePanel>();

        private CancellationTokenSource _disableCancellation = new();
        private int _createdFriendsTabs = 0;
        private int _createdInvitesTabs = 0;
        private void Awake()
        {
            _ = WaitForInit();
            async UniTask WaitForInit()
            {
                await UniTask.WaitUntil(() => LobbyDataManager.Instance != null);
                LobbyDataManager.Instance.OnLobbyMembersCalculated += OnLobbyMembersCalculated;
            }
        }


        private void Start() => _page = ((_page == -1) ? 0 : _page);

        private void OnEnable()
        {
            //Create new cancel token
            if (_disableCancellation != null)
                _disableCancellation.Dispose();
            _disableCancellation = new CancellationTokenSource();

            //Start refreshing the friends list
            _ = RefreshFriendsList(_disableCancellation.Token);

            FillContent();

            LobbyDataManager.Instance.OnInviteReceivedEvent += (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            LobbyDataManager.Instance.OnInviteRejectedEvent += (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            LobbyDataManager.Instance.OnInviteAcceptedEvent += (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            OnWindowEnabled?.Invoke();
        }
        private void OnDisable()
        {
            _disableCancellation.Cancel();

            LobbyDataManager.Instance.OnInviteReceivedEvent -= (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            LobbyDataManager.Instance.OnInviteRejectedEvent -= (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            LobbyDataManager.Instance.OnInviteAcceptedEvent -= (SteamInviteDefenition i) => FillInvitesList(LobbyDataManager.Instance.GetInvites);
            OnWindowDisabled?.Invoke();
        }
        public void OpenLobby()
        {
            _page = 0;
            SetInteractableContent();

        }
        public void OpenFriends()
        {
            _page = 1;
            SetInteractableContent();
            FillFriendsList(SteamDataManager.Instance.GetSteamFriends(1));
        }
        public void OpenInvites()
        {
            _page = 2;
            SetInteractableContent();
            FillInvitesList(LobbyDataManager.Instance.GetInvites);
            OnInvitesOpened?.Invoke(LobbyDataManager.Instance.GetInvites);
        }
        private void FillFriendsList(List<SteamProfileDefenition> friends)
        {
            SteamDataManager.Instance.UpdateSteamFriendInformation();
            _friendsScrollTransform.DestroyChildren();
            int index = 0;
            foreach (SteamProfileDefenition profile in friends)
            {
                if (profile.ID() == SteamDataManager.Instance.GetOwnerProfile.ID()) continue;
                SocialWindowPlayerPanel panel = null;
                if (index < _createdFriendsTabs)
                {
                    //TODO: More efficient
                    panel = _friendsScrollTransform.GetChild(index).GetComponent<SocialWindowPlayerPanel>();
                }
                else
                {
                    GameObject temp = Instantiate(_friendTemplatePrefab, _friendsScrollTransform);
                    panel = temp.GetComponent<SocialWindowPlayerPanel>();
                }
                panel.UpdatePanelInfo(profile.Name(), profile.Status(), profile.Icon());
                panel.SetPanelID(profile.ID());
                panel.SetInviteTimer(_timer);

                if (panel.HasInviteButton && profile.ID() == SteamDataManager.Instance.GetOwnerProfile.ID())
                {
                    panel.RemoveInviteButton();
                }
                index += 1;
            }

            SortFriendPanels();
        }
        private void FillInvitesList(List<SteamInviteDefenition> invites)
        {
            UpdateInviteHeader();
            //Update steam friend information
            SteamDataManager.Instance.UpdateSteamFriendInformation();
            //Don't think need to sort here
            _invitesScrollTransform.DestroyChildren();
            foreach (SteamInviteDefenition i in invites)
            {
                GameObject temp = Instantiate(_inviteTemplatePrefab, _invitesScrollTransform);
                SocialWindowInvitePanel panel = temp.GetComponent<SocialWindowInvitePanel>();
                //panel.socialWindow = this;

                //Apply settings from Steam
                panel.UpdatePanelInfo(i.Sender().Name(), i.Sender().Status(), i.Sender().Icon());
                panel.SetInvite(i);
                i.SetSeen();
            }
            SortInvitePanels();
        }
        /// <summary>
        /// Calls the OnInvitesRefreshedEvent event so that other scripts can access it.
        /// </summary>
        public void CallInvitesRefreshed() => OnInvitesRefreshed?.Invoke(LobbyDataManager.Instance.GetInvites);
        /// <summary>
        /// Updates to the panel that should be called by any refresh method
        /// </summary>
        public void UpdateInviteHeader()
        {
            int inviteNum = LobbyDataManager.Instance.GetInvites == null ? 0 : LobbyDataManager.Instance.GetInvites.Count;
            _inviteHeader.text = $"Invites ({inviteNum})";
        }
        /// <summary>
        /// Sort the friend panels
        /// </summary>
        private void SortFriendPanels() => SortPanels(_friendsScrollTransform.GetComponentsInChildren<SocialPanelItem>().AsValueEnumerable().ToList());
        /// <summary>
        /// Sort the invite panels
        /// </summary>
        private void SortInvitePanels() => SortPanels(_invitesScrollTransform.GetComponentsInChildren<SocialPanelItem>().AsValueEnumerable().ToList());
        /// <summary>
        /// Sort the panels in the window
        /// </summary>
        /// <param name="panels"></param>
        private void SortPanels(List<SocialPanelItem> panels)
        {
            List<SocialPanelItem> sortedPanels = panels;

            sortedPanels = sortedPanels
                .AsValueEnumerable()
                .OrderBy(p => p.name)
                .ThenBy(OrderByStatus)
                .ToList();

            for (int i = 0; i < sortedPanels.Count; i++)
                sortedPanels[i].transform.SetSiblingIndex(i);
        }
        /// <summary>
        /// Order to show online players first
        /// </summary>
        private int OrderByStatus(SocialPanelItem socialPanelItem)
        {
            switch (socialPanelItem.GetActivity())
            {
                case "Offline":
                    return 2;
                case "Away":
                    return 1;
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Refreshes the friends list iwth the most up to date information
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTaskVoid RefreshFriendsList(CancellationToken token)
        {
            while (gameObject.activeInHierarchy)
            {
                await UniTask.WaitForSeconds(30f, cancellationToken: token);

                UpdateInviteHeader();
                if (_friendsScrollTransform.childCount == SteamDataManager.Instance.GetSteamFriends(0).Count)
                {
                    SteamDataManager.Instance.UpdateSteamFriendInformation();

                    //Refresh without destroying transforms
                    List<SteamProfileDefenition> playerList = SteamDataManager.Instance.GetSteamFriends(0);
                    int index = 0;
                    foreach (Transform t in _friendsScrollTransform)
                    {
                        SocialWindowPlayerPanel panel = t.GetComponent<SocialWindowPlayerPanel>();
                        SteamProfileDefenition player = playerList[index];
                        //Apply settings from steam
                        panel.UpdatePanelInfo(player.Name(), player.Status(), player.Icon());

                        index += 1;
                    }
                }
                else
                {
                    //Refresh with destroying transforms as the friend size has changed
                    FillFriendsList(SteamDataManager.Instance.GetSteamFriends(0));
                }

                SortFriendPanels();
            }
        }
        /// <summary>
        /// Creates the lobby group list when data refreshed
        /// </summary>
        private void OnLobbyMembersCalculated(List<SteamProfileDefenition> members, CSteamID owner)
        {
            _groupTransform.transform.DestroyChildren();
            foreach(SteamProfileDefenition m in members)
            {
                GameObject temp = Instantiate(_groupDisplayPrefab, _groupTransform);
                SocialWindowGroupPlayerPanel panel = temp.GetComponent<SocialWindowGroupPlayerPanel>();
                panel.UpdatePanelInfo(m.Name(), m.Status(), m.Icon());
                panel.SetPermissions(owner == SteamDataManager.Instance.GetOwnerProfile.ID(), m.ID() == SteamDataManager.Instance.GetOwnerProfile.ID());
                panel.SetIfAllowedLeave(m.ID() == SteamDataManager.Instance.GetOwnerProfile.ID() && members.Count > 1);
                panel.SetOwnerIcon(owner == m.ID());
                panel.SetUserID(m.ID());
            }
            _lobbyButtonText.text = $"Group ({members.Count}/{LobbyDataManager.Instance.GetCurrentLobby.MaxMembers()})";
        }

        /// <summary>
        /// Sets what buttons are interactable/displayed depending on the given page
        /// </summary>
        private void SetInteractableContent()
        {
            switch (_page)
            {
                case 1:
                    _lobbyGroup.SetActive(false);
                    _friendsGroup.SetActive(true);
                    _invitesGroup.SetActive(false);
                    _lobbyButton.interactable = true;
                    _friendsButton.interactable = false;
                    _invitesButton.interactable = true;
                    break;
                case 2:
                    _lobbyGroup.SetActive(false);
                    _friendsGroup.SetActive(false);
                    _invitesGroup.SetActive(true);
                    _lobbyButton.interactable = true;
                    _friendsButton.interactable = true;
                    _invitesButton.interactable = false;
                    break;
                default:
                    _lobbyGroup.SetActive(true);
                    _friendsGroup.SetActive(false);
                    _invitesGroup.SetActive(false);
                    _lobbyButton.interactable = false;
                    _friendsButton.interactable = true;
                    _invitesButton.interactable = true;
                    break;
            }

        }
        /// <summary>
        /// Creates the content displayed in the window depending on the page
        /// </summary>
        private void FillContent()
        {
            switch (_page)
            {
                case 1:
                    FillFriendsList(SteamDataManager.Instance.GetSteamFriends(1));
                    break;
                case 2:
                    FillInvitesList(LobbyDataManager.Instance.GetInvites);
                    break;
                default:
                    break;

            }
        }
    }
}
