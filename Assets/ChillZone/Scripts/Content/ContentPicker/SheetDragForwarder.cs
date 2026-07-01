using UnityEngine;
using UnityEngine.EventSystems;

namespace ChillZone.Content.ContentPicker
{
    /// <summary>
    /// The header's gesture handler: forwards a tap (close) and a vertical drag (expand / drag-down to dismiss)
    /// to the <see cref="ContentPickerView"/>. Handling tap + drag on ONE component — rather than a Button next
    /// to a drag handler — avoids the Selectable-vs-drag conflict that left input stuck after a drag.
    /// </summary>
    [DisallowMultipleComponent]
    public class SheetDragForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private ContentPickerView _view;

        public void Init(ContentPickerView view) => _view = view;

        public void OnBeginDrag(PointerEventData eventData) => _view?.OnBeginDrag(eventData);
        public void OnDrag(PointerEventData eventData) => _view?.OnDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => _view?.OnEndDrag(eventData);
        public void OnPointerClick(PointerEventData eventData) => _view?.OnHeaderClick();
    }
}
