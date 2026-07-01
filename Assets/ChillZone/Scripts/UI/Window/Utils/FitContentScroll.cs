using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Window.Utils
{
    /// <summary>
    /// Sizes a ScrollRect so its CONTAINER fits the content up to a maximum height, then scrolls (the
    /// container never exceeds the cap). uGUI's ContentSizeFitter can't cap a height, so this drives the
    /// scroll's <see cref="LayoutElement.preferredHeight"/> = min(content, cap − the container's non-scroll
    /// overhead). Vertical scrolling turns on only when the content overflows.
    ///
    /// IMPORTANT: the container must be one whose VerticalLayoutGroup has childControlHeight = TRUE (it sizes
    /// the scroll from its PREFERRED height). Then (container − scroll) is a stable, LAG-FREE overhead. A
    /// container that sizes children by their rect (childControlHeight = false) lags the height this drives and
    /// makes the layout oscillate ("uncontrollably changing height").
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(LayoutElement))]
    public class FitContentScroll : MonoBehaviour
    {
        private ScrollRect _scroll;
        private RectTransform _content;
        private RectTransform _container;
        private LayoutElement _layoutElement;
        private float _maxHeight;

        public void Init(ScrollRect scroll, RectTransform content, RectTransform container, LayoutElement layoutElement, float maxHeight)
        {
            _scroll = scroll;
            _content = content;
            _container = container;
            _layoutElement = layoutElement;
            _maxHeight = Mathf.Max(0f, maxHeight);
            Apply();
        }

        private void OnEnable() => Apply();
        private void LateUpdate() => Apply();

        private void Apply()
        {
            if (!_content || !_container || !_layoutElement) return;

            var scrollRect = (RectTransform)transform;
            var contentHeight = LayoutUtility.GetPreferredHeight(_content);
            var overhead = Mathf.Max(0f, _container.rect.height - scrollRect.rect.height); // everything except the scroll
            var available = Mathf.Max(0f, _maxHeight - overhead);
            var target = Mathf.Min(contentHeight, available);

            // Push a new height ONLY when it changed (and flip vertical scrolling only on a real change) so a
            // settled layout isn't re-dirtied. With a childControlHeight=true container the overhead is lag-free,
            // so 'target' is stable and this converges instead of oscillating.
            if (Mathf.Abs(_layoutElement.preferredHeight - target) > 1f)
                _layoutElement.preferredHeight = target;

            var wantVertical = contentHeight > target + 1f;
            if (_scroll && _scroll.vertical != wantVertical)
                _scroll.vertical = wantVertical;
        }
    }
}
