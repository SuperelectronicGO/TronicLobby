using Steamworks;
using UnityEngine;

namespace TronicSoft.Lobby.Runtime
{
    [System.Serializable]
    public class SteamProfileDefenition
    {
        private CSteamID _id;
        private string _name;
        private Texture2D _icon;
        private string _status;
        private bool _isFriend;
        private bool _joinable;

        /// <summary>
        /// Internal ID
        /// </summary>
        public CSteamID ID()
        {
            return _id;
        }
        /// <summary>
        /// Profile Name
        /// </summary>
        public string Name()
        {
            return _name;
        }
        /// <summary>
        /// 64x64 Profile Icon
        /// </summary>
        public Texture2D Icon()
        {
            return _icon;
        }
        /// <summary>
        /// Status of the player: Online, offline, ect
        /// </summary>
        public string Status()
        {
            return _status;
        }
        /// <summary>
        /// If the profile is friends with the owner
        /// </summary>
        public bool IsFriend()
        {
            return _isFriend;
        }
        /// <summary>
        /// If the player is currently able to be joined
        /// </summary>
        public bool Joinable()
        {
            return _joinable;
        }
        /// <summary>
        /// Set the icon of a profile
        /// </summary>
        /// <param name="icon"></param>
        public void SetIcon(Texture2D icon)
        {
            _icon = icon;
        }
        /// <summary>
        /// Sets the status of the player
        /// </summary>
        public void SetStatus(string status)
        {
            _status = status;
        }
        /// <summary>
        /// Sets if the profile is joinable or not
        /// </summary>
        public void SetJoinable(bool joinable)
        {
            _joinable = joinable;
        }
        public SteamProfileDefenition(CSteamID id)
        {
            _id = id;
        }
        public SteamProfileDefenition(CSteamID id, string name) : this(id)
        {
            _name = name;
        }
        public SteamProfileDefenition(CSteamID id, string name, Texture2D icon, string status, bool joinable, bool friend) : this(id, name)
        {
            _icon = icon;
            _status = status;
            _joinable = joinable;
            _isFriend = friend;
        }
    }
}
