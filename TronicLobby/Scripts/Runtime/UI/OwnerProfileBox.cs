using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TronicSoft.Lobby.Runtime
{
    public class OwnerProfileBox : MonoBehaviour
    {
        [SerializeField] private RawImage _profilePicture;
        [SerializeField] private TextMeshProUGUI _profileNameText;

        private void Start()
        {
            SteamDataManager.Instance.OnSignInCompletedEvent += SignInGraphicEvents;
        }
        private void OnDisable()
        {
            SteamDataManager.Instance.OnSignInCompletedEvent -= SignInGraphicEvents;
        }

        private void SignInGraphicEvents()
        {
            SteamProfileDefenition profile = SteamDataManager.Instance.GetOwnerProfile;
            _profileNameText.text = profile.Name();
            _profilePicture.texture = profile.Icon();
            //_profilePicture.texture = SteamClientManager.CreateImageTextureFromHandle(SteamFriends.GetSmallFriendAvatar(SteamUser.GetSteamID()));

            _ = RefreshLayout();
        }
        private async UniTaskVoid RefreshLayout()
        {
            RectTransform profileNameRectTf = _profileNameText.transform as RectTransform;
            RectTransform parentRectTf = transform as RectTransform;

            LayoutRebuilder.ForceRebuildLayoutImmediate(profileNameRectTf);

            await UniTask.WaitForEndOfFrame();

            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRectTf);
        }
    }
}
