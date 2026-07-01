using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Handles scene loading with optional loading screen and transitions</summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField, Tooltip("Use a loading screen scene when loading other scenes")]
    private bool useLoadingScreen = false;
    [SerializeField, Tooltip("[if useLoadingScreen is true] Name of the loading screen scene to use")]
    private string loadingSceneName = "LoadingScene";
    [SerializeField, Tooltip("[if useLoadingScreen is true] Minimum time to show loading screen (in seconds)")]
    private float minimumLoadTime = 0.5f;

    [Header("Fade Settings")]
    [SerializeField, Tooltip("Use fade transition when loading scenes")]
    private bool useFade = false;
    [SerializeField, Tooltip("[if useFade is true] CanvasGroup to fade in/out during scene transitions")]
    private CanvasGroup fadeCanvasGroup;
    [SerializeField, Tooltip("[if useFade is true] Duration of fade in/out (in seconds)")]
    private float fadeDuration = 0.5f;

    [SerializeField, Header("Debug"), Tooltip("Show debug logs in console")]
    private bool showDebugLogs = true;

    private static SceneLoader _instance;
    private bool _isLoading;
    private readonly Dictionary<string, AsyncOperation> _preloadedScenes = new();

    private void Awake()
    {
        // Singleton pattern
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Scene Loading

    /// <summary>Load scene by name</summary>
    public void LoadScene(string sceneName)
    {
        if (_isLoading)
        {
            if (showDebugLogs)
                Debug.LogWarning("[SceneLoader] Already loading a scene, ignoring request");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty!");
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Loading scene: {sceneName}");

        // A scene transition always starts at normal time. Opening Settings from the PAUSED HUD leaves
        // Time.timeScale at 0; without this the next scene loads frozen (ball stuck, animations dead) and the
        // fade/loading coroutines — which advance on game time — never progress, so the UI never fades back in.
        Time.timeScale = 1f;

        // Check if scene is preloaded
        if (_preloadedScenes.ContainsKey(sceneName)) StartCoroutine(ActivatePreloadedScene(sceneName));
        else if (useLoadingScreen) StartCoroutine(LoadSceneWithLoadingScreen(sceneName));
        else if (useFade) StartCoroutine(LoadSceneWithFade(sceneName));
        else StartCoroutine(LoadSceneAsync(sceneName));
    }

    /// <summary>Load scene by build index</summary>
    public void LoadScene(int sceneBuildIndex)
    {
        var sceneName = GetSceneNameByIndex(sceneBuildIndex);
        if (!string.IsNullOrEmpty(sceneName)) LoadScene(sceneName);
    }

    /// <summary>Reload current scene</summary>
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Load next scene in build order</summary>
    public void LoadNextScene()
    {
        var nextSceneName = GetNextSceneName();
        if (!string.IsNullOrEmpty(nextSceneName)) LoadScene(nextSceneName);
    }

    /// <summary>Load previous scene in build order</summary>
    public void LoadPreviousScene()
    {
        var prevSceneName = GetPreviousSceneName();
        if (!string.IsNullOrEmpty(prevSceneName)) LoadScene(prevSceneName);
    }

    #endregion

    #region Preloading

    /// <summary>Preload scene by name (loads in background but doesn't activate)</summary>
    public void PreloadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty!");
            return;
        }

        if (_preloadedScenes.ContainsKey(sceneName))
        {
            if (showDebugLogs)
                Debug.Log($"[SceneLoader] Scene '{sceneName}' is already preloaded");
            return;
        }

        StartCoroutine(PreloadSceneAsync(sceneName));
    }

    /// <summary>Preload scene by build index</summary>
    public void PreloadScene(int sceneBuildIndex)
    {
        var sceneName = GetSceneNameByIndex(sceneBuildIndex);
        if (!string.IsNullOrEmpty(sceneName)) PreloadScene(sceneName);
    }

    /// <summary>Preload next scene in build order</summary>
    public void PreloadNextScene()
    {
        var nextSceneName = GetNextSceneName();
        if (string.IsNullOrEmpty(nextSceneName)) return;
        PreloadScene(nextSceneName);
        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Preloading next scene: {nextSceneName}");
    }

    /// <summary>Preload previous scene in build order</summary>
    public void PreloadPreviousScene()
    {
        var prevSceneName = GetPreviousSceneName();
        if (string.IsNullOrEmpty(prevSceneName)) return;
        PreloadScene(prevSceneName);
        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Preloading previous scene: {prevSceneName}");
    }

    /// <summary>Check if scene is preloaded by name</summary>
    public bool IsScenePreloaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        var isPreloaded = _preloadedScenes.ContainsKey(sceneName);

        if (showDebugLogs && isPreloaded)
            Debug.Log(
                $"[SceneLoader] Scene '{sceneName}' preload status: {_preloadedScenes[sceneName].progress * 100}%");

        return isPreloaded;
    }

    /// <summary>Check if scene is preloaded by index</summary>
    public bool IsScenePreloaded(int sceneBuildIndex)
    {
        return IsScenePreloaded(GetSceneNameByIndex(sceneBuildIndex));
    }

    /// <summary>Unload preloaded scene (free memory)</summary>
    public void UnloadPreloadedScene(string sceneName)
    {
        if (!_preloadedScenes.Remove(sceneName)) return;
        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Unloaded preloaded scene: {sceneName}");
    }

    /// <summary>Clear all preloaded scenes</summary>
    public void ClearAllPreloadedScenes()
    {
        _preloadedScenes.Clear();
        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Cleared all preloaded scenes");
    }

    #endregion

    #region Helper Functions

    /// <summary>Get scene name by build index</summary>
    public static string GetSceneNameByIndex(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || sceneBuildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Invalid scene index: {sceneBuildIndex}");
            return null;
        }

        var scenePath = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);
        var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

        return sceneName;
    }

    /// <summary>Get current scene index</summary>
    public static int GetCurrentSceneIndex() =>
        SceneManager.GetActiveScene().buildIndex;

    /// <summary>Get current scene name</summary>
    public static string GetCurrentSceneName() =>
        SceneManager.GetActiveScene().name;

    /// <summary>Get next scene index in build order</summary>
    public static int GetNextSceneIndex() =>
        (GetCurrentSceneIndex() + 1) % SceneManager.sceneCountInBuildSettings;

    /// <summary>Get next scene name in build order</summary>
    public static string GetNextSceneName() =>
        GetSceneNameByIndex(GetNextSceneIndex());

    /// <summary>Get previous scene index in build order</summary>
    public static int GetPreviousSceneIndex() =>
        (GetCurrentSceneIndex() - 1 + SceneManager.sceneCountInBuildSettings) %
        SceneManager.sceneCountInBuildSettings;

    /// <summary>Get previous scene name in build order</summary>
    public static string GetPreviousSceneName() =>
        GetSceneNameByIndex(GetPreviousSceneIndex());

    /// <summary>Get total scene count in build settings</summary>
    public static int GetSceneCount() =>
        SceneManager.sceneCountInBuildSettings;

    #endregion

    #region Private Coroutines

    /// <summary>Preload scene asynchronously in background</summary>
    private IEnumerator PreloadSceneAsync(string sceneName)
    {
        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Starting preload for scene: {sceneName}");

        var operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start preload for scene: {sceneName}");
            yield break;
        }

        operation.allowSceneActivation = false; // Don't activate yet

        _preloadedScenes[sceneName] = operation;

        // Wait until preload is complete (90%)
        while (operation.progress < 0.9f)
        {
            if (showDebugLogs)
                Debug.Log($"[SceneLoader] Preloading '{sceneName}': {operation.progress * 100}%");
            yield return null;
        }

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Scene '{sceneName}' preloaded successfully and ready to activate");
    }

    /// <summary>Activate preloaded scene</summary>
    private IEnumerator ActivatePreloadedScene(string sceneName)
    {
        _isLoading = true;

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Activating preloaded scene: {sceneName}");

        if (_preloadedScenes.ContainsKey(sceneName))
        {
            var operation = _preloadedScenes[sceneName];

            // Apply fade if enabled
            if (useFade && fadeCanvasGroup) yield return StartCoroutine(Fade(1f));

            // Activate the scene
            operation.allowSceneActivation = true;

            // Wait for activation
            yield return operation;

            // Remove from preloaded dictionary
            _preloadedScenes.Remove(sceneName);

            // Fade in if enabled
            if (useFade && fadeCanvasGroup) yield return StartCoroutine(Fade(0f));

            if (showDebugLogs)
                Debug.Log($"[SceneLoader] Preloaded scene '{sceneName}' activated successfully");
        }
        else
        {
            Debug.LogWarning(
                $"[SceneLoader] Scene '{sceneName}' was not in preloaded dictionary, loading normally");
            yield return StartCoroutine(LoadSceneAsync(sceneName));
        }

        _isLoading = false;
    }

    /// <summary>Simple async scene load</summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        _isLoading = true;

        var operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
            yield break;
        }

        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            var progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (showDebugLogs)
                Debug.Log($"[SceneLoader] Loading progress: {progress * 100}%");
            yield return null;
        }

        _isLoading = false;

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded successfully");
    }

    /// <summary>Load scene with loading screen</summary>
    private IEnumerator LoadSceneWithLoadingScreen(string sceneName)
    {
        _isLoading = true;

        // Load loading scene first
        var loadingScreenOp = SceneManager.LoadSceneAsync(loadingSceneName);
        yield return loadingScreenOp;

        var startTime = Time.time;

        // Load target scene in background
        var targetSceneOp = SceneManager.LoadSceneAsync(sceneName);
        if (targetSceneOp == null)
        {
            Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
            yield break;
        }

        targetSceneOp.allowSceneActivation = false;

        // Wait for loading to complete
        while (!targetSceneOp.isDone)
        {
            var progress = Mathf.Clamp01(targetSceneOp.progress / 0.9f);

            if (showDebugLogs)
                Debug.Log($"[SceneLoader] Loading progress: {progress * 100}%");

            // Check if loading is done
            if (targetSceneOp.progress >= 0.9f)
            {
                // Wait for minimum load time
                var elapsed = Time.time - startTime;
                if (elapsed >= minimumLoadTime)
                    targetSceneOp.allowSceneActivation = true;
            }

            yield return null;
        }

        _isLoading = false;

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded successfully");
    }

    /// <summary>Load scene with fade transition</summary>
    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        _isLoading = true;

        if (fadeCanvasGroup) yield return StartCoroutine(Fade(1f)); // Fade out

        // Load scene
        var operation = SceneManager.LoadSceneAsync(sceneName);
        yield return operation;

        if (fadeCanvasGroup) yield return StartCoroutine(Fade(0f)); // Fade in

        _isLoading = false;

        if (showDebugLogs)
            Debug.Log($"[SceneLoader] Scene '{sceneName}' loaded successfully");
    }

    /// <summary>Fade canvas group</summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (!fadeCanvasGroup) yield break;

        var startAlpha = fadeCanvasGroup.alpha;
        var elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // unscaled so the transition still runs if the game is paused (timeScale 0)
            var progress = elapsed / fadeDuration;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    #endregion

    #region Static Methods

    /// <summary>Static method to load scene from anywhere (by name)</summary>
    public static void Load(string sceneName)
    {
        if (_instance != null) _instance.LoadScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    /// <summary>Static method to load scene from anywhere (by index)</summary>
    public static void Load(int sceneIndex)
    {
        var sceneName = GetSceneNameByIndex(sceneIndex);
        if (_instance != null) _instance.LoadScene(sceneName);
        else SceneManager.LoadScene(sceneName);
    }

    /// <summary>Static method to load next scene from anywhere</summary>
    public static void LoadNext()
    {
        if (_instance != null) { _instance.LoadNextScene(); return; }
        var sceneName = GetNextSceneName();
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
    }

    /// <summary>Static method to load previous scene from anywhere</summary>
    public static void LoadPrevious()
    {
        if (_instance != null) { _instance.LoadPreviousScene(); return; }
        var sceneName = GetPreviousSceneName();
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
    }

    /// <summary>Static method to preload scene from anywhere</summary>
    public static void Preload(string sceneName)
    {
        if (_instance != null) _instance.PreloadScene(sceneName);
    }

    /// <summary>Static method to preload next scene from anywhere</summary>
    public static void PreloadNext()
    {
        if (_instance != null) _instance.PreloadNextScene();
    }

    /// <summary>Static method to preload previous scene from anywhere</summary>
    public static void PreloadPrevious()
    {
        if (_instance != null) _instance.PreloadPreviousScene();
    }

    /// <summary>Static method to check if scene is preloaded</summary>
    public static bool IsPreloaded(string sceneName)
    {
        return _instance != null && _instance.IsScenePreloaded(sceneName);
    }

    /// <summary>Static method to reload current scene</summary>
    public static void ReloadCurrent()
    {
        if (_instance != null) _instance.ReloadCurrentScene();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Static method to get scene name by index</summary>
    public static string GetSceneName(int sceneIndex)
    {
        if (_instance != null) return GetSceneNameByIndex(sceneIndex);
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings) return null;
        return System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(sceneIndex));
    }

    #region Other Functions

    /// <summary>Quit application</summary>
    public static void Quit()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #endregion

    #endregion
}
