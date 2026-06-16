using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TronicSoft.Lobby.Runtime
{
    public class SocialPanelItem : MonoBehaviour
    {
        public string Name => _name;
        public string Activity => _activity;



        [SerializeField] protected Image _backgroundImage;
        [SerializeField] protected RawImage _profileIcon;
        [SerializeField] protected TextMeshProUGUI _nameText;
        [SerializeField] protected TextMeshProUGUI _activityText;

        [SerializeField] protected Color _onlineColor = Color.grey;
        [SerializeField] protected Color _otherColor = Color.gray3;

        protected string _name;
        protected string _activity;
        public string GetActivity() => _activity;
        public virtual void UpdatePanelInfo(string name, string activity, Texture2D icon)
        {
            _name = name;
            _activity = activity;
            _profileIcon.texture = icon;
            _nameText.text = name;
            _activityText.text = activity;
            _backgroundImage.color = (activity == "Offline" || activity == "Away") ? _otherColor : _onlineColor;
        }
    }
}
