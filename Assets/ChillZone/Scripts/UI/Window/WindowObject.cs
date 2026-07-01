using System;
using System.Collections.Generic;
using System.Linq;
using ChillZone.Core;
using ChillZone.Utils.Native;
using ChillZone.UI.Utils;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window.Utils;
using ChillZone.UI.Window.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Window
{
    /// <summary>
    /// A runtime-built modal window. Created exclusively through WindowManager
    /// (or WindowPreview for edit-mode previewing).
    /// </summary>
    public class WindowObject : MonoBehaviour
    {
        private WindowGlobalConfig _globalConfig;
        private CanvasUtils.CanvasPreset _canvasPreset;   // resolved from _globalConfig.canvasPreset (a Preset enum)
        private bool _registeredBackNav;
        private bool _isClosing;
        private Action _onClosed; // notifies WindowManager on close
        private bool _requestedHideOtherUI;

        // Static so overlapping windows REF-COUNT the hide: the scene canvases stay hidden until the LAST
        // hide-requesting window releases. Without this, a closing window's deferred restore would
        // re-enable UI that a newer window (opened in the meantime) still wants hidden.
        private static int _hideOtherUiRequests;
        private static readonly List<Canvas> HiddenSceneCanvases = new();

        /// <summary>True while any open window is hiding other UI (hideOtherUI). Self-managing canvases (e.g. the
        /// ButtonManager) check this so a re-render can't re-enable them over the hide.</summary>
        public static bool IsHidingOtherUI => _hideOtherUiRequests > 0;

        // Animation state
        private RectTransform _panelRect;
        private CanvasGroup _panelCanvasGroup;
        private float _panelTargetAlpha;
        private CanvasGroup _backdropCanvasGroup;
        private float _backdropTargetAlpha;
        private WindowAnimationConfig _animConfig;

        #region factory

        /// <summary>
        /// Creates and initializes a new instance of a window object within the specified parent transform.
        /// </summary>
        /// <param name="config">The configuration settings for the window, including its unique ID.</param>
        /// <param name="options">Optional actions on backdrop, panel, or header click.</param>
        /// <param name="parent">The transform that will serve as the parent container for the new window object.</param>
        /// <returns>The newly created and configured <see cref="WindowObject"/> instance.</returns>
        internal static WindowObject Create(WindowConfig config, WindowShowOptions options, Transform parent)
        {
            const string prefix = "window_";
            var window = RenderUtils.CreateChild(parent, prefix + config.windowId, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WindowObject)).GetComponent<WindowObject>();

            options ??= new WindowShowOptions();
            window._animConfig = config.animationConfig;
            window._onClosed = () => WindowManager.Instance?.NotifyWindowClosed(config.windowId);
            window.Build(config, options);

            if (!options.SkipBackNavigation && BackNavigationController.Instance)
            {
                BackNavigationController.Instance.Push(window.Close);
                window._registeredBackNav = true;
            }

            window.PlayShowAnimation();
            return window;
        }

        #endregion

        #region public api

        /// <summary>
        /// Closes the window object by handling back navigation, invoking the closed event, and initiating the destruction animation.
        /// </summary>
        public void Close() => Close(true);

        /// <summary>
        /// Closes the window. <paramref name="playSound"/> is false for programmatic teardown (CloseAll) and
        /// re-show — so only user-triggered closes (backdrop / panel / header tap, back button) play the close SFX.
        /// </summary>
        public void Close(bool playSound)
        {
            if (_isClosing) return;
            _isClosing = true;

            if (playSound) AudioService.PlayUi(UiSound.Close);

            if (_registeredBackNav && BackNavigationController.Instance)
            {
                BackNavigationController.Instance.Pop();
                _registeredBackNav = false;
            }

            _onClosed?.Invoke();
            // Restore the other UI NOW (not after the hide animation) so the incoming state's visibility updates
            // (e.g. re-showing the buttons) aren't blocked by IsHidingOtherUI; the window just fades out over it.
            ReleaseHideOtherUI();
            PlayHideAnimation(() => Destroy(gameObject));
        }

        // Safety net: release the hide request if the window is torn down without a normal Close.
        private void OnDestroy() => ReleaseHideOtherUI();

        #endregion

        #region builder

        private void Build(WindowConfig config, WindowShowOptions options)
        {
            _globalConfig = config.globalConfig ?? WindowGlobalConfig.Instance;
            _canvasPreset = CanvasUtils.Get(_globalConfig.canvasPreset);
            RenderUtils.SetupCanvas(GetComponent<Canvas>(), GetComponent<CanvasScaler>(), _canvasPreset.renderMode, _canvasPreset.renderCamera, _canvasPreset.planeDistance, _canvasPreset.referenceResolution, _canvasPreset.matchWidthOrHeight, config.CanvasSortingOrder);
            CreateBackdrop(config.backdropConfig, options);
            CreatePanel(config.panelConfig, config.headerConfig, options);

            switch (config)
            {
                case ManualWindowConfig manual:
                    AddManualContent(manual);
                    break;
                case BestScoreWindowConfig best:
                    AddBestScoreContent(best, options);
                    break;
            }

            if (config.hideOtherUI) RequestHideOtherUI();
        }

        // Single large dynamic value (the best score). Word-wrap off; overflow / character spacing /
        // material preset and the min height come from the config; the value text from options.PrimaryText.
        private void AddBestScoreContent(BestScoreWindowConfig config, WindowShowOptions options)
        {
            var value = options?.PrimaryText ?? string.Empty;
            var display = string.IsNullOrEmpty(config.bestScoreConfig.scoreFormat) ? value : string.Format(config.bestScoreConfig.scoreFormat, value);

            var go  = RenderUtils.CreateText(_panelRect, display, config.bestScoreConfig.scoreTextConfig, "ScoreValue");
            var tmp = go.GetComponent<TextMeshProUGUI>();

            // Use TMP's default font asset; setting the font resets the material, so apply the chosen
            // preset (e.g. Drop Shadow) afterwards.
            if (TMP_Settings.defaultFontAsset) tmp.font = TMP_Settings.defaultFontAsset;
            if (config.bestScoreConfig.scoreTextConfig.fontMaterial) tmp.fontSharedMaterial = config.bestScoreConfig.scoreTextConfig.fontMaterial;

            tmp.enableWordWrapping = false;
            tmp.overflowMode = config.bestScoreConfig.scoreOverflow;
            tmp.characterSpacing = config.bestScoreConfig.scoreCharacterSpacing;

            var pad = config.bestScoreConfig.scorePadding ?? new RectOffset();
            tmp.margin = new Vector4(pad.left, pad.top, pad.right, pad.bottom);

            // The panel's VerticalLayoutGroup has childControlHeight = false, so the row height comes from
            // the rect — give it the configured min height (the value centres within it via alignment).
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, Mathf.Max(0f, config.bestScoreConfig.scoreMinHeight));
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = config.bestScoreConfig.scoreMinHeight;
        }

        // Manual layout: a self-sizing wrapper holding an elastic, scrollbar-less scroll (text
        // column on the left, icon on the right) with a summary pinned below it. FitContentScroll
        // caps the panel at maxScreenHeightFraction of the screen, scrolling past that.
        private void AddManualContent(ManualWindowConfig config)
        {
            var root = RenderUtils.CreateChild(_panelRect, "Manual", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            RenderUtils.SetupContentSizeFitter(root.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
            var rootVlg = root.GetComponent<VerticalLayoutGroup>();
            rootVlg.padding = _globalConfig.bodyPadding;
            rootVlg.spacing = config.manualConfig.summarySpacing > 0f ? config.manualConfig.summarySpacing : 60f;
            rootVlg.childControlWidth = rootVlg.childControlHeight = rootVlg.childForceExpandWidth = true;
            rootVlg.childForceExpandHeight = false;

            // Elastic, clipped, scrollbar-less scroll area. The transparent Image is a RAYCAST TARGET so the
            // ScrollRect actually receives drags — nothing else in the scroll (text/icons) is raycastable, so
            // without it drags fall through to the panel behind and nothing scrolls.
            var scrollGo = RenderUtils.CreateChild(root.transform, "ManualScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D), typeof(LayoutElement));
            RenderUtils.SetupImage(scrollGo.GetComponent<Image>(), Color.clear, raycastTarget: true);
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = false; // FitContentScroll turns this on only when the content overflows
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 25f;
            scroll.viewport = (RectTransform)scrollGo.transform;

            var content = RenderUtils.CreateChild(scrollGo.transform, "Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            // Horizontally stretched: clear any inherited width delta so the content matches the viewport width
            // EXACTLY. A leftover sizeDelta.x made it overflow ±50 and the RectMask2D clipped the content edges.
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y);
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);
            var contentVlg = content.GetComponent<VerticalLayoutGroup>();
            contentVlg.childControlWidth = contentVlg.childControlHeight = contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = config.manualConfig.rowSpacing > 0f ? config.manualConfig.rowSpacing : 65f;
            RenderUtils.SetupContentSizeFitter(content.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
            scroll.content = contentRect;

            // One [text | icon] row per manual entry, stacked vertically.
            if (config.manualConfig.rows != null)
                foreach (var entry in config.manualConfig.rows)
                    AddManualRow(content.transform, entry, config);

            // Summary pinned below the scroll (last child of the wrapper), styled with the "Quote" TMP text style.
            var summaryTmp = RenderUtils.CreateText(root.transform, config.manualConfig.summary, config.manualConfig.summaryConfig, "Summary").GetComponent<TextMeshProUGUI>();
            var quoteStyle = TMP_Settings.defaultStyleSheet ? TMP_Settings.defaultStyleSheet.GetStyle("Quote") : null;
            if (quoteStyle != null) summaryTmp.textStyle = quoteStyle;

            // Cap against the Manual ROOT (its VLG has childControlHeight=true → lag-free overhead), NOT the
            // window panel (childControlHeight=false → lagged → the height oscillates).
            scrollGo.AddComponent<FitContentScroll>().Init(scroll, contentRect, (RectTransform)root.transform, scrollGo.GetComponent<LayoutElement>(), config.manualConfig.maxScreenHeightFraction * _canvasPreset.referenceResolution.y);
        }

        // One manual row: text column (title + description, flexible) with the icon hugging the left.
        private static void AddManualRow(Transform parent, ManualConfig.Row entry, ManualWindowConfig config)
        {
            var row = RenderUtils.CreateChild(parent, "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rowHlg = row.GetComponent<HorizontalLayoutGroup>();
            rowHlg.childControlWidth = rowHlg.childControlHeight = true;
            rowHlg.childForceExpandWidth = rowHlg.childForceExpandHeight = false;
            rowHlg.childAlignment = TextAnchor.UpperLeft;
            rowHlg.spacing = 30f;

            var textCol = RenderUtils.CreateChild(row.transform, "Text", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            textCol.GetComponent<LayoutElement>().flexibleWidth = 1f; // fills the row, pushing the icon to the right
            var textVlg = textCol.GetComponent<VerticalLayoutGroup>();
            textVlg.childControlWidth = textVlg.childControlHeight = textVlg.childForceExpandWidth = true;
            textVlg.childForceExpandHeight = false;
            textVlg.spacing = 12f;
            RenderUtils.CreateText(textCol.transform, entry.title, config.manualConfig.titleConfig, "Title");
            RenderUtils.CreateText(textCol.transform, entry.description, config.manualConfig.descriptionConfig, "Description");

            var iconRow = entry.iconConfig;
            if (iconRow.icons == null || iconRow.icons.Length == 0) return;
            row.GetComponent<HorizontalLayoutGroup>().reverseArrangement = true;

            // Icon column: an HLG/VLG (per iconsLayout) of one or more images, each sized by the config. The row
            // HLG controls width with childForceExpandWidth=false, so the column hugs its icon content.
            var vertical = iconRow.iconsLayout == RectTransform.Axis.Vertical;
            var iconsGo = RenderUtils.CreateChild(row.transform, "Icons", typeof(RectTransform), vertical ? typeof(VerticalLayoutGroup) : typeof(HorizontalLayoutGroup));
            var iconsLayout = iconsGo.GetComponent<HorizontalOrVerticalLayoutGroup>();
            iconsLayout.childControlWidth = iconsLayout.childControlHeight = true;
            iconsLayout.childForceExpandWidth = iconsLayout.childForceExpandHeight = false;
            iconsLayout.childAlignment = TextAnchor.UpperCenter;
            iconsLayout.spacing = iconRow.iconSpacing;

            foreach (var entryIcon in iconRow.icons)
            {
                if (!entryIcon.sprite) continue;
                var le = RenderUtils.CreateImage(iconsGo.transform, iconRow.iconColor, entryIcon.sprite, Image.Type.Simple, false, true, "IconImage").AddComponent<LayoutElement>();
                // Lock min == preferred (width AND height) so a wide title/description can't shrink the icon.
                var iconSize = entryIcon.sizeOverride > 0 ? entryIcon.sizeOverride : iconRow.iconSize;
                le.minWidth = le.minHeight = le.preferredWidth = le.preferredHeight = iconSize;

                if (entryIcon.spacingAfter < 1) continue;
                var spacer = RenderUtils.CreateChild(iconsGo.transform, "Spacer", typeof(RectTransform), typeof(LayoutElement)).GetComponent<LayoutElement>();
                if (vertical) spacer.preferredHeight = entryIcon.spacingAfter; else spacer.preferredWidth = entryIcon.spacingAfter;
            }
        }

        private void CreateBackdrop(BackdropConfig config, WindowShowOptions options)
        {
            const string goName = "Backdrop";
            var backdrop = RenderUtils.CreateChild(transform, goName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            
            var action = RenderUtils.ResolveClickAction(options.OnBackdropClick, config.closeOnBackdropClick, Close);
            RenderUtils.SetupRectTransformFullScreen(backdrop.GetComponent<RectTransform>());
            RenderUtils.SetupCanvasGroup(backdrop.GetComponent<CanvasGroup>(), action != null, action != null || config.blockRaycasts);
            RenderUtils.SetupImage(backdrop.GetComponent<Image>(), config.color, raycastTarget:action != null || config.blockRaycasts);
            RenderUtils.SetupButton(backdrop.GetComponent<Button>(), backdrop.GetComponent<Image>(), Selectable.Transition.None, action);

            // for animations
            _backdropCanvasGroup = backdrop.GetComponent<CanvasGroup>();
            _backdropTargetAlpha = 1f;
        }

        private void CreatePanel(PanelConfig config, HeaderConfig headerConfig, WindowShowOptions options)
        {
            const string goName = "Panel";
            var panel = RenderUtils.CreateChild(transform, goName, typeof(RectTransform), typeof(CanvasGroup), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup), typeof(Image), typeof(Button));

            var width = config.preferredWidth > 0 ? config.preferredWidth : Mathf.Max(config.minWidth, _canvasPreset.referenceResolution.x * config.screenWidthFraction);
            var height = config.preferredHeight > 0 ? config.preferredHeight : 0f;

            RenderUtils.SetupRectTransformCentered(panel.GetComponent<RectTransform>(), new Vector2(width, height));
            RenderUtils.SetupCanvasGroup(panel.GetComponent<CanvasGroup>(), true, config.blockRaycasts, config.backgroundAlpha);
            RenderUtils.SetupVerticalLayoutGroup(panel.GetComponent<VerticalLayoutGroup>(), _globalConfig.panelPadding, childControlHeight:false);
            RenderUtils.SetupContentSizeFitter(panel.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, config.preferredHeight > 0 ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize);
            UIRenderUtils.ApplyBackground(panel.GetComponent<Image>(), config.panelAppearanceConfig, config.blockRaycasts);
            RenderUtils.SetupButton(panel.GetComponent<Button>(), panel.GetComponent<Image>(), Selectable.Transition.ColorTint, RenderUtils.ResolveClickAction(options.OnPanelClick, config.closeOnPanelClick, Close));

            // for animations
            _panelRect        = panel.GetComponent<RectTransform>();
            _panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            _panelTargetAlpha = config.backgroundAlpha;

            if (!string.IsNullOrEmpty(headerConfig.titleText))
                AddHeader(headerConfig, RenderUtils.ResolveClickAction(options.OnHeaderClick, headerConfig.closeOnHeaderClick, Close), panel.transform, _globalConfig.headerHeight, _globalConfig.headerPadding);
            if (config.iconConfig.icons is { Length: > 0 })
                AddIconsRow(config, panel.transform, _globalConfig.iconRowPadding);
            if (!string.IsNullOrEmpty(config.bodyText))
                AddBody(config, panel.transform, _globalConfig.bodyPadding);

            if (config is { minHeight: > 0, preferredHeight: <= 0 })
                RenderUtils.RebuildLayoutIfNeeded(_panelRect, config.minHeight);
        }

        #endregion

        #region section builders (static)

        private static void AddHeader(HeaderConfig config, Action action, Transform parent, float headerHeight = 100f, RectOffset padding = null)
        {
            const string goName = "Header";
            const string textName = "TitleText";

            var go = RenderUtils.CreateChild(parent, goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, headerHeight);
            UIRenderUtils.ApplyBackground(go.GetComponent<Image>(), config.headerAppearanceConfig, config.blockRaycasts);
            RenderUtils.SetupButton(go.GetComponent<Button>(), go.GetComponent<Image>(), Selectable.Transition.ColorTint, action);

            var title = RenderUtils.CreateText(go.transform, config.titleText, config.titleTextConfig, textName).GetComponent<RectTransform>();
            RenderUtils.SetupRectTransformFullScreen(title, padding);
        }

        private static void AddIconsRow(PanelConfig config, Transform parent, RectOffset padding = null)
        {
            const string goName = "IconsRow";
            const string imageName = "Icon";
            const string spacerName = "Spacer";

            var go = RenderUtils.CreateChild(parent, goName, typeof(RectTransform), typeof(ContentSizeFitter));
            RenderUtils.SetupHorizontalOrVerticalLayoutGroup(config.iconConfig.iconsLayout == RectTransform.Axis.Vertical ? go.AddComponent<VerticalLayoutGroup>() : go.AddComponent<HorizontalLayoutGroup>(), padding, spacing: config.iconConfig.iconSpacing);
            RenderUtils.SetupContentSizeFitter(go.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);

            foreach (var icon in config.iconConfig.icons)
            {
                if (!icon.sprite) continue;
                RenderUtils.CreateImage(go.transform, Color.white, icon.sprite, Image.Type.Simple, false, true, imageName).GetComponent<RectTransform>().sizeDelta = Vector2.one * (icon.sizeOverride > 0 ? icon.sizeOverride : config.iconConfig.iconSize);

                if (icon.spacingAfter < 1) continue;
                RenderUtils.CreateChild(go.transform, spacerName, typeof(RectTransform)).GetComponent<RectTransform>().sizeDelta = config.iconConfig.iconsLayout == RectTransform.Axis.Vertical ? new Vector2(0f, icon.spacingAfter) : new Vector2(icon.spacingAfter, 0f);
            }
        }

        private static void AddBody(PanelConfig config, Transform parent, RectOffset padding = null)
        {
            const string goName = "BodyWrapper";
            const string textName = "BodyText";

            var wrapper = RenderUtils.CreateChild(parent, goName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RenderUtils.SetupVerticalLayoutGroup(wrapper.GetComponent<VerticalLayoutGroup>(), padding);
            RenderUtils.SetupContentSizeFitter(wrapper.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
            RenderUtils.CreateText(wrapper.transform, config.bodyText, config.bodyTextConfig, textName);
        }

        #endregion

        #region scene ui

        // Hides every OTHER (non-window) Canvas in the scene so only this window — over the camera — is visible.
        // Order-independent: it doesn't matter where this window sits in the sorting stack, which is what the
        // full-screen welcome takeover needs. Ref-counted across windows: the FIRST request captures + disables
        // them; later requests just bump the count, so a closing window can't re-enable UI a newer window hides.
        private void RequestHideOtherUI()
        {
            if (!Application.isPlaying) return;  // never disable real scene canvases from the edit-mode window preview
            if (_requestedHideOtherUI) return;
            _requestedHideOtherUI = true;

            if (_hideOtherUiRequests++ > 0) return;  // already hidden by an earlier window

            HiddenSceneCanvases.Clear();
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                if (!canvas.enabled) continue;
                if (canvas.GetComponentInParent<WindowObject>()) continue;  // never hide a window's own canvas (incl. this one)
                canvas.enabled = false;
                HiddenSceneCanvases.Add(canvas);
            }
        }

        // Releases this window's hide request; restores the scene canvases only when the LAST one closes.
        // Idempotent — safe to call from both Close and OnDestroy.
        private void ReleaseHideOtherUI()
        {
            if (!_requestedHideOtherUI) return;
            _requestedHideOtherUI = false;

            if (--_hideOtherUiRequests > 0) return;
            _hideOtherUiRequests = 0;

            foreach (var canvas in HiddenSceneCanvases.Where(canvas => canvas)) canvas.enabled = true;
            HiddenSceneCanvases.Clear();
        }

        #endregion

        #region animation

        private void PlayShowAnimation()
        {
            // Skip in edit mode — DOTween doesn't tick, so the window would stay invisible.
            if (Application.isPlaying)
                WindowAnimator.PlayShow(_panelRect, _panelCanvasGroup, _panelTargetAlpha, _backdropCanvasGroup, _backdropTargetAlpha, _animConfig);
        }

        private void PlayHideAnimation(Action onComplete) =>
            WindowAnimator.PlayHide(_panelRect, _panelCanvasGroup, _backdropCanvasGroup, _animConfig, () => onComplete());

        #endregion
    }
}
