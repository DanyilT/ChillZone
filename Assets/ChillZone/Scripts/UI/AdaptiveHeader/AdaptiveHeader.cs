using System.Collections;
using System.Collections.Generic;
using ChillZone.UI.Header.Config;
using ChillZone.UI.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Header
{
    /// <summary>
    /// A single, config-driven adaptive header. Wire <see cref="leftContainer"/> to your
    /// compact element (score / credits button) and <see cref="rightContainer"/> to your
    /// expanded element (marquee / title). Everything else — height, side-swapping,
    /// collapse vs. swap behavior — is controlled by <see cref="AdaptiveHeaderConfig"/>.
    ///
    /// The header lays its children out with a HorizontalLayoutGroup: the two containers
    /// flex by ratio while the camera / notch is inserted as a fixed-width spacer element,
    /// so the panels automatically resize to keep clear of it.
    ///
    /// Note: <see cref="headerPanel"/> should contain only the two containers (its background
    /// belongs on the panel itself), since the layout group controls every child it has.
    /// </summary>
    public class AdaptiveHeader : MonoBehaviour
    {
        public enum HeaderElementPosition { Left, Center, Right }

        [Header("References")]
        [SerializeField, Tooltip("Canvas used for layout + safe-area/status-bar/cutout measurement. If left empty, the header falls back to its parent Canvas (GetComponentInParent) at runtime.")]
        private Canvas canvas;
        [SerializeField, Tooltip("Config asset that controls all layout decisions. Changes to the config will be applied at runtime when RefreshLayout() is called.")]
        private AdaptiveHeaderConfig config;

        [Header("Containers")]
        [SerializeField, Tooltip("The header panel RectTransform whose height will be set and whose children are laid out.")]
        private RectTransform headerPanel;
        [SerializeField, Tooltip("Compact side (score in Game scene, credits button in Settings scene).")]
        private RectTransform leftContainer;
        [SerializeField, Tooltip("Expanded side (marquee in Game scene, title in Settings scene).")]
        private RectTransform rightContainer;

        [SerializeField, Header("Debug"), Tooltip("When true, debug info from the camera / cutout detection helpers will be printed to the console and shown as an overlay on the canvas. This is useful for testing how the header responds to different camera positions and cutout sizes without needing to build to a device. The detected camera position and cutout rect (if any) are shown in the overlay.")]
        private bool showDebugInfo;

        // States
        private bool _isInitialized;
        private RectTransform _cutoutSpacer;
        private CameraDetectionHelper.CameraPosition _cameraPos;

        // Events
        /// <summary>Fired once the header has finished initializing (after the first frame).</summary>
        public event System.Action OnInitialized;

        #region lifecycle

        private void Awake() => StartCoroutine(Initialize());

        private IEnumerator Initialize()
        {
            yield return new WaitForEndOfFrame();

            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (showDebugInfo)
                CanvasScalerHelper.ShowInfo(canvas, canvas.GetComponent<CanvasScaler>());

            ComputeHeight();
            ComputePositions();
            ApplyLayout();

            _isInitialized = true;
            OnInitialized?.Invoke();
        }

        #endregion

        #region public api

        public void RefreshLayout()
        {
            if (!gameObject.activeInHierarchy) return;
            CameraDetectionHelper.ClearCache();
            StartCoroutine(Initialize());
        }

        public HeaderElementPosition LeftPosition { get; private set; } = HeaderElementPosition.Left;
        public HeaderElementPosition RightPosition { get; private set; } = HeaderElementPosition.Right;
        public float HeaderHeight { get; private set; }

        #endregion

        #region layout steps (private)

        private void ComputeHeight() => HeaderHeight = ResolveHeight(config, config && config.useStatusBarHeight ? CanvasScalerHelper.StatusBarHeight(canvas, canvas.GetComponent<CanvasScaler>()) : 0f);

        private void ComputePositions()
        {
            if (!config || !config.useAdaptiveHeader)
            {
                (LeftPosition, RightPosition) = (HeaderElementPosition.Left, HeaderElementPosition.Right);
                return;
            }

            _cameraPos = CameraDetectionHelper.Detect(showDebugInfo);
            (LeftPosition, RightPosition) = ResolvePositions(_cameraPos, config.useCollapse);
        }

        private void ApplyLayout()
        {
            if (!headerPanel || !leftContainer || !rightContainer || !config) return;

            var canvasWidth = ((RectTransform)canvas.transform).rect.width;
            var scaler = canvas.GetComponent<CanvasScaler>();

            // Safe-area insets (canvas units). Only applied when useSafeArea is enabled.
            float leftInset = 0f, rightInset = 0f, topInset = 0f;
            if (config.useSafeArea)
            {
                var safe = CanvasScalerHelper.SafeArea(canvas);
                leftInset = Mathf.Max(0f, safe.xMin);
                rightInset = Mathf.Max(0f, canvasWidth - safe.xMax);
                topInset = CanvasScalerHelper.StatusBarHeight(canvas, scaler);
            }

            // Cutout width (canvas units). The notch only overlaps header content when the header sits in the status-bar row (i.e., NOT inset into the safe area).
            var cutoutWidth = config.useAdaptiveHeader && !config.useSafeArea ? ComputeCutoutWidth() : 0f;

            EnsureSpacer();
            ApplyHeaderLayout(headerPanel, leftContainer, rightContainer, _cutoutSpacer, config, HeaderHeight, leftInset, rightInset, topInset, _cameraPos, LeftPosition, RightPosition, cutoutWidth);
        }

        /// <summary>Exact detected cutout width in canvas units (no padding). 0 when there is no cutout.</summary>
        private float ComputeCutoutWidth()
        {
            var info = CameraDetectionHelper.DetectCutout(showDebugInfo);
            if (!info.Exists || Screen.width <= 0) return 0f;
            // The spacer matches the camera/notch exactly — convert the detected pixel width to canvas units.
            return CanvasScalerHelper.PixelsToCanvasUnits(info.Rect.width, canvas);
        }

        /// <summary>Lazily create the cutout spacer child under the header panel.</summary>
        private void EnsureSpacer()
        {
            if (_cutoutSpacer) return;
            var go = new GameObject("_CutoutSpacer", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(headerPanel, false);
            _cutoutSpacer = (RectTransform)go.transform;
        }

        #endregion

        #region pure layout helpers (public static methods - shared with the editor preview)

        /// <summary>Resolve header height from config and a measured status-bar height (0 if unavailable).</summary>
        public static float ResolveHeight(AdaptiveHeaderConfig config, float statusBarHeight)
        {
            if (!config) return 100f;
            if (!config.useStatusBarHeight) return config.fixedHeight;
            return statusBarHeight > 0f ? Mathf.Max(statusBarHeight, config.minHeight) : config.fixedHeight;
        }

        /// <summary>
        /// Resolve which side each container occupies so the compact element avoids the
        /// camera / notch.
        ///
        /// useCollapse = true  → element on the camera side moves to Center.
        /// useCollapse = false → the two elements swap sides.
        /// </summary>
        public static (HeaderElementPosition left, HeaderElementPosition right) ResolvePositions(CameraDetectionHelper.CameraPosition cameraPos, bool useCollapse) => cameraPos switch
        {
            CameraDetectionHelper.CameraPosition.Left => useCollapse ? (HeaderElementPosition.Center, HeaderElementPosition.Right) : (HeaderElementPosition.Right, HeaderElementPosition.Center),
            CameraDetectionHelper.CameraPosition.Right => useCollapse ? (HeaderElementPosition.Left, HeaderElementPosition.Center) : (HeaderElementPosition.Center, HeaderElementPosition.Left),
            _ => (HeaderElementPosition.Left, HeaderElementPosition.Right)
        };

        /// <summary>
        /// Lay the header out as a horizontal row: the two containers flex by ratio while the
        /// cutout is treated as a fixed-width spacer element. Shared by the runtime instance and
        /// the editor preview so both produce identical layouts.
        ///
        /// • The panel is a top-anchored bar inset by the safe-area margins.
        /// • <paramref name="leftPos"/> / <paramref name="rightPos"/> (the keep-both swap logic) set the
        ///   element ORDER; <paramref name="cameraPos"/> slots the spacer at the notch side.
        /// • Element WIDTHS come from the ratio — the alternate cutoutLeftPanelRatio is used
        ///   whenever a cutout is present (<paramref name="cutoutWidth"/> &gt; 0).
        /// </summary>
        public static void ApplyHeaderLayout(RectTransform headerPanel, RectTransform left, RectTransform right, RectTransform spacer, AdaptiveHeaderConfig config, float headerHeight, float leftInset, float rightInset, float topInset, CameraDetectionHelper.CameraPosition cameraPos, HeaderElementPosition leftPos, HeaderElementPosition rightPos, float cutoutWidth)
        {
            // 1. Panel as a top-anchored bar, inset by the safe area and pushed below the status bar.
            headerPanel.anchorMin = new Vector2(0f, 1f);
            headerPanel.anchorMax = new Vector2(1f, 1f);
            headerPanel.pivot     = new Vector2(0.5f, 1f);
            headerPanel.offsetMin = new Vector2(leftInset,  -topInset - headerHeight);
            headerPanel.offsetMax = new Vector2(-rightInset, -topInset);

            // 2. Horizontal layout group that distributes leftover width by flexible ratio.
            var hlg = headerPanel.GetComponent<HorizontalLayoutGroup>() ?? headerPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = false;   // honour flexibleWidth ratios exactly
            hlg.childForceExpandHeight = true;
            hlg.spacing = config.headerSpacing;
            hlg.padding = config.headerPadding;

            // 3. Element widths: normal ratio, or the cutout-specific ratio when a notch is present.
            var hasCutout = cutoutWidth > 0f && spacer;
            var leftRatio = hasCutout ? config.cutoutLeftPanelRatio : config.leftPanelRatio;
            SetFlexibleWidth(left,  leftRatio);
            SetFlexibleWidth(right, 1f - leftRatio);

            // 4. Cutout spacer: a fixed-width, non-flexing element standing in for the notch.
            if (spacer)
            {
                spacer.gameObject.SetActive(hasCutout);
                if (hasCutout)
                {
                    var le = spacer.GetComponent<LayoutElement>() ?? spacer.gameObject.AddComponent<LayoutElement>();
                    le.flexibleWidth  = 0f;
                    le.minWidth       = cutoutWidth;
                    le.preferredWidth = cutoutWidth;
                }
            }

            // 5. Order the row left→right: elements by their resolved position, spacer at the notch side.
            OrderRow(left, right, spacer, hasCutout, cameraPos, leftPos, rightPos);
        }

        #endregion

        #region layout helpers (private static methods)

        private static void SetFlexibleWidth(RectTransform rect, float flex)
        {
            var le = rect.GetComponent<LayoutElement>();
            if (!le) le = rect.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = Mathf.Max(0f, flex);
            le.minWidth = 0f;
            le.preferredWidth = -1f;   // let flexibleWidth drive the size
        }

        /// <summary>
        /// Assign sibling indices so children read left→right. Element ranks come from the
        /// keep-both swap logic; the spacer rank comes from the camera side. ResolvePositions
        /// guarantees the elements vacate the notch slot, so ranks never collide.
        /// </summary>
        private static void OrderRow(RectTransform left, RectTransform right, RectTransform spacer, bool hasCutout, CameraDetectionHelper.CameraPosition cameraPos, HeaderElementPosition leftPos, HeaderElementPosition rightPos)
        {
            var items = new List<(int rank, Transform t)>(3)
            {
                (PositionRank(leftPos),  left),
                (PositionRank(rightPos), right),
            };
            if (hasCutout && spacer) items.Add((CameraRank(cameraPos), spacer));

            items.Sort((a, b) => a.rank.CompareTo(b.rank));
            for (int i = 0; i < items.Count; i++)
                items[i].t.SetSiblingIndex(i);
        }

        private static int PositionRank(HeaderElementPosition pos) => pos switch
        {
            HeaderElementPosition.Left   => 0,
            HeaderElementPosition.Center => 1,
            _                            => 2,
        };

        private static int CameraRank(CameraDetectionHelper.CameraPosition pos) => pos switch
        {
            CameraDetectionHelper.CameraPosition.Left  => 0,
            CameraDetectionHelper.CameraPosition.Right => 2,
            _                                          => 1,   // Center / fallback
        };

        #endregion
    }
}
