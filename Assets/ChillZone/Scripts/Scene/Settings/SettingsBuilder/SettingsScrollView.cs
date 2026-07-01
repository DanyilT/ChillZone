using ChillZone.UI.Header;
using ChillZone.UI.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.Scene.Settings.SettingsBuilder
{
    /// <summary>
    /// Fills the area below the AdaptiveHeader with a ScrollRect, accounting for the safe area.
    /// Subscribe to <see cref="AdaptiveHeader.OnInitialized"/> to receive the final header height
    /// before setting scroll-view offsets.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class SettingsScrollView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform viewportRect;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private AdaptiveHeader headerScript;
        [SerializeField] private RectTransform headerRect;

        [Header("Content")]
        [SerializeField]
        private RectOffset padding;
        private void Reset() => padding = new RectOffset(50, 50, 50, 50);
        [SerializeField]
        private float contentSpacing = 20f;

        [SerializeField, Header("Safe Area"), Tooltip("When true, leaves room for the navigation bar at the bottom.")]
        private bool useBottomSafeArea = true;

        #region lifecycle

        private void Awake()
        {
            SetupContentLayout();
        }

        private void Start() => SetupScrollViewArea();

        private void OnEnable()
        {
            if (headerScript != null)
                headerScript.OnInitialized += SetupScrollViewArea;
        }

        private void OnDisable()
        {
            if (headerScript != null)
                headerScript.OnInitialized -= SetupScrollViewArea;
        }

        #endregion

        #region public api

        public void RefreshLayout()
        {
            if (!gameObject.activeInHierarchy) return;
            SetupScrollViewArea();
            SetupContentLayout();
        }

        public void ScrollToTop() => GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
        public void ScrollToBottom() => GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;

        #endregion

        #region helpers (private)

        private void SetupScrollViewArea()
        {
            var safeBottom = useBottomSafeArea ? CanvasScalerHelper.NavigationBarHeight() : 0f;
            var safeTop = headerRect ? headerRect.rect.height : 0f;

            var scrollRect = GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.pivot = new Vector2(0.5f, 1f);
            scrollRect.offsetMin = new Vector2(0f, safeBottom);
            scrollRect.offsetMax = new Vector2(0f, -safeTop);
        }

        private void SetupContentLayout()
        {
            if (contentRect == null) return;

            var fitter = contentRect.GetComponent<ContentSizeFitter>() ?? contentRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var verticalLayout = contentRect.GetComponent<VerticalLayoutGroup>() ?? contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = contentSpacing;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.padding = padding;
        }

        #endregion
    }
}
