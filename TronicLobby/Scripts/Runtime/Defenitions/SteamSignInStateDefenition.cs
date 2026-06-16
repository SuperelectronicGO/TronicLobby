using UnityEngine;

namespace TronicSoft.Lobby.Runtime
{
    public class SteamSignInStateDefenition
    {
        public enum SignInState
        {
            AttemptLogin = 0,
            Success = 1,
            FailedAuthentication = 2,
            FailedOther = 3,
            DisplayNotification = 4,
            SteamManagerNotInitialized = 5,
            RequestFailed = 6,
        }
        public SignInState State;
        public string Message = "";
        public SteamSignInStateDefenition(SignInState state)
        {
            State = state;
        }
        public SteamSignInStateDefenition(SignInState state, string message) : this(state)
        {
            Message = message;
        }
    }
}
