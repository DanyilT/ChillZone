using ChillZone.Utils.Native;
using ChillZone.UI.Utils;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window.Utils;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Settings.Credits
{
    /// <summary>
    /// Bespoke, code-generated full-screen credits overlay (no prefab / no WindowConfig).
    /// Slides up from the bottom on Show and back down on tap or hardware back. Built with
    /// RenderUtils and animated with DOTween, mirroring UI/Game/PauseOverlay. Registers a
    /// back-nav handler while open so the hardware back button closes it first.
    /// </summary>
    public class CreditsOverlay : MonoBehaviour
    {
        private RectTransform _panel;
        private float _slideDistance;
        private bool _isOpen;
        private const float SlideDuration = 0.4f;

        #region factory

        public static CreditsOverlay Create(CreditsConfig config)
        {
            var root = new GameObject(nameof(CreditsOverlay), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var overlay = root.AddComponent<CreditsOverlay>();
            overlay.Build(config);
            return overlay;
        }

        #endregion

        #region Show / Hide

        public void Show()
        {
            if (_isOpen) return;
            _isOpen = true;
            gameObject.SetActive(true);

            BackNavigationController.Instance?.Push(CloseFromBack);

            _panel.DOKill();
            _panel.anchoredPosition = new Vector2(0f, -_slideDistance);
            _panel.DOAnchorPos(Vector2.zero, SlideDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        public void Hide() => Close(fromBack: false);

        // Invoked by BackNavigationController (which already popped the handler).
        private void CloseFromBack() => Close(fromBack: true);

        private void Close(bool fromBack)
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (!fromBack) BackNavigationController.Instance?.Pop();

            _panel.DOKill();
            _panel.DOAnchorPos(new Vector2(0f, -_slideDistance), SlideDuration)
                  .SetEase(Ease.InCubic)
                  .SetUpdate(true)
                  .OnComplete(() => gameObject.SetActive(false));
        }

        #endregion

        #region build

        private void Build(CreditsConfig config)
        {
            var title = config && !string.IsNullOrEmpty(config.title) ? config.title : "Credits";
            var hint = config && !string.IsNullOrEmpty(config.bottomHint) ? config.bottomHint : "Tap to close";

            CanvasUtils.Apply(GetComponent<Canvas>(), CanvasUtils.CanvasPreset.Overlay(sortingOrder: 1000));

            _slideDistance = GetComponent<RectTransform>().rect.height;
            if (_slideDistance <= 0f) _slideDistance = 1920f;

            // Full-screen sliding panel; tapping it closes the overlay.
            var backdropColor = new Color(0.06f, 0.06f, 0.09f, 0.98f);
            var panelGo = RenderUtils.CreateImage(transform, backdropColor, raycastTarget: true, preserveAspect: false, name: "Panel");
            _panel = panelGo.GetComponent<RectTransform>();
            RenderUtils.SetupRectTransformFullScreen(_panel);
            RenderUtils.SetupButton(panelGo.AddComponent<Button>(), panelGo.GetComponent<Image>(), Selectable.Transition.None, Hide);

            // Title near the top.
            var titleGo = RenderUtils.CreateText(_panel, title, TextConfig.TitleTextDefault(), "Title");
            Anchor(titleGo.GetComponent<RectTransform>(), new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.9f));

            // Body — one row per credit: definition (left) + author (right), or the definition centered when
            // there is no author (e.g. "Made with Unity").
            BuildLines(_panel, config ? config.lines : null);

            // Bottom hint (from the config).
            var hintText = RenderUtils.CreateText(_panel, hint, TextConfig.BodyTextDefault(), "Hint").GetComponent<TextMeshProUGUI>();
            hintText.textStyle = TMP_Settings.defaultStyleSheet ? TMP_Settings.defaultStyleSheet.GetStyle("Quote") : null;
            hintText.alignment = TextAlignmentOptions.Center;
            Anchor(hintText.GetComponent<RectTransform>(), new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.1f));

            gameObject.SetActive(false);
        }

        // Vertical list of credit lines in the body region. Each line is definition LEFT + author RIGHT; with
        // no author the definition is centered across the full width.
        private static void BuildLines(RectTransform parent, CreditEntry[] lines)
        {
            var container = RenderUtils.CreateChild(parent, "Lines", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Anchor((RectTransform)container.transform, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.8f));
            var vlg = container.GetComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = vlg.childControlHeight = vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 40f;

            if (lines == null) return;

            // Fixed font size (auto-size off) so every line matches regardless of column widths.
            var lineConfig = TextConfig.BodyTextDefault();
            lineConfig.enableAutoSizing = false;
            lineConfig.fontSize = 30;

            foreach (var line in lines)
                AddLine(container.transform, line, lineConfig);
        }

        private static void AddLine(Transform parent, CreditEntry line, TextConfig lineConfig)
        {
            // No author → a single full-width centered line.
            if (string.IsNullOrEmpty(line.author))
            {
                RenderUtils.CreateText(parent, line.definition, lineConfig, "Line").GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
                return;
            }

            // Definition LEFT (flexes to fill the row), author RIGHT.
            var row = RenderUtils.CreateChild(parent, "Line", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            hlg.spacing = 16f;

            var def = RenderUtils.CreateText(row.transform, line.definition, lineConfig, "Definition").GetComponent<TextMeshProUGUI>();
            def.alignment = TextAlignmentOptions.Left;
            def.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f; // fills the left → pushes the author to the right

            RenderUtils.CreateText(row.transform, line.author, lineConfig, "Author").GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
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
    }
}
