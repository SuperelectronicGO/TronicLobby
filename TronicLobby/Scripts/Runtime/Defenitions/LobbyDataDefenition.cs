using UnityEngine;
using Steamworks;
using System.Collections.Generic;
namespace TronicSoft.Lobby.Runtime
{
    [System.Serializable]
    public class LobbyDataDefenition
    {
        private CSteamID _id;
        private List<SteamProfileDefenition> _members;
        private int _maxMembers;
        /// <summary>
        /// Lobby ID
        /// </summary>
        public CSteamID ID()
        {
            return _id;
        }
        /// <summary>
        /// Get members in the lobby
        /// </summary>
        public List<SteamProfileDefenition> Members()
        {
            return _members;
        }
        /// <summary>
        /// Number of members in the lobby
        /// </summary>
        public int MemberNumber()
        {
            return _members == null ? 0 : _members.Count;
        }
        /// <summary>
        /// If a member exists in the lobby
        /// </summary>
        public bool HasMember(SteamProfileDefenition member)
        {
            return _members.Contains(member);
        }
        /// <summary>
        /// Adds a member to the lobby
        /// </summary>
        /// <returns>True if the member was added, or false if the member already existed in the lobby</returns>
        public bool AddMember(SteamProfileDefenition member)
        {
            if (HasMember(member)) return false;
           
            _members.Add(member);
            return true;
        }
        /// <summary>
        /// Removes a member from the lobby
        /// </summary>
        /// <returns>True if the member was removed, or false if the member wasn't in the lobby.</returns>
        public bool RemoveMember(SteamProfileDefenition member)
        {
            if (HasMember(member)) return false;

            _members.Remove(member);
            return true;
        }
        /// <summary>
        /// Clears the members in the lobby
        /// </summary>
        public void ClearMembers()
        {
            _members.Clear();
        }
        /// <summary>
        /// Gets how many members are allowed in the lobby
        /// </summary>
        public int MaxMembers()
        {
            return _maxMembers;
        }
        /// <summary>
        /// Updates the max amount of members allowed in the lobby
        /// </summary>
        public void UpdateMaxMembers(int newMax)
        {
            _maxMembers = newMax;
        }
        public LobbyDataDefenition(CSteamID id)
        {
            _id = id;
            _members = new List<SteamProfileDefenition>();
        }
        public LobbyDataDefenition(CSteamID id,  List<SteamProfileDefenition> members)
        {
            _id = id;
            _members = members;
        }
    }
}
