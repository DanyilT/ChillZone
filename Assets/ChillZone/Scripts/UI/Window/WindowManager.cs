using System.Collections.Generic;
using System.Linq;
using ChillZone.UI.Window.Config;
using UnityEngine;

namespace ChillZone.UI.Window
{
    /// <summary>
    /// Singleton that creates and tracks modal windows.
    /// Attach to a persistent GameObject in the scene (or let it auto-bootstrap).
    /// Register WindowConfig assets in the Inspector; then call
    /// WindowManager.Instance.Show("my-window-id", options) from any script.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }

        [SerializeField, Tooltip("All WindowConfig assets that can be shown by ID.")]
        private WindowConfig[] registeredWindows = {};

        private readonly Dictionary<string, WindowConfig> _registry = new();
        private readonly Dictionary<string, WindowObject> _open = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject(nameof(WindowManager));
            go.AddComponent<WindowManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Instance.AdoptConfigFrom(this);   // a scene-placed manager's config wins over the auto-bootstrap fallback
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            RebuildRegistry();
        }

        // The BeforeSceneLoad auto-bootstrap creates the singleton before any scene manager Awakes, so a
        // scene WindowManager would otherwise be destroyed with its Inspector config. Hand it over instead.
        private void AdoptConfigFrom(WindowManager other)
        {
            if (other.registeredWindows is not { Length: > 0 }) return;
            registeredWindows = other.registeredWindows;
            RebuildRegistry();
        }

        private void OnValidate() => RebuildRegistry();

        #region registration

        /// <summary>Register a config at runtime (e.g. from other scripts).</summary>
        public void Register(WindowConfig config)
        {
            if (config == null) return;
            var id = config.windowId;
            if (string.IsNullOrEmpty(id)) return;
            _registry[id] = config;
        }

        private void RebuildRegistry()
        {
            _registry.Clear();
            foreach (var cfg in registeredWindows)
                Register(cfg);
        }

        #endregion

        #region show / close

        /// <summary>Show a window by ID. Returns the WindowObjectWindowObject instance, or null if the ID is unknown.</summary>
        public WindowObject Show(string windowId, WindowShowOptions options = null)
        {
            if (_registry.TryGetValue(windowId, out var config)) return Show(config, options);
            Debug.LogWarning($"[WindowManager] No WindowConfig registered with id '{windowId}'.");
            return null;
        }

        /// <summary>Show a window directly from a config asset.</summary>
        public WindowObject Show(WindowConfig config, WindowShowOptions options = null)
        {
            if (!config) return null;

            // Close any existing instance of this window first (silent — it's a re-show, not a user close).
            if (!string.IsNullOrEmpty(config.windowId) && _open.Remove(config.windowId, out var existing) && existing)
                existing.Close(false);

            var window = WindowObject.Create(config, options, transform);
            _open[config.windowId] = window;
            return window;
        }

        /// <summary>Close a window by ID.</summary>
        public void Close(string windowId)
        {
            if (string.IsNullOrEmpty(windowId)) return;
            if (!_open.Remove(windowId, out var window)) return;
            if (window) window.Close();
        }

        /// <summary>Close all open windows.</summary>
        public void CloseAll()
        {
            // Snapshot before clearing: WindowObject.Close() calls back into
            // NotifyWindowClosed (which mutates _open), so iterating _open directly
            // would throw "collection modified during enumeration".
            var windows = _open.Values.ToList();
            _open.Clear();
            // Silent: CloseAll is teardown (scene change / every GameFlowController state transition), not a user close.
            foreach (var window in windows.Where(window => window)) window.Close(false);
        }

        /// <summary>True if a window with this ID is currently open.</summary>
        public bool IsOpen(string windowId) => _open.TryGetValue(windowId, out var w) && w != null;

        /// <summary>Called by WindowObject when it begins closing, so the registry stays consistent.</summary>
        internal void NotifyWindowClosed(string windowId) => _open.Remove(windowId);

        #endregion
    }
}
