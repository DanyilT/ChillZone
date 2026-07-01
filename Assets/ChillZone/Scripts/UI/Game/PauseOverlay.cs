using System;
using ChillZone.UI.Utils;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window;
using ChillZone.UI.Window.Config;
using ChillZone.UI.Window.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// Fully code-generated pause overlay — no prefab and no WindowConfig. Builds a
    /// screen-space-overlay canvas (renders even while the AR session is paused) showing a
    /// frozen snapshot of the camera frame captured at pause, a translucent dim over it, a
    /// centred title, and a bottom hint line that cycles on UNSCALED time (so it keeps
    /// animating while Time.timeScale is 0). Show / hide fade via <see cref="WindowAnimator"/>
    /// (unscaled), the same fade path as the windows. Tapping anywhere invokes the resume callback.
    /// </summary>
    public class PauseOverlay : MonoBehaviour
    {
        private TextMeshProUGUI _hintLabel;
        private RawImage _snapshotImage;
        private RectTransform _panel;
        private CanvasGroup _panelCg;
        private CanvasGroup _backdropCg;
        private WindowAnimationConfig _animConfig;
        private string[] _hints;
        private float _interval;
        private float _timer;
        private int _index;
        private bool _hiding;

        #region factory

        /// <summary>Builds the overlay on a fresh root GameObject (hidden until <see cref="Show"/>).</summary>
        public static PauseOverlay Create(string title, string[] hints, float hintInterval, Action onResume)
        {
            var root = new GameObject(nameof(PauseOverlay), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var overlay = root.AddComponent<PauseOverlay>();
            overlay.Build(title, hints, hintInterval, onResume);
            root.SetActive(false);
            return overlay;
        }

        #endregion

        #region show / hide

        /// <summary>
        /// Shows the overlay over <paramref name="snapshot"/> (the frozen camera frame). Pass
        /// <paramref name="flipVertically"/> = true when the capture came out upside down (top-left-origin APIs).
        /// </summary>
        public void Show(Texture snapshot, bool flipVertically)
        {
            _hiding = false;
            SetSnapshot(snapshot, flipVertically);
            _index = 0;
            _timer = 0f;
            RefreshHint();
            gameObject.SetActive(true);

            // Fade the backdrop (snapshot + dim) and the panel (title + hint) in on unscaled time.
            WindowAnimator.PlayShow(_panel, _panelCg, 1f, _backdropCg, 1f, _animConfig);
        }

        public void Hide()
        {
            _hiding = true;
            // Fade out, then deactivate — but only if a re-show didn't happen during the fade.
            WindowAnimator.PlayHide(_panel, _panelCg, _backdropCg, _animConfig, () =>
            {
                if (_hiding) gameObject.SetActive(false);
            });
        }

        private void SetSnapshot(Texture snapshot, bool flipVertically)
        {
            if (!_snapshotImage) return;
            _snapshotImage.texture = snapshot;
            _snapshotImage.enabled = snapshot;
            _snapshotImage.uvRect = flipVertically ? new Rect(0f, 1f, 1f, -1f) : new Rect(0f, 0f, 1f, 1f);
        }

        #endregion

        #region build

        private void Build(string title, string[] hints, float hintInterval, Action onResume)
        {
            _hints = hints;
            _interval = Mathf.Max(0.5f, hintInterval);

            CanvasUtils.Apply(GetComponent<Canvas>(), CanvasUtils.CanvasPreset.Overlay(sortingOrder: 1000));

            _animConfig = CreateFadeConfig();

            // Backdrop layer (faded as one by WindowAnimator): frozen snapshot behind a translucent dim.
            var backdropGo = RenderUtils.CreateChild(transform, "Backdrop", typeof(RectTransform), typeof(CanvasGroup));
            var backdropRect = (RectTransform)backdropGo.transform;
            RenderUtils.SetupRectTransformFullScreen(backdropRect);
            _backdropCg = backdropGo.GetComponent<CanvasGroup>();

            // Frozen camera snapshot (filled in by Show); sits behind the dim so the dim darkens it.
            var snapGo = RenderUtils.CreateChild(backdropRect, "Snapshot", typeof(RectTransform), typeof(RawImage));
            _snapshotImage = snapGo.GetComponent<RawImage>();
            _snapshotImage.raycastTarget = false;
            _snapshotImage.enabled = false;
            RenderUtils.SetupRectTransformFullScreen(_snapshotImage.rectTransform);

            // Translucent dim over the snapshot; tapping it resumes.
            var dimColor = new Color(0.1f, 0.1f, 0.12f, 0.45f);
            var dim = RenderUtils.CreateImage(backdropRect, dimColor, raycastTarget: true, preserveAspect: false, name: "Dim");
            RenderUtils.SetupRectTransformFullScreen(dim.GetComponent<RectTransform>());
            RenderUtils.SetupButton(dim.AddComponent<Button>(), dim.GetComponent<Image>(), Selectable.Transition.None, onResume);

            // Panel holds the title + hint, faded independently; lets taps fall through to the dim.
            var panelGo = RenderUtils.CreateChild(transform, "Panel", typeof(RectTransform), typeof(CanvasGroup));
            _panel = (RectTransform)panelGo.transform;
            RenderUtils.SetupRectTransformFullScreen(_panel);
            _panelCg = panelGo.GetComponent<CanvasGroup>();
            _panelCg.blocksRaycasts = false;

            // Centred title.
            var titleGo = RenderUtils.CreateText(_panel, title, TextConfig.TitleTextDefault(), "Title");
            Anchor(titleGo.GetComponent<RectTransform>(), new Vector2(0.1f, 0.6f), new Vector2(0.9f, 0.8f));

            // Bottom hint line (cycled in Update).
            _hintLabel = RenderUtils.CreateText(_panel, string.Empty, TextConfig.BodyTextDefault(), "Hint").GetComponent<TextMeshProUGUI>();
            _hintLabel.textStyle = TMP_Settings.defaultStyleSheet ? TMP_Settings.defaultStyleSheet.GetStyle("Quote") : null;
            _hintLabel.alignment = TextAlignmentOptions.Center;
            Anchor(_hintLabel.GetComponent<RectTransform>(), new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.15f));
        }

        // A runtime Fade config so the overlay animates with WindowAnimator without needing a serialized asset.
        private static WindowAnimationConfig CreateFadeConfig()
        {
            var config = ScriptableObject.CreateInstance<WindowAnimationConfig>();
            config.showAnimation = WindowAnimationConfig.AnimationType.Fade;
            config.hideAnimation = WindowAnimationConfig.AnimationType.Fade;
            config.showDuration  = 0.25f;
            config.hideDuration  = 0.2f;
            return config;
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region hint cycling

        private void Update()
        {
            if (_hints is not { Length: > 1 }) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer < _interval) return;

            _timer = 0f;
            _index = (_index + 1) % _hints.Length;
            RefreshHint();
        }

        private void RefreshHint()
        {
            if (_hintLabel && _hints is { Length: > 0 })
                _hintLabel.text = _hints[Mathf.Clamp(_index, 0, _hints.Length - 1)];
        }

        #endregion

        #region cleanup

        private void OnDestroy()
        {
            if (_animConfig) Destroy(_animConfig);
        }

        #endregion
    }
}
