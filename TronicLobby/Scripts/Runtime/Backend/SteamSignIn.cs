using Cysharp.Threading.Tasks;
using Steamworks;
using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using Notification = Unity.Services.Authentication.Notification;

namespace TronicSoft.Lobby.Runtime
{
    public class SteamSignIn : TronicSingleton<SteamSignIn>
    {
        #region Delegates
        /// <summary>
        /// Event response any time anything changes with the current sign in state
        /// </summary>
        public event Action<SteamSignInStateDefenition> OnSignInStateUpdated;
        
        /// <summary>
        /// How long since being initialized the script should wait before attempting to sign in
        /// </summary>
        [SerializeField] private float _startDelay = 4.5f;
        #endregion

        #region Fields
        /// <summary>
        /// Web ticket request to auth service
        /// </summary>
        private Callback<GetTicketForWebApiResponse_t> m_AuthTicketForWebApiResponseCallback;
        /// <summary>
        /// Auth ticket
        /// </summary>
        private HAuthTicket m_AuthTicket;
        /// <summary>
        /// Session ticket
        /// </summary>
        private string m_SessionTicket;
        /// <summary>
        /// Notifications retrived by service
        /// </summary>
        private List<Notification> notifications;
        /// <summary>
        /// Start delay method
        /// </summary>
        private UniTaskVoid? _startDelayVoid = null;
        /// <summary>
        /// If the start delay should be skipped
        /// </summary>
        private bool _skipStartup = false;
        /// <summary>
        /// If the script should log to the console
        /// </summary>
        [SerializeField] private bool _logging;
        #endregion

        #region Built-In Methods
        /// <summary>
        /// Logs in to Unity Authentication and retrieves any pending notifications
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("TypeSafety", "UNT0006:Incorrect message signature", Justification = "UniTask signiture")]
        private async UniTaskVoid Start()
        {
            //Log in to unity services
            await Init();
            //Retrieve if any notifications need to be displayed
            await RetrieveNotifications();
            //And display if notifications exist
            ReadNotifications();
            //Dispatch wait method to delay signin
            _startDelayVoid = SignInAfterDelay();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) return;
#endif
            SteamUser.CancelAuthTicket(m_AuthTicket);
        }
        #endregion

        #region Public
        /// <summary>
        /// List of any notifications retrieved from authentication service during sign in
        /// </summary>
        public List<Notification> GetNotifications() => notifications;
        /// <summary>
        /// Skip the startup delay and immediately try to sign in
        /// </summary>
        public void SkipStartupSequence()
        {
            // Don't do anything if the start delay isn't active
            if (!_startDelayVoid.HasValue) return;
            // If it isn't active, skip the delay and immediately request a sign in
            _skipStartup = true;
            _startDelayVoid = null;
            SignInWithSteam();
        }
        #endregion

        #region Private
        /// <summary>
        /// Inits Unity Authentication
        /// </summary>
        private async UniTask Init()
        {
            var options = new InitializationOptions();
            options.SetEnvironmentName("production");
            await UnityServices.InitializeAsync(options);
            if (_logging) Debug.Log(UnityServices.State + " in environment production");
        }
        /// <summary>
        /// Retrieves any messages that need to be read
        /// </summary>
        private async UniTask RetrieveNotifications()
        {
            try
            {
                var lastNotificationDate = AuthenticationService.Instance.LastNotificationDate;
                long storedNotificationDate = GetLastNotificationReadDate(); // Retrieve the last notification read createdAt date from storage using GetLastNotificationReadDate();
                                                                             // Verify if the LastNotification date is available and greater than the last read notifications
                if (lastNotificationDate != null && long.Parse(lastNotificationDate) > storedNotificationDate)
                {
                    // Retrieve the notifications from the backend
                    notifications = await AuthenticationService.Instance.GetNotificationsAsync();
                }
            }
            catch (AuthenticationException e)
            {
                // Read notifications from the banned player exception
                notifications = e.Notifications;
                // Send message to notify
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.FailedAuthentication, e.Message));
            }
            catch (Exception e)
            {
                // Notify the player with the proper error message
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.FailedOther, e.Message));
            }
        }
        /// <summary>
        /// Calls SteamSignInState with notification if a message needs to be reads
        /// </summary>
        private void ReadNotifications()
        {
            if (notifications != null)
            {
                foreach (var notification in notifications)
                {
                    OnNotificationRead(notification);
                    OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.DisplayNotification));
                }
            }
        }
        /// <summary>
        /// Marks a notification as read after it is displayed
        /// </summary>
        private void OnNotificationRead(Notification notification)
        {
            long storedNotificationDate = GetLastNotificationReadDate(); // Retrieve the last notification read createdAt date from storage GetLastNotificationReadDate();
            var notificationDate = long.Parse(notification.CreatedAt);
            if (notificationDate > storedNotificationDate)
            {
                SaveNotificationReadDate(notificationDate);
            }
        }
        /// <summary>
        /// Stores the notification read date to PlayerPrefs
        /// </summary>
        /// <param name="notificationReadDate"></param>
        private void SaveNotificationReadDate(long notificationReadDate)
        {
            PlayerPrefs.SetString("notifdate", notificationReadDate.ToString());
        }
        /// <summary>
        /// Retrieves the last date where notifications were read
        /// </summary>
        /// <returns></returns>
        private long GetLastNotificationReadDate()
        {
            if (long.TryParse(PlayerPrefs.GetString("notifdate"), out long result))
            {
                return result;
            }
            else
            {
                PlayerPrefs.SetString("notifdate", "0");
                return 0;
            }
        }
        /// <summary>
        /// Waits for the start delay, then starts the sign in process
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid SignInAfterDelay()
        {
            await UniTask.Delay((int)(_startDelay * 1000));
            if (_skipStartup) return;
            SignInWithSteam();
            _startDelayVoid = null;
        }
        /// <summary>
        /// Tries to sign the user in to the authentication service with steam
        /// </summary>
        private void SignInWithSteam()
        {
            if (_logging) Debug.Log("Starting Sign-In");
            //Throw error if steam manager isn't initialized
            if (!SteamManager.Initialized)
            {
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.SteamManagerNotInitialized, "Steam Manager is not initialized"));
                return;
            }
            m_AuthTicketForWebApiResponseCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnAuthCallback);
            m_AuthTicket = SteamUser.GetAuthTicketForWebApi("unityauthenticationservice");
            OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.AttemptLogin, "Logging in..."));
        }
        /// <summary>
        /// Disposes of the web ticket after recieving callback and invokes async sign in
        /// </summary>
        private void OnAuthCallback(GetTicketForWebApiResponse_t callback)
        {
            if (_logging) Debug.Log("Callback");
            m_SessionTicket = BitConverter.ToString(callback.m_rgubTicket).Replace("-", string.Empty);
            m_AuthTicketForWebApiResponseCallback.Dispose();
            m_AuthTicketForWebApiResponseCallback = null;
            //Debug.Log("Steam Login success. Session Ticket: " + m_SessionTicket);

            _ = LogInToSteam(m_SessionTicket, "unityauthenticationservice");
        }
        /// <summary>
        /// Invokes async sign in with params
        /// </summary>
        private async UniTaskVoid LogInToSteam(string ticket, string identity)
        {
            await SignInWithSteamAsync(ticket, identity);
        }
        /// <summary>
        /// Tries to log in with steam to the authentication service
        /// </summary>
        private async UniTask SignInWithSteamAsync(string ticket, string identity)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithSteamAsync(ticket, identity);
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.Success, "Logged In"));
                if (_logging) Debug.Log("SignIn is successful.");
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.FailedAuthentication, ex.Message));
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.RequestFailed, ex.Message));
                Debug.LogException(ex);
            }
            catch (Exception ex)
            {
                OnSignInStateUpdated?.Invoke(new SteamSignInStateDefenition(SteamSignInStateDefenition.SignInState.FailedOther, ex.Message));
            }
        }
        #endregion
    }
}
