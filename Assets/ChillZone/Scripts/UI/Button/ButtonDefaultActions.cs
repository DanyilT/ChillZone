using System;
using UnityEngine;
using ChillZone.Core;
using ChillZone.Core.Events;

namespace ChillZone.UI.Button1
{
    public static class ButtonDefaultActions
    {
        #region SceneLoader

        /// <summary>Load a scene by name.</summary>
        public static Action LoadScene(string sceneName) => () => SceneLoader.Load(sceneName);

        /// <summary>Load a scene by build index.</summary>
        public static Action LoadScene(int sceneIndex) => () => SceneLoader.Load(sceneIndex);

        /// <summary>Load the next scene in build order.</summary>
        public static Action LoadNextScene() => SceneLoader.LoadNext;

        /// <summary>Load the previous scene in build order.</summary>
        public static Action LoadPreviousScene() => SceneLoader.LoadPrevious;

        /// <summary>Reload the current active scene.</summary>
        public static Action ReloadCurrentScene() => SceneLoader.ReloadCurrent;

        /// <summary>Quit the application.</summary>
        public static Action Quit() => SceneLoader.Quit;

        #endregion

        #region Other

        /// <summary>Request a pause toggle; GameFlowController handles it via the event bus (keeps UI decoupled from the gameplay assembly).</summary>
        public static Action TogglePause() => () => EventBus<TogglePauseRequestedEvent>.Raise(default);

        /// <summary>Request resetting/respawning the active ball; BallSpawnManager handles it via the event bus (score reset is conditional).</summary>
        public static Action ResetBall() => () => EventBus<ResetBallRequestedEvent>.Raise(default);

        /// <summary>Request clearing scanned surfaces and returning to the scanning state; GameFlowController handles it via the event bus.</summary>
        public static Action ResetScanning() => () => EventBus<ResetScanRequestedEvent>.Raise(default);

        /// <summary>Request toggling the virtual (camera-off) environment; GameFlowController handles it via the event bus.</summary>
        public static Action ToggleVirtualEnvironment() => () => EventBus<ToggleVirtualEnvironmentRequestedEvent>.Raise(default);

        /// <summary>Open a URL in the system browser.</summary>
        public static Action OpenURL(string url) => () => Application.OpenURL(url);

        /// <summary>Wraps a raw Action as a Button action.</summary>
        public static Action Custom(Action action) => action;

        #endregion
    }
}
