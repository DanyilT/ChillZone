using System;
using System.Collections.Generic;
using System.Linq;
using ChillZone.Core;
using ChillZone.UI.Button1.Config;
using ChillZone.UI.Header;
using ChillZone.UI.Helpers;
using ChillZone.UI.Utils;
using ChillZone.UI.Utils.Config;
using ChillZone.UI.Window;
using ChillZone.UI.Window.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Button1
{
    /// <summary>
    /// Manages button placement within safe area with corner and center positioning
    /// </summary>
    public class ButtonManager : MonoBehaviour
    {
        [Serializable]
        public class ButtonGroup
        {
            [Tooltip("Name for this button group (for organization only).")]
            public string groupName = "Button Group";
            [Tooltip("List of buttons to position in this group.")]
            public List<ButtonConfig> buttons = new();
            [Tooltip("Corner to position buttons from.")]
            public TextAnchor corner = TextAnchor.UpperLeft;
            [Tooltip("Stack direction for buttons.")]
            public RectTransform.Axis stackDirection = RectTransform.Axis.Vertical;
            [Tooltip("Spacing between buttons (horizontal or vertical).")]
            public float spacing = 50f;
            [Tooltip("Reverse button order (top-to-bottom for vertical, left-to-right for horizontal).")]
            public bool reverseOrder;
        }

        [Header("Canvas"), SerializeField, Tooltip("Preset for the buttons' OWN canvas (created as a child). For a fully independent canvas the ButtonManager should be a ROOT object — nested under another Canvas, render mode + scaler are inherited.")]
        private CanvasUtils.Preset canvasPreset = CanvasUtils.Preset.Overlay;
        [SerializeField, Tooltip("Sorting order for the buttons' canvas. Keep it ABOVE the pause overlay and the AdaptiveHeader so the buttons (e.g. the main settings/pause group) stay on top and clickable while paused.")]
        private int canvasSortingOrder = 1020;

        [SerializeField, Header("Button Groups"), Tooltip("List of button groups to position on the canvas.")]
        private List<ButtonGroup> buttonGroups = new();
        [SerializeField, Header("Margins"), Tooltip("Margins around the canvas")]
        private RectOffset offset = new ();
        [SerializeField, Header("Settings"), Tooltip("Use safe area (true) or full canvas size (false).")]
        private bool useSafeArea = true;
        [SerializeField, Tooltip("Update button positions on start (true) or wait for manual Refresh() call (false).")]
        private bool updateOnStart = true;

        [SerializeField, Header("Header Clearance"), Tooltip("When a header is present, the safe-area container's TOP inset uses the header height instead of the status-bar height, so buttons start right below the header. With no header it uses the normal safe-area inset.")]
        private bool mindHeaderHeight = true;
        [SerializeField, Tooltip("Header to mind when 'Mind Header Height' is on. If left empty, one is found in the scene at runtime.")]
        private AdaptiveHeader header;

        private RectTransform _canvasRect;
        private RectTransform _safeAreaContainer;
        private Dictionary<string, List<GameObject>> _renderedButtons = new();
        private readonly List<GameObject> _renderedGroups = new();
        private readonly Dictionary<string, GameObject> _renderedGroupContainers = new();
        private readonly Dictionary<string, bool> _groupVisible = new();
        private bool _buttonsVisible = true;

        #region event

        private void Reset() => offset = new RectOffset(50, 50, 50, 50);

        private void Awake()
        {
            InitCanvas();
            ResolveHeader();
        }

        private void OnDestroy()
        {
            if (header != null) header.OnInitialized -= OnHeaderInitialized;
        }

        private void Start()
        {
            // Render now so the container is populated immediately; OnHeaderInitialized re-renders to apply
            // the header height once it's measured (re-render is idempotent, so the double pass is safe).
            if (updateOnStart) RenderAll();
        }

        #endregion

        #region rendering

        /// <summary>Render all buttons on the canvas based on the defined groups and their configurations</summary>
        private void RenderAll()
        {
            ClearAllRendered();
            if (!_canvasRect)
            {
                InitCanvas();
                if (!_canvasRect) { Debug.LogError("[ButtonManager] No Canvas found!"); return; }
            }

            SetupSafeAreaContainer();  // re-apply insets — the header height may have resolved since Awake

            foreach (var group in buttonGroups.Where(group => group.buttons.Count != 0))
                RenderGroup(group);

            Rendered?.Invoke();
        }

        /// <summary>Destroy every rendered group container (and the buttons inside it) and clear tracking.</summary>
        private void ClearAllRendered()
        {
            foreach (var go in _renderedGroups.Where(go => go != null))
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }

            _renderedGroups.Clear();
            _renderedGroupContainers.Clear();
            _renderedButtons.Clear();
        }

        /// <summary>Render a single button group by creating a parent GameObject for the group and positioning each button based on the group's corner, stack direction, spacing, and order settings.</summary>
        private void RenderGroup(ButtonGroup group)
        {
            if (group.buttons.Count == 0) return;

            if (!_renderedButtons.ContainsKey(group.groupName))
                _renderedButtons[group.groupName] = new List<GameObject>();

            // Left-handed mode mirrors every group horizontally: the anchor corner flips left<->right and
            // horizontal stacks reverse their order, so the whole layout is a mirror image for left-hand reach.
            var leftHanded = LeftHanded;
            var corner = leftHanded ? MirrorCorner(group.corner) : group.corner;
            var reverseOrder = leftHanded && group.stackDirection == RectTransform.Axis.Horizontal ? !group.reverseOrder : group.reverseOrder;

            const string groupNamePrefix = "group_";
            var groupGo = RenderUtils.CreateChild(useSafeArea ? _safeAreaContainer : _canvasRect, groupNamePrefix + group.groupName, typeof(RectTransform));
            _renderedGroups.Add(groupGo);
            _renderedGroupContainers[group.groupName] = groupGo;
            // Re-apply visibility set before this (re-)render so SetGroupVisible survives rebuilds.
            if (_groupVisible.TryGetValue(group.groupName, out var groupVisible)) groupGo.SetActive(groupVisible);
            var groupRect = groupGo.GetComponent<RectTransform>();
            groupRect.anchorMin = groupRect.anchorMax = groupRect.pivot = UIRenderUtils.GetPivot(corner);
            groupRect.anchoredPosition = Vector2.zero;

            RenderUtils.SetupHorizontalOrVerticalLayoutGroup(group.stackDirection == RectTransform.Axis.Vertical ? groupGo.AddComponent<VerticalLayoutGroup>() : groupGo.AddComponent<HorizontalLayoutGroup>(), offset, corner, group.spacing, reverseOrder);

            // Size the group to its buttons so the corner pivot anchors it correctly — without this the
            // rect stays zero-sized and a bottom-anchored group lays its buttons out downward, off-screen.
            RenderUtils.SetupContentSizeFitter(groupGo.AddComponent<ContentSizeFitter>(), ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize);

            foreach (var button in group.buttons.Where(button => button != null))
            {
                var btnGo = RenderButton(button, groupGo.transform);
                if (!btnGo) continue;
                btnGo.GetComponent<RectTransform>().sizeDelta = button.buttonAppearance.size;
                _renderedButtons[group.groupName].Add(btnGo);
            }
        }

        /// <summary>Render a single button with the specified configuration and parent transform. Returns the created GameObject representing the button.</summary>
        private static GameObject RenderButton(ButtonConfig config, Transform parent)
        {
            var appearance = config.buttonAppearance;
            var icon = config.buttonIcon;
            var toggle = config.buttonToggle;
            var label = config.buttonLabel;
            var blockRaycasts = config.buttonBehaviour.blockRaycasts;

            var btnGo = RenderUtils.CreateChild(parent, config.buttonId, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.GetComponent<RectTransform>().sizeDelta = appearance.size;

            var bg = btnGo.GetComponent<Image>();
            var button = btnGo.GetComponent<Button>();
            UIRenderUtils.ApplyBackground(bg, appearance.backgroundImage, blockRaycasts);
            RenderUtils.SetupButton(button, bg, appearance.transition, ResolveAction(config.actionEntry), appearance.darkBackground ? appearance.ColorTintBlock : null);
            WireButtonSound(button, config);   // per-action UI click SFX

            // Toggle buttons need an icon to flip even if only the OFF sprite is set.
            var hasIcon = icon.iconSprite || (toggle.isToggle && toggle.iconSpriteOff);
            if (hasIcon)
            {
                var iconImage = UIRenderUtils.RenderIcon(btnGo.transform, icon, blockRaycasts).GetComponent<Image>();
                RenderUtils.SetupRectTransformFullScreen(iconImage.GetComponent<RectTransform>(), icon.iconPadding);

                if (toggle.isToggle)
                {
                    var toggleVisual = btnGo.AddComponent<ButtonToggleVisual>();
                    toggleVisual.Init(iconImage, icon.iconColor, icon.iconSprite, toggle.iconColorOff, toggle.iconSpriteOff, toggle.toggleStartsOn);
                    // Externally-driven toggles (e.g. the pause/play button) get their icon from game state via
                    // SetState — NOT flipped on click — so it stays correct however the state changed (button,
                    // overlay tap-to-resume, or back button). Wiring Toggle() too would double-flip and desync.
                    if (!toggle.externallyDriven)
                        button.onClick.AddListener(toggleVisual.Toggle);
                }
            }

            if (!string.IsNullOrEmpty(label.text))
                RenderUtils.SetupRectTransformFullScreen(UIRenderUtils.RenderText(btnGo.transform, label.text, label.textConfig, "Label").GetComponent<RectTransform>(), label.padding);

            return btnGo;
        }

        #endregion

        #region public methods

        /// <summary>Refresh button positions (e.g. after orientation change or safe area update)</summary>
        public void Refresh() => RenderAll();

        /// <summary>Raised after every (re-)render. Cached references to rendered buttons must be re-resolved here — a re-render destroys and recreates the button GameObjects.</summary>
        public event Action Rendered;

        /// <summary>The rendered button GameObject whose config buttonId matches, or null. Valid only after a render (see <see cref="Rendered"/>).</summary>
        public GameObject GetRenderedButton(string buttonId) => string.IsNullOrEmpty(buttonId) ? null : _renderedButtons.SelectMany(kv => kv.Value).FirstOrDefault(go => go != null && go.name == buttonId);

        /// <summary>Show or hide ALL buttons by toggling the buttons' own canvas. Persists across re-renders.</summary>
        public void SetButtonsVisible(bool visible)
        {
            _buttonsVisible = visible;
            // While a window is hiding other UI (hideOtherUI), stay hidden even when asked to show — otherwise a
            // re-render side effect (e.g. a closed picker re-showing its trigger) would re-enable us over the hide.
            if (ButtonsCanvas) ButtonsCanvas.enabled = visible && !WindowObject.IsHidingOtherUI;
        }

        /// <summary>Show or hide ONE button group by name. Persists across re-renders (re-applied on every rebuild), so it can be set before the group has rendered.</summary>
        public void SetGroupVisible(string groupName, bool visible)
        {
            if (string.IsNullOrEmpty(groupName)) return;
            _groupVisible[groupName] = visible;
            if (_renderedGroupContainers.TryGetValue(groupName, out var go) && go) go.SetActive(visible);
        }

        /// <summary>Show or hide EVERY group at once — e.g. hide all, then re-show only the 'main' group while paused.</summary>
        public void SetAllGroupsVisible(bool visible)
        {
            foreach (var group in buttonGroups)
                SetGroupVisible(group.groupName, visible);
        }

        /// <summary>The buttons' own canvas (child of this ButtonManager), created on first init. Null before that.</summary>
        public Canvas ButtonsCanvas { get; private set; }

        /// <summary>Get button group by name.</summary>
        public ButtonGroup GetGroupByName(string nameToFind) => buttonGroups.Find(g => g.groupName == nameToFind);

        /// <summary>Find the first ButtonConfig whose buttonId matches across all groups.</summary>
        public ButtonConfig GetButtonById(string id) => buttonGroups.SelectMany(g => g.buttons).FirstOrDefault(b => b != null && b.buttonId == id);

        /// <summary>Add a button to a group.</summary>
        public void AddButtonToGroup(int groupIndex, ButtonConfig button)
        {
            if (groupIndex < 0 || groupIndex >= buttonGroups.Count) return;
            buttonGroups[groupIndex].buttons.Add(button);
            RenderAll();
        }

        /// <summary>Remove a button from a group.</summary>
        public void RemoveButtonFromGroup(int groupIndex, ButtonConfig button)
        {
            if (groupIndex < 0 || groupIndex >= buttonGroups.Count) return;
            buttonGroups[groupIndex].buttons.Remove(button);
            RenderAll();
        }

        /// <summary>Clear all button groups.</summary>
        public void ClearAllGroups() => buttonGroups.Clear();

#if UNITY_EDITOR
        /// <summary>
        /// Copies all serialized ButtonManager fields (groups, margins, canvas settings, …) from
        /// <paramref name="source"/> into this instance, then builds the buttons canvas from them.
        /// Used exclusively by preview components in edit mode.
        /// </summary>
        public void ApplyPreviewConfig(ButtonManager source)
        {
            // Canvas settings are now serialized fields, so the JSON copy carries them too.
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), this);
            InitCanvas();
        }

        /// <summary>Replaces the group list with the supplied groups (subset preview).</summary>
        public void SetPreviewGroups(List<ButtonGroup> groups) => buttonGroups = groups;
#endif

        #endregion

        #region helpers

        // Left-handed mode is a device preference (ButtonManagers aren't DDOL, so each scene reads it itself).
        private static bool LeftHanded => PlayerPrefs.GetInt(PrefKeys.LeftHanded, 0) == 1;

        // Flip a corner anchor across the vertical axis (left<->right); vertical-centred corners are unchanged.
        private static TextAnchor MirrorCorner(TextAnchor corner) => corner switch
        {
            TextAnchor.UpperLeft => TextAnchor.UpperRight,
            TextAnchor.UpperRight => TextAnchor.UpperLeft,
            TextAnchor.MiddleLeft => TextAnchor.MiddleRight,
            TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
            TextAnchor.LowerLeft => TextAnchor.LowerRight,
            TextAnchor.LowerRight => TextAnchor.LowerLeft,
            _ => corner
        };

        private void InitCanvas()
        {
            EnsureButtonsCanvas();
            ApplyCanvasSettings();
            Canvas.ForceUpdateCanvases();   // lay out the freshly-created canvas so its rect is valid for the safe-area math
            _canvasRect = (RectTransform)ButtonsCanvas.transform;
            SetupSafeAreaContainer();
        }

        // The buttons live on their OWN canvas, created as a child of this ButtonManager, so they can be
        // sorted and shown/hidden independently of the scene UI. Reused if it already exists.
        private void EnsureButtonsCanvas()
        {
            if (ButtonsCanvas) return;
            var existing = transform.Find("ButtonsCanvas");
            ButtonsCanvas = existing && existing.TryGetComponent(out Canvas found) ? found : CanvasUtils.CreateChildCanvas(transform, "ButtonsCanvas", canvasPreset);
        }

        private void ApplyCanvasSettings()
        {
            CanvasUtils.Apply(ButtonsCanvas, canvasPreset);
            // Layer the buttons above the pause overlay + header. overrideSorting makes the order take effect
            // even if this canvas ends up nested under another (a ROOT object is still preferred for Overlay).
            ButtonsCanvas.overrideSorting = true;
            ButtonsCanvas.sortingOrder = canvasSortingOrder;
            // canvas.enabled is owned by SetButtonsVisible (and external hides like a window's hideOtherUI).
            // Re-asserting it on every render would let a rebuild re-enable a canvas that hideOtherUI disabled, so it is deliberately NOT set here.
        }

        private void SetupSafeAreaContainer()
        {
            EnsureButtonsCanvas();
            if (!useSafeArea) return;

            var canvasTransform = (RectTransform)ButtonsCanvas.transform;
            if (!_safeAreaContainer)
                _safeAreaContainer = RenderUtils.CreateChild(canvasTransform, "_SafeAreaContainer", typeof(RectTransform)).GetComponent<RectTransform>();

            var safeArea = CanvasScalerHelper.SafeArea(ButtonsCanvas);
            var canvasSize = canvasTransform.rect.size;

            // Bottom / left / right always honour the safe area (home indicator, notch, rounded corners).
            // The bottom also clears the Android nav bar, which Screen.safeArea omits (see NavigationBarHeight).
            var leftInset = Mathf.Max(0f, safeArea.xMin);
            var rightInset = Mathf.Max(0f, canvasSize.x - safeArea.xMax);
            var bottomInset = Mathf.Max(Mathf.Max(0f, safeArea.yMin), CanvasScalerHelper.NavigationBarHeight(ButtonsCanvas));

            // Top: the safe area (status-bar height) normally, but the HEADER height when a header is
            // present — so content starts right below the header instead of below the status bar.
            var topInset = Mathf.Max(0f, canvasSize.y - safeArea.yMax);
            if (mindHeaderHeight && header && header.HeaderHeight > 0f)
                topInset = header.HeaderHeight;

            // offsetMin = inset from canvas bottom-left; offsetMax = inset from canvas top-right (negative = inward).
            _safeAreaContainer.anchorMin = Vector2.zero;
            _safeAreaContainer.anchorMax = Vector2.one;
            _safeAreaContainer.offsetMin = new Vector2(leftInset, bottomInset);
            _safeAreaContainer.offsetMax = new Vector2(-rightInset, -topInset);
        }

        #region header

        private void ResolveHeader()
        {
            if (!mindHeaderHeight) return;
            if (header == null) header = FindObjectOfType<AdaptiveHeader>();
            if (header != null) header.OnInitialized += OnHeaderInitialized;
        }

        // The header measures its height a frame after load (and again on RefreshLayout); re-render once
        // it reports in so the safe-area container's top inset uses the real header height instead of 0.
        private void OnHeaderInitialized() { if (gameObject.activeInHierarchy) RenderAll(); }

        #endregion

        /// <summary>
        /// Wait a frame for canvas to be ready
        /// </summary>
        private IEnumerable<WaitForEndOfFrame> DelayedUpdate()
        {
            yield return new WaitForEndOfFrame();
            RenderAll();
        }

        /// <summary>
        /// Resolve button action based on the provided ButtonActionEntry, which can specify a predefined action type (like loading a scene or opening a URL) or a custom Action delegate. Returns the Action to execute on button click, or null if no valid action is defined.
        /// </summary>
        private static Action ResolveAction(ButtonActionEntry entry) => entry.actionType switch
        {
            ButtonActionType.TogglePause => ButtonDefaultActions.TogglePause(),
            ButtonActionType.OpenURL => string.IsNullOrEmpty(entry.url)
                ? ButtonDefaultActions.OpenURL(entry.url) : null,
            ButtonActionType.LoadScene => string.IsNullOrEmpty(entry.sceneName)
                ? () => ButtonDefaultActions.LoadScene(entry.sceneIndex)
                : () => ButtonDefaultActions.LoadScene(entry.sceneName),
            ButtonActionType.LoadNextScene => ButtonDefaultActions.LoadNextScene(),
            ButtonActionType.LoadPreviousScene => ButtonDefaultActions.LoadPreviousScene(),
            ButtonActionType.ReloadCurrentScene => ButtonDefaultActions.ReloadCurrentScene(),
            ButtonActionType.Quit => ButtonDefaultActions.Quit(),
            ButtonActionType.ResetScanning => ButtonDefaultActions.ResetScanning(),
            ButtonActionType.ResetBall => ButtonDefaultActions.ResetBall(),
            _ => null
        };

        /// <summary>
        /// Wire the per-action UI click sound. Pause/unpause is played by GameFlowController (so the back
        /// button and the pause-overlay tap match the pause button), so the pause toggle stays silent here;
        /// reset buttons and ordinary (non-externally-driven) toggles get their own sound, the rest click.
        /// Named method groups (not lambdas) are used so a listener stays removable — the content picker
        /// drops the generic click from its trigger so opening plays only the Open sound.
        /// </summary>
        private static void WireButtonSound(Button button, ButtonConfig config)
        {
            var actionType = config.actionEntry.actionType;
            switch (actionType)
            {
                case ButtonActionType.TogglePause:
                    return;
                case ButtonActionType.ResetBall or ButtonActionType.ResetScanning:
                    button.onClick.AddListener(PlayResetSound);
                    break;
                default:
                {
                    if (config.buttonToggle is { isToggle: true, externallyDriven: false })
                        button.onClick.AddListener(PlayToggleSound);
                    else
                        button.onClick.AddListener(AudioService.PlayButtonClick);
                    break;
                }
            }
        }

        private static void PlayResetSound()  => AudioService.PlayUi(UiSound.Reset);
        private static void PlayToggleSound() => AudioService.PlayUi(UiSound.Toggle);

        #endregion
    }
}
