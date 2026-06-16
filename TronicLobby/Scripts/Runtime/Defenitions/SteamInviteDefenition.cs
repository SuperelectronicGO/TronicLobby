using Steamworks;
using UnityEngine;

namespace TronicSoft.Lobby.Runtime
{
    [System.Serializable]
    public class SteamInviteDefenition
    {
        private SteamProfileDefenition _sender;
        private CSteamID _id;
        private bool _seen;
        /// <summary>
        /// Profile that sent the invite
        /// </summary>
        public SteamProfileDefenition Sender()
        {
            return _sender;
        }
        /// <summary>
        /// Lobby ID
        /// </summary>
        public CSteamID LobbyID()
        {
            return _id;
        }
        public bool Seen()
        {
            return _seen;
        }
        public void SetSeen()
        {
            _seen = true;
        }
        public SteamInviteDefenition(SteamProfileDefenition sender, CSteamID lobbyID)
        {
            _sender = sender;
            _id = lobbyID;
        }
    }
}
