using System;
using System.Collections;
using System.Collections.Generic;
using ChillZone.Core;
using ChillZone.Player;
using ChillZone.UI.Button1;
using ChillZone.UI.ContentPicker.Config;
using ChillZone.UI.Helpers;
using ChillZone.UI.Window.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChillZone.Content.ContentPicker
{
    /// <summary>
    /// Reusable bottom-sheet content picker: owns the open/close/expand animation, drag and
    /// scroll handling, and the populate loop. It is content-agnostic — concrete pickers
    /// (balls, baskets, …) subclass it and supply the items plus the unlock/select behaviour.
    /// </summary>
    public abstract class ContentPickerView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Canvas the picker is drawn on. If empty, falls back to the parent Canvas (GetComponentInParent) at runtime.")]
        private Canvas canvas;
        [SerializeField, Tooltip("Optional: game header RectTransform used as a top margin limit for the expanded picker container.")]
        private RectTransform adaptiveHeader;
        [SerializeField, Tooltip("Optional: ButtonManager that renders the open-trigger. If empty, one is found in the scene.")]
        private ButtonManager triggerButtonManager;
        [SerializeField, Tooltip("buttonId of the ButtonManager button that opens this picker.")]
        private string triggerButtonId;

        [Header("Config")]
        [SerializeField, Tooltip("Drives the generated picker. The backdrop / sheet / scroll / grid / header are all code-generated under the canvas — no scene wiring needed.")]
        private ContentPickerConfig config;
        [SerializeField, Tooltip("Alpha value of the sheet when fully open.")]
        private float openAlpha = 0.7f;

        // Code-generated at runtime (BuildFromConfig); not serialized.
        private RectTransform _sheet;
        private CanvasGroup _canvasGroup;
        private CanvasGroup _backdropCanvasGroup;
        private GameObject _triggerButton;
        private ScrollRect _scrollRect;
        private Transform _itemContainer;

        private float _animDuration = 0.3f;
        private float _minimumSheetHeight = 800f;
        private float _openHeightFraction = 0.5f;

        private float _openHeight;
        private float _fullHeight;
        private bool _isOpen;
        private bool _isFullscreen;
        private float _dragStartY;
        private float _dragStartHeight;

        #region data hooks (subclass)

        /// <summary>The content to list, in display order.</summary>
        protected abstract IReadOnlyList<UnlockableContent> GetContentItems();
        /// <summary>Whether this item is unlocked for the given profile (may also persist criteria-met unlocks).</summary>
        protected abstract bool IsUnlocked(UnlockableContent item, PlayerProfileData profile);
        /// <summary>Whether this item is the currently selected one.</summary>
        protected abstract bool IsSelected(UnlockableContent item);
        /// <summary>Apply a selection (select/persist/spawn) when the user taps an unlocked item.</summary>
        protected abstract void OnContentSelected(UnlockableContent item);

        // Names used to auto-resolve child objects when not wired (overridden per picker).
        protected virtual string TriggerObjectName => null;
        protected virtual string SheetObjectName => "Sheet";
        protected virtual string HeaderObjectName => "Header";

        #endregion

        #region lifecycle

        private void Awake()
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();

            if (config != null)
            {
                _animDuration = config.animDuration;
                _minimumSheetHeight = config.minimumSheetHeight;
                _openHeightFraction = config.openHeightFraction;
                BuildFromConfig();
            }

            // Detect camera/notch position and status bar in canvas units
            var statusBarHeightUnits = CameraDetectionHelper.Detect() is CameraDetectionHelper.CameraPosition.Unknown or CameraDetectionHelper.CameraPosition.Center ? CanvasScalerHelper.StatusBarHeight() : 0f;

            var canvasHeight = Screen.height / CanvasScalerHelper.GetScaleFactor();
            var headerHeight = adaptiveHeader ? adaptiveHeader.rect.height : 0f;
            _fullHeight = Mathf.Max(0f, canvasHeight - statusBarHeightUnits - headerHeight);
            _isFullscreen = canvasHeight <= _minimumSheetHeight;

            // Open height: either fullscreen adjusted or a fraction of the screen (but not below minimum).
            _openHeight = _isFullscreen ? _fullHeight : Mathf.Clamp(Mathf.Max(_minimumSheetHeight, canvasHeight * _openHeightFraction), 0f, _fullHeight);

            ResolveReferences();
            RegisterButtonListener(_triggerButton, Open);

            if (_sheet) _sheet.gameObject.SetActive(true);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }

            SetSheetHeight(0f);
        }

        private void OnEnable()
        {
            if (triggerButtonManager == null && !string.IsNullOrEmpty(triggerButtonId))
                triggerButtonManager = FindObjectOfType<ButtonManager>();

            if (triggerButtonManager != null)
            {
                triggerButtonManager.Rendered += ResolveTriggerFromManager;
                ResolveTriggerFromManager();  // resolve now in case it has already rendered
            }
        }

        private void OnDisable()
        {
            if (triggerButtonManager != null)
                triggerButtonManager.Rendered -= ResolveTriggerFromManager;
        }

        private void Update()
        {
            if (_isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        #endregion

        #region open / close / populate

        private void Open()
        {
            if (_isOpen) return;
            _isOpen = true;

            AudioService.PlayUi(UiSound.Open);
            Populate();
            SetTriggerVisible(false);

            StopAllCoroutines();
            StartCoroutine(AnimateSheet(_openHeight, true));
        }

        protected void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            AudioService.PlayUi(UiSound.Close);
            StopAllCoroutines();
            StartCoroutine(AnimateSheet(0f, false, () => SetTriggerVisible(true)));
        }

        // Header tap → always close (the header's drag is handled separately by SheetDragForwarder).
        public void OnHeaderClick() => Close();

        // Panel (sheet) tap → close only when expanded to (near) full height.
        private void OnPanelClick()
        {
            if (_sheet && _sheet.sizeDelta.y >= _fullHeight - 0.5f) Close();
        }

        private void Populate()
        {
            if (!ValidatePopulateReferences()) return;

            var profile = PlayerProfileManager.Instance ? PlayerProfileManager.Instance.EnsureProfile() : null;

            foreach (Transform child in _itemContainer)
                Destroy(child.gameObject);

            foreach (var item in GetContentItems())
            {
                if (!item) continue;
                config.item.Build(_itemContainer, item, IsUnlocked(item, profile), IsSelected(item), OnContentSelected);
            }
        }

        #endregion

        #region drag / scroll

        public void OnBeginDrag(PointerEventData e)
        {
            if (_isFullscreen) return;

            _dragStartY = e.position.y;
            _dragStartHeight = _sheet != null ? _sheet.sizeDelta.y : 0f;
        }

        public void OnDrag(PointerEventData e)
        {
            if (_isFullscreen || _sheet == null) return;

            // Allow dragging below the peek height (toward 0) so the header can be dragged down to dismiss.
            var delta = e.position.y - _dragStartY;
            var newHeight = Mathf.Clamp(_dragStartHeight + delta, 0f, _fullHeight);
            SetSheetHeight(newHeight);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_isFullscreen || _sheet == null) return;

            // Dragged the header down well below the peek height → slide the rest of the way and close.
            var currentHeight = _sheet.sizeDelta.y;
            if (currentHeight < _openHeight * 0.5f)
            {
                Close();
                return;
            }

            var midpoint = (_openHeight + _fullHeight) * 0.5f;
            var snapTo = currentHeight >= midpoint ? _fullHeight : _openHeight;
            StopAllCoroutines();
            StartCoroutine(AnimateSheet(snapTo, true));
        }

        private IEnumerator AnimateSheet(float targetHeight, bool fadeIn, Action onComplete = null)
        {
            if (_canvasGroup)
                _canvasGroup.blocksRaycasts = fadeIn;
            if (_backdropCanvasGroup)
                _backdropCanvasGroup.blocksRaycasts = fadeIn;

            var startHeight = _sheet ? _sheet.sizeDelta.y : 0f;
            var startAlpha = _canvasGroup ? _canvasGroup.alpha : 0f;
            var endAlpha = fadeIn ? openAlpha : 0f;
            var startBackdrop = _backdropCanvasGroup ? _backdropCanvasGroup.alpha : 0f;
            var endBackdrop = fadeIn ? 1f : 0f;
            var elapsed = 0f;

            while (elapsed < _animDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, elapsed / _animDuration);

                SetSheetHeight(Mathf.Lerp(startHeight, targetHeight, t));

                if (_canvasGroup)
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                if (_backdropCanvasGroup)
                    _backdropCanvasGroup.alpha = Mathf.Lerp(startBackdrop, endBackdrop, t);

                yield return null;
            }

            SetSheetHeight(targetHeight);
            if (_canvasGroup)
                _canvasGroup.alpha = endAlpha;
            if (_backdropCanvasGroup)
                _backdropCanvasGroup.alpha = endBackdrop;

            onComplete?.Invoke();
        }

        private void SetSheetHeight(float height)
        {
            if (!_sheet) return;
            _sheet.sizeDelta = new Vector2(_sheet.sizeDelta.x, height);
        }

        #endregion

        #region references / triggers

        // Code-generates the whole picker under THIS GameObject: a full-screen backdrop, a bottom sheet
        // (image bg + canvas group, no layout group), an image header with the title, and a clamped scroll →
        // masked viewport → padded grid. Registers the backdrop, sheet and header as close triggers.
        private void BuildFromConfig()
        {
            // Backdrop — full screen, tap to close. Its CanvasGroup keeps it invisible / non-blocking while the
            // picker is closed (the animation fades + un-blocks it on open).
            var backdrop = RenderUtils.CreateChild(transform, "Backdrop", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RenderUtils.SetupRectTransformFullScreen(backdrop.GetComponent<RectTransform>());
            RenderUtils.SetupImage(backdrop.GetComponent<Image>(), config.backdropColor, raycastTarget: true);
            _backdropCanvasGroup = backdrop.GetComponent<CanvasGroup>();
            _backdropCanvasGroup.alpha = 0f;
            _backdropCanvasGroup.blocksRaycasts = false;
            RegisterButtonListener(backdrop, Close);  // backdrop tap → close (always)

            // Sheet (panel) — bottom-anchored, full width; image bg + canvas group; height driven by SetSheetHeight.
            // No layout group: the header + scroll are anchored directly (their RectTransforms sit at zero).
            var sheetGo = RenderUtils.CreateChild(transform, SheetObjectName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _sheet = (RectTransform)sheetGo.transform;
            _sheet.anchorMin = new Vector2(0f, 0f);
            _sheet.anchorMax = new Vector2(1f, 0f);
            _sheet.pivot = new Vector2(0.5f, 0f);
            _sheet.offsetMin = Vector2.zero;
            _sheet.offsetMax = Vector2.zero;
            _sheet.sizeDelta = Vector2.zero;
            _canvasGroup = sheetGo.GetComponent<CanvasGroup>();
            RenderUtils.SetupImage(sheetGo.GetComponent<Image>(), config.sheetColor, raycastTarget: true);
            RegisterButtonListener(sheetGo, OnPanelClick);  // panel tap → close only when expanded

            BuildHeader(_sheet);
            BuildScrollGrid(_sheet);
        }

        // Image header pinned to the top of the sheet; height comes from its RectTransform (no layout group /
        // element) and the title fills it. Sits at zero; tapping it closes the sheet.
        private void BuildHeader(Transform parent)
        {
            var header = RenderUtils.CreateChild(parent, HeaderObjectName, typeof(RectTransform), typeof(Image));
            RenderUtils.SetupImage(header.GetComponent<Image>(), config.headerColor, raycastTarget: true);

            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, config.headerHeight);
            rect.anchoredPosition = Vector2.zero;
            header.AddComponent<SheetDragForwarder>().Init(this);  // header is the drag handle (expand / drag-down to close)

            if (!string.IsNullOrEmpty(config.title))
            {
                var title = RenderUtils.CreateText(header.transform, config.title, config.titleTextConfig, "Title");
                RenderUtils.SetupRectTransformFullScreen((RectTransform)title.transform);
                if (title.TryGetComponent<Graphic>(out var titleGraphic)) titleGraphic.raycastTarget = false; // taps fall through to the forwarder
            }
        }

        // Clamped scroll filling below the header → masked viewport (mask graphic hidden) → padded grid at zero.
        private void BuildScrollGrid(Transform parent)
        {
            var scrollGo = RenderUtils.CreateChild(parent, "Scroll", typeof(RectTransform), typeof(ScrollRect));
            _scrollRect = scrollGo.GetComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 25f;

            // Fill from the bottom of the sheet up to just below the header.
            var scrollRectT = (RectTransform)scrollGo.transform;
            scrollRectT.anchorMin = new Vector2(0f, 0f);
            scrollRectT.anchorMax = new Vector2(1f, 1f);
            scrollRectT.offsetMin = Vector2.zero;
            scrollRectT.offsetMax = new Vector2(0f, -config.headerHeight);

            // Viewport — a Mask whose graphic isn't drawn; just clips the grid to the scroll rect.
            var viewport = RenderUtils.CreateChild(scrollGo.transform, "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            var viewportRect = (RectTransform)viewport.transform;
            RenderUtils.SetupRectTransformFullScreen(viewportRect);
            RenderUtils.SetupImage(viewport.GetComponent<Image>(), Color.white, raycastTarget: true);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = viewportRect;

            // Content — top-anchored grid at zero; padding applied to the grid itself.
            var content = RenderUtils.CreateChild(viewport.transform, "Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentRect.sizeDelta.y); // stretch full width (left/right = 0)
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.padding = config.padding ?? new RectOffset();
            grid.cellSize = config.cellSize;
            grid.spacing = config.spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, config.columns);
            grid.childAlignment = TextAnchor.UpperCenter;
            RenderUtils.SetupContentSizeFitter(content.GetComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize);
            _scrollRect.content = contentRect;
            _itemContainer = content.transform;
        }

        private void ResolveReferences()
        {
            // Sheet / header / backdrop are code-generated and registered as close triggers in BuildFromConfig.
            // The open-trigger usually comes from a ButtonManager (triggerButtonId); fall back to a named scene object.
            if (_triggerButton != null || string.IsNullOrEmpty(TriggerObjectName)) return;
            var trigger = GameObject.Find(TriggerObjectName);
            if (trigger != null) _triggerButton = trigger;
        }

        private static void RegisterButtonListener(GameObject target, UnityAction action)
        {
            if (!target || action == null) return;

            var button = target.GetComponent<Button>();
            if (!button)
            {
                var graphic = target.GetComponent<Graphic>();
                if (!graphic)
                {
                    var image = target.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0f);
                    image.raycastTarget = true;
                    graphic = image;
                }

                button = target.AddComponent<Button>();
                button.targetGraphic = graphic;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        // While the sheet is open hide the WHOLE ButtonManager buttons container (all buttons), not just
        // the trigger. Falls back to toggling the trigger GameObject when no ButtonManager is wired.
        private void SetTriggerVisible(bool visible)
        {
            if (triggerButtonManager)
                triggerButtonManager.SetButtonsVisible(visible);
            else if (_triggerButton)
                _triggerButton.SetActive(visible);
        }

        // Resolves the open-trigger from a ButtonManager by buttonId and wires Open. A ButtonManager
        // re-render destroys/recreates its buttons, so this re-runs on every Rendered event.
        private void ResolveTriggerFromManager()
        {
            if (!triggerButtonManager || string.IsNullOrEmpty(triggerButtonId)) return;

            var button = triggerButtonManager.GetRenderedButton(triggerButtonId);
            if (!button) return;

            _triggerButton = button;
            // The trigger opens the picker, which plays the Open sound — drop ButtonManager's generic
            // click from it so a tap isn't heard twice. Re-resolved on every render, so re-apply each time.
            if (button.TryGetComponent<Button>(out var triggerButtonComponent))
                triggerButtonComponent.onClick.RemoveListener(AudioService.PlayButtonClick);
            RegisterButtonListener(_triggerButton, Open);
            SetTriggerVisible(!_isOpen);
        }

        private bool ValidatePopulateReferences()
        {
            if (_itemContainer && config && config.item && GetContentItems() != null) return true;
            Debug.LogError($"{GetType().Name}: missing references for content item population (need a ContentPickerConfig with an item).", this);
            return false;
        }

        #endregion
    }
}
