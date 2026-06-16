using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using Codice.Client.Common.GameUI;

namespace TronicSoft.Lobby.Runtime
{
    public class SteamSignInGraphics : MonoBehaviour
    {
        #region Fields
        [Header("Settings")]
        [Tooltip("How long the graphic should remain active after a successful connection")]
        [SerializeField] private float _connectedStayTime = 1.0f;

        //Decided to go with individual components instead of one 'header' and 'description' text component because it's easier to make changes in the editor vs in script.
        //It's not because I'm bad at coding >:(
        [Header("Components")]
        [SerializeField] private GameObject _parent;
        [SerializeField] private GameObject _connectingText;
        [SerializeField] private GameObject _connectedText;
        [SerializeField] private GameObject _failedText;
        [SerializeField] private TextMeshProUGUI _failedDescriptionText;
        [SerializeField] private GameObject _loadingCircle;
        [SerializeField] private GameObject _check;
        #endregion

        #region Built-In Methods
        private void Start()
        {
            if(SteamSignIn.Instance == null)
            {
                Debug.LogWarning("Steam sign in is null; graphical updates will not play.");
                return;
            }
            SteamSignIn.Instance.OnSignInStateUpdated += OnUpdateConnectionState;
        }
        private void OnDisable()
        {
            SteamSignIn.Instance.OnSignInStateUpdated -= OnUpdateConnectionState;
        }
        #endregion

        #region Private
        /// <summary>
        /// Update UI in response to sign in callbacks
        /// </summary>
        private void OnUpdateConnectionState(SteamSignInStateDefenition state)
        {
            switch (state.State)
            {
                case SteamSignInStateDefenition.SignInState.AttemptLogin:
                    SetConnecting();
                    return;
                case SteamSignInStateDefenition.SignInState.Success:
                    SetConnected();
                    return;
                case SteamSignInStateDefenition.SignInState.FailedAuthentication:
                case SteamSignInStateDefenition.SignInState.FailedOther:
                case SteamSignInStateDefenition.SignInState.SteamManagerNotInitialized:
                case SteamSignInStateDefenition.SignInState.RequestFailed:
                    SetFailed(state.Message);
                    return;
                case SteamSignInStateDefenition.SignInState.DisplayNotification:
                    Debug.Log(state.Message);
                    return;
            }
        }
        /// <summary>
        /// Set the notification state to connecting
        /// </summary>
        private void SetConnecting()
        {
            _parent.SetActive(true);
            _connectingText.SetActive(true);
            _connectedText.SetActive(false);
            _failedText.SetActive(false);
            _failedDescriptionText.gameObject.SetActive(false);
            _loadingCircle.SetActive(true);
        }
        /// <summary>
        /// Set the notification state to connected and close
        /// </summary>
        private void SetConnected()
        {
            _connectingText.SetActive(false);
            _connectedText.SetActive(true);
            _failedText.SetActive(false);
            _failedDescriptionText.gameObject.SetActive(false);
            _loadingCircle.SetActive(false);
            _check.SetActive(true);
            _ = WaitForCompletedTime();
        }
        /// <summary>
        /// Set the notification state to failed
        /// </summary>
        /// <param name="msg">Fail reason</param>
        private void SetFailed(string msg)
        {
            _connectingText.SetActive(false);
            _connectedText.SetActive(false);
            _failedText.SetActive(true);
            _failedDescriptionText.gameObject.SetActive(true);
            _failedDescriptionText.text = msg;
            _loadingCircle.SetActive(false);
        }
        /// <summary>
        /// Wait for set time, then disable graphics
        /// </summary>
        private async UniTaskVoid WaitForCompletedTime()
        {
            await UniTask.WaitForSeconds(_connectedStayTime);
            _parent.SetActive(false);
        }
        #endregion
    }
}
