using UnityEngine;
using UnityEngine.UI;

// not in use
namespace ChillZone.SceneLoadAction
{
    /// <summary>
    /// Simple component to load scene when button is clicked.
    /// Attach this to any button
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SceneLoadButton : SceneLoadAction
    {
        [Header("Preloading (note: not applicable on \"Reload Current\" or \"Quit Game)\")")]
        [SerializeField, Tooltip("Should the scene be preloaded?")]
        private bool preloadScene;
        [SerializeField, Tooltip("When to preload: Awake, Start, or OnEnable")]
        private PreloadTiming preloadTiming;
        public SceneLoadButton(bool preloadScene, PreloadTiming preloadTiming, LoadType loadType, string sceneName, int sceneIndex, bool showDebugLogs) : base(loadType, sceneName, sceneIndex, showDebugLogs)
        {
            this.preloadScene = preloadScene;
            this.preloadTiming = preloadTiming;
        }

        public enum PreloadTiming
        {
            Awake,
            Start,
            OnEnable
        }

        private Button _button;
        private SceneLoader _sceneLoader;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _sceneLoader = FindObjectOfType<SceneLoader>();
            if (preloadScene && preloadTiming == PreloadTiming.Awake) PreloadTargetScene();
        }

        private void Start()
        {
            _button?.onClick.AddListener(LoadTargetScene);
            if (preloadScene && preloadTiming == PreloadTiming.Start) PreloadTargetScene();
        }

        private void OnEnable()
        {
            if (preloadScene && preloadTiming == PreloadTiming.OnEnable && !HasPreloaded) PreloadTargetScene();
        }

        private void OnDestroy() => _button?.onClick.RemoveListener(LoadTargetScene);

        private void PreloadTargetScene() => base.PreloadTargetScene(_sceneLoader);
        private void LoadTargetScene() => base.LoadTargetScene(_sceneLoader);
    }
}
