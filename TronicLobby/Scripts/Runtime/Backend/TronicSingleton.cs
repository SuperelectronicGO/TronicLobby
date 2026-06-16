using UnityEngine;
///This is a copy of LoogaSingleton. I made it a seperate file so that TronicLobby doesn't have a dependancy on LoogaTools.
namespace TronicSoft.Lobby.Runtime
{
    public abstract class TronicSingleton<T> : MonoBehaviour where T : Component
    {
        protected static T _instance;
        public static T Instance => _instance;

        protected virtual void Awake()
        {
            if (_instance == null)
                _instance = this as T;
            else
                Destroy(gameObject);
        }
    }
}
