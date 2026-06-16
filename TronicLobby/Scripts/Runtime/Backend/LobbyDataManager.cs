using UnityEngine;
using Steamworks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Text;
using Codice.Client.Common.GameUI;
namespace TronicSoft.Lobby.Runtime
{
    public class LobbyDataManager : TronicSingleton<LobbyDataManager>
    {
        public event Action<List<SteamProfileDefenition>, CSteamID> OnLobbyMembersCalculated;
        public event Action<SteamInviteDefenition> OnInviteReceivedEvent;
        public event Action<SteamInviteDefenition> OnInviteAcceptedEvent;
        public event Action<SteamInviteDefenition> OnInviteRejectedEvent;
        public LobbyDataDefenition GetCurrentLobby
        {
            get
            {
                return _currentLobby;
            }
            private set
            {
                _currentLobby = value;
            }
        }
        /// <summary>
        /// Get the list of current invites
        /// </summary>
        public List<SteamInviteDefenition> GetInvites
        {
            get
            {
                return _invites;
            }
            private set
            {
                _invites = value;
            }
        }
        private LobbyDataDefenition _currentLobby = null;
        protected Callback<LobbyCreated_t> LobbyCreatedCallback;
        protected Callback<LobbyEnter_t> LobbyEnteredCallback;
        protected Callback<LobbyDataUpdate_t> LobbyDataChangedCallback;
        protected Callback<LobbyInvite_t> LobbyInviteRecievedCallback;
        protected Callback<LobbyChatUpdate_t> LobbyStatusUpdateCallback;
        protected Callback<LobbyChatMsg_t> LobbyMsgUpdatedCallback;
        private List<SteamInviteDefenition> _invites = new List<SteamInviteDefenition>();

        #region Built-In and Init
        protected override void Awake()
        {
            base.Awake();
            _ = WaitForInit(); 
            async UniTask WaitForInit()
            {
                await UniTask.WaitUntil(() => SteamUser.BLoggedOn());
                Initialize();
            }
        }
        /// <summary>
        /// Creates a lobby when sign in is completed if the player doesn't already exist in one, and sets up later steam callbacks
        /// </summary>
        private void Initialize()
        {
            //Register callbacks
            LobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            LobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
            LobbyDataChangedCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataChanged);
            LobbyInviteRecievedCallback = Callback<LobbyInvite_t>.Create(OnInvitedToLobby);
            LobbyStatusUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyStatusChanged);
            LobbyMsgUpdatedCallback = Callback<LobbyChatMsg_t>.Create(OnLobbyMessageRecieved);
            if (_currentLobby != null) return;
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 3);
        }
        #endregion

        #region Public
        /// <summary>
        /// Invites a user to the lobby
        /// </summary>
        /// <param name="userToInvite"></param>
        public void InviteUserToLobby(CSteamID userToInvite)
        {
            SteamMatchmaking.InviteUserToLobby(GetCurrentLobby.ID(), userToInvite);
            Debug.Log("Inviting");
        }
        /// <summary>
        /// Accepts an invite from a user to join their lobby
        /// </summary>
        public void AcceptLobbyInvite(SteamInviteDefenition d)
        {
            _invites.Remove(d);
            OnInviteAcceptedEvent?.Invoke(d);
            TryJoinLobby(d.LobbyID());
        }
        /// <summary>
        /// Leaves the current lobby and creates a new one with only the user
        /// </summary>
        public void LeaveCurrentLobby()
        {
            SteamMatchmaking.LeaveLobby(_currentLobby.ID());
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 3);
        }
        public void PromoteUserToLobbyOwner(CSteamID userToPromote)
        {
            SteamMatchmaking.SetLobbyOwner(_currentLobby.ID(), userToPromote);
        }
        public void KickUserFromLobby(CSteamID _userToKick)
        {
            string msg = "$KICK" + _userToKick.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            SteamMatchmaking.SendLobbyChatMsg(_currentLobby.ID(), bytes, bytes.Length);
        }
        #endregion

        #region Private
        List<SteamProfileDefenition> tempLobbyMembers = new List<SteamProfileDefenition>();
        /// <summary>
        /// Filters the list of members in a lobby when created/joined to see if any additional profiles need to be cached
        /// </summary>
        private void CheckLobbyMembers()
        {
            int numMembers = SteamMatchmaking.GetNumLobbyMembers(_currentLobby.ID());
            tempLobbyMembers.Clear();
            _currentLobby.ClearMembers();
            //Iterate through each member
            for (int i = 0; i < numMembers; i++)
            {
                SteamProfileDefenition persona = null;
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobby.ID(), i);
                //First check for owner profile
                if(member == SteamDataManager.Instance.GetOwnerProfile.ID())
                {
                    tempLobbyMembers.Add(SteamDataManager.Instance.GetOwnerProfile);
                    persona = SteamDataManager.Instance.GetOwnerProfile;
                }
                //Then check if profile is already cached
                else if (SteamDataManager.Instance.HasProfileCached(member, out SteamProfileDefenition p))
                {
                    tempLobbyMembers.Add(p);
                    persona = p;
                }
                //Finally if it isn't we need to save the profile data before adding
                else
                {
                    persona = SteamDataManager.Instance.CacheProfile(member);
                    tempLobbyMembers.Add(persona);

                }
                if (!_currentLobby.AddMember(persona))
                {
                    Debug.LogWarning("Somehow trying to add a duplicate member here");
                }
            }
            _currentLobby.UpdateMaxMembers(SteamMatchmaking.GetLobbyMemberLimit(_currentLobby.ID()));
            CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobby.ID());
            //Trigger event
            OnLobbyMembersCalculated?.Invoke(tempLobbyMembers, lobbyOwner);
        }
        private void TryJoinLobby(CSteamID lobbyToJoin)
        {
            /*
            if (GetCurrentLobby != null)
            {
                SteamMatchmaking.LeaveLobby(_currentLobby.ID());
                await UniTask.WaitUntil(() => _currentLobby == null);
            }
            */
            SteamMatchmaking.JoinLobby(lobbyToJoin);
        }
        private void TryKickPlayer(CSteamID sender, CSteamID target)
        {
            //First check that the command is coming from a valid owner
            if (sender.m_SteamID != SteamMatchmaking.GetLobbyOwner(_currentLobby.ID()).m_SteamID) return;
            //Then check if this client is the target
            if(target.m_SteamID == SteamDataManager.Instance.GetOwnerProfile.ID().m_SteamID)
            {
                LeaveCurrentLobby();
            }
        }
        #endregion

        #region Callback
        /// <summary>
        /// Callback recieved when lobby is created
        /// </summary>
        private void OnLobbyCreated(LobbyCreated_t callback)
        {
            //If there was an issue, log an error to the console
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogError("Error creating Steam lobby: " + callback.m_eResult.ToString());
                return;
            }
            //Otherwise get the lobby ID
            CSteamID lobbyID = new CSteamID(callback.m_ulSteamIDLobby);
            //Create the data object
            _currentLobby = new LobbyDataDefenition(lobbyID);
        }
        /// <summary>
        /// Callback received when lobby is entered
        /// </summary>
        private void OnLobbyEntered(LobbyEnter_t callback)
        {
            var result = callback.m_EChatRoomEnterResponse;

            Debug.Log("Join result: " + result.ToString());
            //Create the new data object
            _currentLobby = new LobbyDataDefenition(new CSteamID(callback.m_ulSteamIDLobby));
        }
        /// <summary>
        /// Callback recieved when lobby data is changed
        /// </summary>
        private void OnLobbyDataChanged(LobbyDataUpdate_t callback)
        {
            Debug.Log("Lobby Entered");
            //Rebuild data for lobby members
            CheckLobbyMembers();
        }
        /// <summary>
        /// Callback recieved when getting a lobby invite
        /// </summary>
        private void OnInvitedToLobby(LobbyInvite_t callback)
        {
            //Get the lobby invitee
            SteamProfileDefenition invitee = SteamDataManager.Instance.ProfileFromID((CSteamID)callback.m_ulSteamIDUser);
            //If the inviting profile wasn't cached, need to save the data for the user.
            if (invitee == null)
            {
                invitee = SteamDataManager.Instance.CacheProfile((CSteamID)callback.m_ulSteamIDUser);
            }
            //Create a new invite
            SteamInviteDefenition invite = new SteamInviteDefenition(
                invitee,
                (CSteamID)callback.m_ulSteamIDLobby);
            //Remove any duplicates - done this way instead of not adding new invite if one from sender already exists so that most recent invite information is stored
            List<SteamInviteDefenition> duplicates = new List<SteamInviteDefenition>();
            //Removes any instances of invites that already exist from this friend from the list
            foreach (SteamInviteDefenition i in GetInvites)
            {
                if (i.Sender().ID() == invite.Sender().ID()) duplicates.Add(i);
            }
            foreach (SteamInviteDefenition i in duplicates)
            {
                _invites.Remove(i);
            }
            //Add it to invite list
            _invites.Add(invite);
            //Broadcast
            OnInviteReceivedEvent?.Invoke(invite);
        }
        protected void OnLobbyStatusChanged(LobbyChatUpdate_t callback)
        {
            //Rebuild data for lobby members
            CheckLobbyMembers();
        }
        protected void OnLobbyMessageRecieved(LobbyChatMsg_t callback)
        {
            byte[] data = new byte[4096];

            int len = SteamMatchmaking.GetLobbyChatEntry(
                (CSteamID)callback.m_ulSteamIDLobby,
                (int)callback.m_iChatID,
                out CSteamID sender,
                data,
                data.Length,
                out EChatEntryType type
            );
            string msg = Encoding.UTF8.GetString(data, 0, len);
            if(TryFilterChatMessage(msg)) return;

            Debug.Log($"Chat message from {sender}: {msg}");
            ///Check if any commands are within the message before some sort of display
            bool TryFilterChatMessage(string m)
            {
                //Check if command prefix attached
                if (m.Substring(0, 1) != "$") return false;
                if(m.Substring(1, 4) == "KICK")
                {
                    ulong target = ulong.Parse(m.Substring(5));
                    TryKickPlayer(sender, new CSteamID(target));
                    return true;
                }
                return false;
            }
        }

        #endregion
    }
}
