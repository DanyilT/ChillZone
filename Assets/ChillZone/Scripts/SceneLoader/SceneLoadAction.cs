using JetBrains.Annotations;
using UnityEngine;

// not in use
namespace ChillZone.SceneLoadAction
{
    /// <summary>
    /// Manages the logic for loading specific scenes within the game environment, typically triggered by a UI button press.
    /// This class determines how the scene transition should occur based on predefined load types (e.g., by name, index, or relative navigation).
    /// </summary>
    public class SceneLoadAction : MonoBehaviour
    {
        [SerializeField, Header("Scene to Load"), Tooltip("Method to specify which scene to load. Scene Name and Scene Index require additional fields below.")]
        private LoadType loadType;
        [SerializeField, Header("Load Type: Scene Name"), Tooltip("Scene Name to load. Required if Load Type is Scene Name"), CanBeNull]
        private string sceneName;
        [SerializeField, Header("Load Type: Scene Index"), Tooltip("Scene Index to load. Required if Load Type is Scene Index")]
        private int sceneIndex;
        [SerializeField, Header("Debug"), Tooltip("Enable to show debug logs in the console")]
        protected bool showDebugLogs;

        public SceneLoadAction(LoadType loadType, [CanBeNull] string sceneName, int? sceneIndex, bool showDebugLogs = false)
        {
            this.loadType = loadType;
            this.sceneName = sceneName;
            this.sceneIndex = sceneIndex ?? 0;
            this.showDebugLogs = showDebugLogs;
        }

        public enum LoadType
        {
            SceneName,
            SceneIndex,
            NextScene,
            PreviousScene,
            ReloadCurrent,
            QuitGame
        }

        protected bool HasPreloaded { get; private set; }

        #region public api

        /// <summary>Preload the target scene</summary>
        public void PreloadTargetScene(SceneLoader sceneLoader = null)
        {
            if (HasPreloaded)
            {
                if (showDebugLogs)
                    Debug.Log("[SceneLoadButton] Scene already preloaded");
                return;
            }

            switch (loadType)
            {
                case LoadType.SceneName or LoadType.SceneIndex:
                {
                    SetSceneNameByIndex(sceneIndex);
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        Debug.LogError("[SceneLoadButton] Cannot preload: scene name is empty!");
                        return;
                    }

                    if (sceneLoader != null)
                        sceneLoader.PreloadScene(sceneName);
                    else
                        SceneLoader.Preload(sceneName);
                    HasPreloaded = true;

                    if (showDebugLogs)
                        Debug.Log($"[SceneLoadButton] Preloading scene: {sceneName}");
                    return;
                }
                // Use helper methods from SceneLoader
                case LoadType.NextScene:
                {
                    if (sceneLoader != null)
                        sceneLoader.PreloadNextScene();
                    else
                        SceneLoader.PreloadNext();
                    HasPreloaded = true;

                    if (showDebugLogs)
                        Debug.Log("[SceneLoadButton] Preloading next scene");
                    return;
                }
                // Use helper methods from SceneLoader
                case LoadType.PreviousScene:
                {
                    if (sceneLoader != null)
                        sceneLoader.PreloadPreviousScene();
                    else
                        SceneLoader.PreloadPrevious();
                    HasPreloaded = true;

                    if (showDebugLogs)
                        Debug.Log("[SceneLoadButton] Preloading previous scene");
                    return;
                }
                // Don't preload scene if it is reload or quit
                case LoadType.ReloadCurrent or LoadType.QuitGame:
                {
                    if (showDebugLogs)
                        Debug.Log("[SceneLoadButton] Skipping preload, no need of this");
                    return;
                }
            }
        }

        /// <summary>Load the target scene based on the selected LoadType and parameters</summary>
        public void LoadTargetScene(SceneLoader sceneLoader = null)
        {
            switch (loadType)
            {
                case LoadType.SceneName or LoadType.SceneIndex:
                {
                    SetSceneNameByIndex(sceneIndex);
                    if (string.IsNullOrEmpty(sceneName))
                    {
                        Debug.LogError("[SceneLoadButton] Cannot load: scene name is empty!");
                        return;
                    }

                    if (sceneLoader != null)
                        sceneLoader.LoadScene(sceneName);
                    else
                        SceneLoader.Load(sceneName);

                    if (showDebugLogs)
                        Debug.Log(
                            $"[SceneLoadButton] Loading scene '{sceneName}' (Preloaded: {IsTargetScenePreloaded()})");
                    return;
                }
                case LoadType.NextScene:
                {
                    if (sceneLoader != null)
                        sceneLoader.LoadNextScene();
                    else
                        SceneLoader.LoadNext();
                    return;
                }
                case LoadType.PreviousScene:
                {
                    if (sceneLoader != null)
                        sceneLoader.LoadPreviousScene();
                    else
                        SceneLoader.LoadPrevious();
                    return;
                }
                case LoadType.ReloadCurrent:
                {
                    if (sceneLoader != null)
                        sceneLoader.ReloadCurrentScene();
                    else
                        SceneLoader.ReloadCurrent();
                    return;
                }
                case LoadType.QuitGame:
                {
                    SceneLoader.Quit();
                    return;
                }
            }
        }

        #endregion

        #region helpers

        /// <summary>Helper method to get target scene name based on index for LoadType.SceneIndex</summary>
        protected void SetSceneNameByIndex(int? index)
        {
            index ??= 0;
            if (loadType is LoadType.SceneIndex)
                sceneName = SceneLoader.GetSceneName((int)index);
        }

        /// <summary>Check if target scene is preloaded</summary>
        private bool IsTargetScenePreloaded(SceneLoader sceneLoader = null)
        {
            SetSceneNameByIndex(sceneIndex);
            if (string.IsNullOrEmpty(sceneName)) return false;
            return sceneLoader != null ? sceneLoader.IsScenePreloaded(sceneName) : SceneLoader.IsPreloaded(sceneName);
        }

        #endregion
    }
}
