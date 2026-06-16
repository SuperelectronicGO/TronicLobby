using UnityEngine;
using UnityEngine.UI;
namespace TronicSoft.Lobby.Runtime
{
    public class LoadingCircle : MonoBehaviour
    {
        [SerializeField] private RectTransform _wheelTransform;
        [SerializeField] private Image _wheelImage;
        [SerializeField] private float _imageRotationSpeed = -360;
        [SerializeField] private float _imageFillChangeSpeed = 1.75f;


        //Simple growing-and-shrinking spinning animation
        void Update()
        {
            _wheelTransform.Rotate(0, 0, _imageRotationSpeed * Time.deltaTime);
            _wheelImage.fillAmount += _imageFillChangeSpeed * Time.deltaTime;
            if ((_wheelImage.fillAmount <= 0 && _imageFillChangeSpeed < 0) || (_wheelImage.fillAmount >= 1 && _imageFillChangeSpeed > 0))
            {
                _imageFillChangeSpeed *= -1;
                if (_imageFillChangeSpeed > 0)
                {
                    _wheelTransform.localScale = Vector3.one;
                }
                else
                {
                    _wheelTransform.localScale = new Vector3(-1, 1, 1);
                }
            }

        }
    }
}
