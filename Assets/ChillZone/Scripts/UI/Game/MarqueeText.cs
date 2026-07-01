using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// Seamlessly scrolling marquee. Attach this to the marquee CONTAINER. The script resolves its
    /// <see cref="TextMeshProUGUI"/> from a serialized reference, an existing child, or a created child,
    /// styles it (left-middle alignment, white @ 0.8 alpha, "Quote" text style, auto-sizing), fits its
    /// height to the container (minus <see cref="padding"/>) while stretching its width to the text
    /// content, then clones it for a continuous loop.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(Mask))]
    public class MarqueeText : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField, Tooltip("Text element to scroll. If empty, an existing child TextMeshProUGUI is used, or one is created under this container.")]
        private TextMeshProUGUI text;
        [SerializeField, Tooltip("Text colour override. Default is white @ 0.8 alpha.")]
        private Color textColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField, Tooltip("Padding offset inside the container. Top/bottom inset the fitted height; the text is vertically centred within what remains.")]
        private RectOffset padding;

        [Header("Content")]
        [SerializeField, Tooltip("Text to scroll. Can also call SetText() at runtime.")]
        private string content = "Welcome to the ChillZone! • High Score Challenge • Beat Your Best! • qwerty • github.com/danyilt • ";
        [SerializeField, Tooltip("Scroll speed in pixels per second.")]
        private float scrollSpeed = 30f;
        [SerializeField, Tooltip("Spacing between the end of one text and the start of the next.")]
        private float spacing = 100f;

        [Header("Auto-size")]
        [SerializeField, Tooltip("Smallest auto-sized font.")]
        private float fontSizeMin = 10f;
        [SerializeField, Tooltip("Largest auto-sized font (cap so the text never overshoots the container height).")]
        private float fontSizeMax = 72f;

        [Header("Startup")]
        [SerializeField, Tooltip("Delay before the marquee starts scrolling (in seconds).")]
        private float startDelay = 0.5f;

        private RectTransform _container;
        private RectTransform _rect;
        private RectTransform _cloneRect;
        private float _textWidth;
        private bool _scrolling;

        #region lifecycle

        private void Reset() => padding = new RectOffset(0, 0, 0, 0);

        private void Awake()
        {
            _container = (RectTransform)transform;
            ResolveText();
        }

        private void Start() => StartCoroutine(DelayedInit());

        private IEnumerator DelayedInit()
        {
            yield return new WaitForSeconds(startDelay);
            Init();
        }

        private void Update()
        {
            if (!_scrolling || !_cloneRect) return;
            var delta = scrollSpeed * Time.deltaTime;
            Scroll(_rect, delta);
            Scroll(_cloneRect, delta);
        }

        private void OnDestroy()
        {
            if (_cloneRect != null) Destroy(_cloneRect.gameObject);
        }

        private void OnValidate()
        {
            if (scrollSpeed < 0f) scrollSpeed = 0f;
            if (fontSizeMin < 0f) fontSizeMin = 0f;
            if (fontSizeMax < fontSizeMin) fontSizeMax = fontSizeMin;
        }

        #endregion

        #region public api

        public void SetText(string value)
        {
            content = value;
            if (text) text.text = value;
            if (_cloneRect != null) _cloneRect.GetComponent<TextMeshProUGUI>().text = value;
            if (_scrolling) Refresh();
        }

        public void SetSpeed(float speed) => scrollSpeed = speed;
        public void Pause()  => _scrolling = false;
        public void Resume() => _scrolling = true;
        public void Refresh() { if (_scrolling) Init(); }

        #endregion

        #region setup

        private void Init()
        {
            ResolveText();
            _rect = text.rectTransform;

            text.text = content;
            ConfigureText(text);
            FitRect(_rect);   // fit height to the container (minus padding), width to the content

            Canvas.ForceUpdateCanvases();
            _textWidth = text.preferredWidth;
            SetX(_rect, _container.rect.width);

            CreateClone();
            _scrolling = true;
        }

        private void CreateClone()
        {
            if (_cloneRect) Destroy(_cloneRect.gameObject);

            // Clone the EXACT text element so every TMP setting (font, size, colour, style, spacing,
            // material…) matches. Instantiate also copies THIS behaviour — strip it off the clone.
            var cloneTmp = Instantiate(text, _container);
            cloneTmp.name = "MarqueeTextClone";
            if (cloneTmp.TryGetComponent<MarqueeText>(out var stowaway)) Destroy(stowaway);
            cloneTmp.text = content;

            _cloneRect = cloneTmp.rectTransform;
            FitRect(_cloneRect);
            SetX(_cloneRect, _container.rect.width + _textWidth + spacing);
        }

        /// <summary>Left-middle alignment, white @ 0.8 alpha, "Quote" text style, single-line auto-sizing.</summary>
        private void ConfigureText(TextMeshProUGUI t)
        {
            t.alignment = TextAlignmentOptions.Left;   // horizontal Left + vertical Middle
            t.color = textColor;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true;
            t.fontSizeMin = fontSizeMin;
            t.fontSizeMax = fontSizeMax;

            var style = TMP_Settings.defaultStyleSheet ? TMP_Settings.defaultStyleSheet.GetStyle("Quote") : null;
            if (style != null) t.textStyle = style;
        }

        /// <summary>
        /// Stretch the text vertically to the container height (inset by <see cref="padding"/> top/bottom) while
        /// leaving the width free, then drive the width from the content via a ContentSizeFitter. The horizontal
        /// position is owned by the scroll (<see cref="SetX"/>), so the rect is anchored to the LEFT edge only.
        /// </summary>
        private void FitRect(RectTransform rect)
        {
            var pad = padding ?? new RectOffset();

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);

            // Vertical: height = containerHeight - (pad.top + pad.bottom), centred with the padding offset.
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, -(pad.top + pad.bottom));
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, (pad.bottom - pad.top) / 2f);

            // Width: driven by the text content (horizontal only — vertical stays stretched).
            var fitter = rect.GetComponent<ContentSizeFitter>() ?? rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        private static void SetX(RectTransform rect, float x)
        {
            var pos = rect.anchoredPosition;
            pos.x = x;
            rect.anchoredPosition = pos;
        }

        private void Scroll(RectTransform target, float delta)
        {
            var pos = target.anchoredPosition;
            pos.x -= delta;
            if (pos.x + _textWidth < 0f)
            {
                // Wrap: jump to just after the other text.
                var other = target == _rect ? _cloneRect : _rect;
                pos.x = other.anchoredPosition.x + _textWidth + spacing;
            }
            target.anchoredPosition = pos;
        }

        #endregion

        #region helpers (private)

        /// <summary>Resolve the TMP element: serialized → existing child → newly created child (under this container).</summary>
        private void ResolveText()
        {
            if (!text) text = GetComponentInChildren<TextMeshProUGUI>(true);
            if (text) return;

            var go = new GameObject("MarqueeText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_container, false);
            text = go.GetComponent<TextMeshProUGUI>();
        }

        #endregion
    }
}
