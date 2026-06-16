using Cysharp.Threading.Tasks;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TronicSoft.Lobby.Runtime
{
    public class SteamDataManager : TronicSingleton<SteamDataManager>
    {
        #region Delegates
        public event Action OnSignInCompletedEvent;
        public event Action<bool, string> OnLobbyEnteredEvent;
        public event Action OnLobbyLeftEvent;
        #endregion

        #region Steam Callbacks
        protected Callback<GameLobbyJoinRequested_t> LobbyJoinRequestedCallback;
        protected Callback<AvatarImageLoaded_t> AvatarImageLoadedCallback;
        #endregion

        #region Public Fields
        /// <summary>
        /// Get the owner profile
        /// </summary>
        public SteamProfileDefenition GetOwnerProfile
        {
            get
            {
                return _ownerProfile;
            }
            private set
            {
                _ownerProfile = value;
            }
        }
        /// <summary>
        /// Get all cached profiles
        /// </summary>
        public List<SteamProfileDefenition> GetCachedProfiles
        {
            get
            {
                return _cachedProfiles;
            }
            private set
            {
                _cachedProfiles = value;
            }
        }
        /// <summary>
        /// Gets a SteamProfileDefenition from its CSteamID
        /// </summary>
        /// <returns>The Steam profile if it exists, or null if not</returns>
        /// <param name="id">CSteamID of the friend</param>
        /// <exception cref="System.NullReferenceException">No profile is cached with that ID</exception>
        public SteamProfileDefenition ProfileFromID(CSteamID id)
        {
            foreach (SteamProfileDefenition f in _cachedProfiles)
            {
                if (f.ID() == id) return f;
            }
            return null;
        }
        #endregion

        #region Private Fields
        private SteamProfileDefenition _ownerProfile;
        private List<SteamProfileDefenition> _cachedProfiles;
        private List<SteamProfileDefenition> _profilesWaitingForicon = new List<SteamProfileDefenition>();
        #endregion

        #region Built-In Methods and Init
        protected override void Awake()
        {
            base.Awake();


            LobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
            AvatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnReceivedAvatarImageLoaded);
            //Then load initial
            _ = Init();
        }

        private async UniTaskVoid Init()
        {
            await UniTask.WaitUntil(() => SteamUser.BLoggedOn());
            CreateOwnerProfile();
            _cachedProfiles = CreateFriendsList();
            OnSignInCompletedEvent?.Invoke();
        }
        #endregion

        #region Public
        public void OpenSteamProfileForUser(CSteamID id)
        {
            if (!SteamUtils.IsOverlayEnabled() || !id.IsValid()) return;
            SteamFriends.ActivateGameOverlayToUser("steamid", id);
        }
        /// <summary>
        /// This method should be called anytime before accessing the _cachedProfiles list in order to show the most up-to-date information about the profile
        /// </summary>
        public void UpdateSteamFriendInformation()
        {
            for (int i = 0; i < _cachedProfiles.Count; i++)
            {
                if (!_cachedProfiles[i].IsFriend()) continue;
                CSteamID id = _cachedProfiles[i].ID();
                EPersonaState p_state = SteamFriends.GetFriendPersonaState(id);
                bool isInGame = SteamFriends.GetFriendGamePlayed(id, out FriendGameInfo_t friendGameInfo);
                string status = "";
                bool onKubera = false;
                switch (p_state)
                {
                    default:
                    case EPersonaState.k_EPersonaStateOffline: //0
                        status = "Offline";
                        break;
                    case EPersonaState.k_EPersonaStateOnline: //1
                    case EPersonaState.k_EPersonaStateLookingToTrade: //5
                    case EPersonaState.k_EPersonaStateLookingToPlay: //6
                        if (isInGame)
                        {
                            status = "Online";
                            if (friendGameInfo.m_gameID.m_GameID == 2397310)
                            {
                                CSteamID friendLobbyID = friendGameInfo.m_steamIDLobby;
                                string lobbyData = SteamMatchmaking.GetLobbyData(friendLobbyID, "scene");
                                if (lobbyData == "")
                                {
                                    status = "Playing Kubera - Alone";
                                }
                                else
                                {
                                    status = "Playing Kubera - " + lobbyData;
                                }
                                onKubera = true;
                            }
                        }
                        else
                        {
                            status = "Online";
                        }
                        break;
                    case EPersonaState.k_EPersonaStateBusy: //2
                        status = "Do not Disturb";
                        break;
                    case EPersonaState.k_EPersonaStateAway: //3
                        if (isInGame && friendGameInfo.m_gameID.m_GameID == 2397310)
                        {
                            status = "Online, Playing Kubera";
                            onKubera = true;
                        }
                        else
                        {
                            status = "Away";
                        }
                        break;
                    case EPersonaState.k_EPersonaStateSnooze: //4
                        status = "Away";
                        break;

                }
                _cachedProfiles[i].SetJoinable(onKubera);
                _cachedProfiles[i].SetStatus(status);
            }
        }
        /// <summary>
        /// Get a list of all cached profiles that are friends with the user
        /// </summary>
        /// <param name="method">How to sort the returned profiles: 0 = no sort, anything else = order by joinable</param>
        public List<SteamProfileDefenition> GetSteamFriends(int method)
        {
            List<SteamProfileDefenition> data = new List<SteamProfileDefenition>();
            for (int i = 0; i < _cachedProfiles.Count; i++)
            {
                if (_cachedProfiles[i].IsFriend()) data.Add(_cachedProfiles[i]);
            }
            if (method == 0) return data;

            data = data.OrderBy(x => x.IsFriend()).ToList();
            return data;
        }
        /// <summary>
        /// Gets if the user already has a profile cached
        /// </summary>
        public bool HasProfileCached(CSteamID id, out SteamProfileDefenition profile)
        {
            for (int i = 0; i < _cachedProfiles.Count; i++)
            {
                if (_cachedProfiles[i].ID() == id)
                {
                    profile = _cachedProfiles[i];
                    return true;
                }
            }
            profile = null;
            return false;
        }
        /// <summary>
        /// Saves the information for a user
        /// </summary>
        public SteamProfileDefenition CacheProfile(CSteamID id)
        {
            SteamProfileDefenition profile = CacheInfoForUser(id);
            _cachedProfiles.Add(profile);
            return profile;
        }
        #endregion

        #region Private
        /// <summary>
        /// Gets aa list of SteamProfileDefenition created from the logged-in user's steam friendslist
        /// </summary>
        /// <exception cref="System.Exception">The player isn't logged in</exception>
        private List<SteamProfileDefenition> CreateFriendsList()
        {
            int friendNum = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            List<SteamProfileDefenition> friends = new List<SteamProfileDefenition>();
            if (friendNum == -1)
            {
                throw new System.Exception("You are not logged in!");
            }
            for (int i = 0; i < friendNum; i++)
            {
                CSteamID friend_id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                SteamProfileDefenition friend = new SteamProfileDefenition(
                    friend_id,
                    SteamFriends.GetFriendPersonaName(friend_id),
                    null,
                    SteamFriends.GetFriendPersonaState(friend_id).ToString(),
                    false,
                    true);

                int imageHandle = SteamFriends.GetLargeFriendAvatar(friend_id);
                GetSteamTextureFromImageHandle(imageHandle, friend);
                friends.Add(friend);
            }
            return friends;
        }
        /// <summary>
        /// Creates the profile for the user logged into the steam account
        /// </summary>
        private void CreateOwnerProfile()
        {
            _ownerProfile = new SteamProfileDefenition(
                SteamUser.GetSteamID(),
                SteamFriends.GetPersonaName(),
                null,
                "Online, Playing Kubera",
                false,
                false);
            int imageHandle = SteamFriends.GetLargeFriendAvatar(_ownerProfile.ID());
            GetSteamTextureFromImageHandle(imageHandle, _ownerProfile);
        }
        /// <summary>
        /// Caches the information for a steam user and returns the profile
        /// </summary>
        /// <param name="id">CSteamID of the user</param>
        private SteamProfileDefenition CacheInfoForUser(CSteamID id)
        {
            SteamProfileDefenition persona = new SteamProfileDefenition(
                id,
                SteamFriends.GetFriendPersonaName(id),
                null,
                "Online, Playing Kubera",
                false,
                SteamFriends.HasFriend(id, EFriendFlags.k_EFriendFlagImmediate));
            int imageHandle = SteamFriends.GetLargeFriendAvatar(id);
            GetSteamTextureFromImageHandle(imageHandle, persona);
            return persona;
        }
        /// <summary>
        /// Coroutine that gets the sprite of a profile once the image handle is valid
        /// </summary>
        /// <param name="handle">Image handle of the sprite</param>
        /// <param name="profile">Profile reference</param>
        /// <returns></returns>
        private void GetSteamTextureFromImageHandle(int handle, SteamProfileDefenition profile)
        {
            //If the handle is valid, add the profile to the waiting queue for the handle
            if (handle == -1)
            {
                _profilesWaitingForicon.Add(profile);
            }
            //Otherwise set the icon immediately
            profile.SetIcon(CreateImageTextureFromHandle(handle));
        }
        /// <summary>
        /// Gets the Texture2D of a steamavatar
        /// </summary>
        /// <param name="handle">Image handle for the sprite</param>
        public static Texture2D CreateImageTextureFromHandle(int handle)
        {
            if (handle <= 0)
            {
                return null;
            }

            // Steam API returns a boolean indicating success, which is safer to check
            if (!SteamUtils.GetImageSize(handle, out uint imageWidth, out uint imageHeight) || imageWidth == 0 || imageHeight == 0)
            {
                return null;
            }

            int totalBytes = (int)(imageWidth * imageHeight * 4);
            byte[] avatarStream = new byte[totalBytes];

            if (!SteamUtils.GetImageRGBA(handle, avatarStream, totalBytes))
            {
                Debug.LogWarning($"Failed to get image data for handle {handle}");
                return null;
            }

            // OPTIMIZATION: Flip the image vertically in-place using Buffer.BlockCopy
            // Steam provides images Top-Down, Unity expects Bottom-Up.
            int rowSizeBytes = (int)imageWidth * 4;
            byte[] tempRow = new byte[rowSizeBytes];
            int halfHeight = (int)imageHeight / 2;

            for (int y = 0; y < halfHeight; y++)
            {
                int topRowOffset = y * rowSizeBytes;
                int bottomRowOffset = ((int)imageHeight - 1 - y) * rowSizeBytes;

                // Swap top row and bottom row
                Buffer.BlockCopy(avatarStream, topRowOffset, tempRow, 0, rowSizeBytes);
                Buffer.BlockCopy(avatarStream, bottomRowOffset, avatarStream, topRowOffset, rowSizeBytes);
                Buffer.BlockCopy(tempRow, 0, avatarStream, bottomRowOffset, rowSizeBytes);
            }

            Texture2D temp = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false);
            temp.LoadRawTextureData(avatarStream);
            temp.Apply();


            return temp;

        }
        #endregion

        #region Callbacks

        /// <summary>
        /// Callback recieved when lobby join requested
        /// </summary>
        private void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            Debug.Log("Request to join lobby recieved");
        }
        /// <summary>
        /// Callback when an avatar handle is loaded
        /// </summary>
        private void OnReceivedAvatarImageLoaded(AvatarImageLoaded_t callback)
        {
            CSteamID id = callback.m_steamID;
            int handle = callback.m_iImage;

            foreach (SteamProfileDefenition profile in _profilesWaitingForicon)
            {
                if (profile.ID() == id)
                {
                    profile.SetIcon(CreateImageTextureFromHandle(handle));
                    _profilesWaitingForicon.Remove(profile);
                    return;
                }
            }
            Debug.LogError("Found no profile stored in array that matches requested callback.");
        }
        #endregion
    }
}
