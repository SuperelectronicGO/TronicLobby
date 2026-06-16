using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//Script by Ben
namespace TronicSoft.Lobby.Runtime
{
    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDeselectHandler
    {
        public Action onPointerEnter;
        public Action onPointerExit;
        public Action onSelect;
        public Action onDeselect;

        private Selectable _selectable;
        private Button _button;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _button = _selectable as Button;

            if (_button)
                _button.onClick.AddListener(OnSelect);

            //CanvasSoundManager.Instance.AddButton(this);
        }
        private void OnDestroy()
        {
            if (_button)
                _button.onClick.RemoveListener(OnSelect);

            //CanvasSoundManager.Instance.RemoveButton(this);
        }

        public virtual void OnPointerEnter(PointerEventData data)
        {
            //CursorManager.Instance.SetButtonHover();
            onPointerEnter?.Invoke();
        }
        public virtual void OnPointerExit(PointerEventData data)
        {
            //CursorManager.Instance.SetDefault();
            onPointerExit?.Invoke();
        }

        public virtual void OnSelect()
        {
            onSelect?.Invoke();
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            onDeselect?.Invoke();
        }
    }
}
