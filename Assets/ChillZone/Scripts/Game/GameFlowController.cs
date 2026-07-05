using System.Collections;
using ChillZone.Basket;
using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Utils.Native;
using ChillZone.UI.Game;
using ChillZone.UI.Game.Config;
using ChillZone.UI.Window;
using ChillZone.UI.Window.Config;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ChillZone.Ball;
using ChillZone.Gameplay;

namespace ChillZone.Game
{
    /// <summary>
    /// Drives the top-level game flow state machine.
    /// Also owns pause/resume/quit and hardware back-button behaviour:
    ///   Playing  → back → Pause (AR session off, camera kept on, time frozen, frozen-frame overlay)
    ///   Paused   → back → Quit
    ///   Settings → back → Game scene (handled by SettingsSceneController)
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [Header("Window Configs")]
        [SerializeField, Tooltip("Shown on first launch, dismissed by user tap. If null, skips to the manual.")]
        private WindowConfig welcomeWindowConfig;
        [SerializeField, Tooltip("Shown while scanning for surfaces. Auto-advances to placing when a surface is found.")]
        private WindowConfig scanWindowConfig;
        [SerializeField, Tooltip("Shown while placing the basket. Auto-advances to manual when the basket is placed.")]
        private WindowConfig placeWindowConfig;
        [SerializeField, Tooltip("Shown on first launch right after the welcome window (before scanning). Dismissed by user tap. If null, skips to scanning.")]
        private WindowConfig manualWindowConfig;
        [SerializeField, Tooltip("Shown when the device doesn't support AR — the game falls back to the virtual environment. Optional; if null, virtual mode starts silently.")]
        private WindowConfig arUnsupportedWindowConfig;

        [Header("Gameplay References")]
        [SerializeField, Tooltip("If empty, found automatically.")]
        private ARRaycastManager raycastManager;
        [SerializeField, Tooltip("If empty, found automatically.")]
        private ARPlaneManager planeManager;
        [SerializeField, Tooltip("If empty, found automatically.")]
        private ARSurfaceDetector surfaceDetector;
        [SerializeField, Tooltip("If empty, found automatically.")]
        private BasketSpawnManager basketSpawnManager;
        [SerializeField, Tooltip("If empty, found automatically.")]
        private ARSession arSession;
        [SerializeField, Tooltip("Virtual (camera-off) environment controller — the fallback play mode for non-AR devices, and an opt-in toggle otherwise. If empty, found automatically.")]
        private VirtualEnvironmentController virtualEnvironment;
        [SerializeField, Tooltip("AR camera. Kept rendering while paused (the overlay shows a frozen snapshot). If empty, uses Camera.main.")]
        private Camera arCamera;

        [Header("Pause Overlay (code-generated, no prefab)")]
        [SerializeField, Tooltip("Title shown in the centre of the pause overlay.")]
        private string pauseTitle = "Paused";
        [SerializeField, Tooltip("Hints (and their cycle interval) shown at the bottom of the pause overlay. Optional — none shown if unset.")]
        private PauseHintsConfig pauseHints;

        [Header("Debug (editor play mode only)")]
        [SerializeField, Tooltip("Editor-only: skip onboarding, spawn the basket in front of the camera, and start straight in Playing. Ignored in builds.")]
        private bool debugStartInPlayMode;
        [SerializeField, Tooltip("Distance (metres) in front of the camera to spawn the debug basket.")]
        private float debugBasketDistanceMeters = 2f;

        public static GameFlowController Instance { get; private set; }

        /// <summary>Current game-flow state (read-only). Lets late-enabled UI (e.g. the HUD visibility controller) sync without waiting for the next transition.</summary>
        public GameState CurrentState { get; private set; }

        /// <summary>True if the device supports AR (resolved once at startup). False → the game is locked into the virtual environment, so the virtual-env toggle should be hidden.</summary>
        public bool ArSupported { get; private set; } = true;

        // Welcome and Manual are both first-run-only, gated on the ManualViewed pref (written when the
        // manual is dismissed). Each is dismissed only by the user tapping its panel or header.
        private bool _manualViewed;

        // Virtual (camera-off) environment mode. _arSupported is resolved once at startup; when AR is
        // unsupported the game is locked into virtual mode (toggle-off requests are ignored).
        private bool _virtualMode;

        private GameState _stateBeforePause = GameState.Playing;
        private PauseOverlay _pauseOverlay;
        private RenderTexture _pauseSnapshot;
        private Coroutine _pauseCaptureRoutine;

        // Unscaled time of the last resume. An edge-swipe "back" gesture first taps the pause overlay (which
        // resumes via tap-to-resume) and THEN fires the back press a moment later; within this grace window a
        // back press is treated as the paused-state back (quit) instead of re-pausing.
        private float _lastResumeUnscaledTime = -10f;
        private const float ResumeBackGraceSeconds = 0.35f;

        #region lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Entering the Game scene always starts unpaused. Opening Settings from the paused HUD leaves the
            // global Time.timeScale at 0; if it persisted into this fresh scene the game would load frozen, and
            // the back button would then hit the paused handler (→ quit) instead of playing. This scene owns its
            // run state, so it asserts normal time on entry (belt-and-suspenders with SceneLoader's reset).
            Time.timeScale = 1f;

            raycastManager ??= FindObjectOfType<ARRaycastManager>();
            planeManager ??= FindObjectOfType<ARPlaneManager>();
            surfaceDetector ??= FindObjectOfType<ARSurfaceDetector>();
            basketSpawnManager ??= FindObjectOfType<BasketSpawnManager>();
            arSession ??= FindObjectOfType<ARSession>();
            virtualEnvironment ??= FindObjectOfType<VirtualEnvironmentController>();

            RegisterWindowConfigs();
        }

        private void Start()
        {
            _manualViewed = PlayerPrefs.GetInt(PrefKeys.ManualViewed, 0) == 1;
            basketSpawnManager.Initialize(raycastManager, planeManager);
            surfaceDetector.OnSurfaceRequirementMet += OnSurfaceReady;

            if (Application.isEditor && debugStartInPlayMode)
            {
                StartCoroutine(DebugAutoStart());
                return;
            }

            StartCoroutine(InitializeFlow());
        }

        // Resolves AR support before starting onboarding. On a device without ARCore/ARKit, forces the virtual
        // environment (and a notification) instead of stranding the player on a scan screen that never finds a
        // surface. CheckAvailability is a quick platform capability query, not a full session start.
        private IEnumerator InitializeFlow()
        {
            yield return ARSession.CheckAvailability();

            if (ARSession.state == ARSessionState.Unsupported)
            {
                ArSupported = false;
                EnterForcedVirtualMode();
                yield break;
            }

            // Restore the user's last virtual-env choice (persisted) across scene loads / restarts. AR is supported
            // here, so if they had turned it on, drop straight into virtual placing instead of the AR onboarding.
            if (PlayerPrefs.GetInt(PrefKeys.VirtualEnvironment, 0) == 1)
            {
                ApplyVirtualMode(true);
                ShowState(GameState.Placing);
                yield break;
            }

            ShowState(GameState.Welcome);
        }

        // Editor-only fast path: skip the welcome/scan/place/manual flow, drop the basket a fixed distance in
        // front of the camera (XR Simulation surface detection is unreliable), and jump straight into Playing —
        // so gameplay can be iterated on without tapping through onboarding every run. Inert in builds.
        private IEnumerator DebugAutoStart()
        {
            yield return null; // let the XR origin position the camera before we place the basket relative to it

            if (!basketSpawnManager.PlaceBasketInFrontOfCamera(debugBasketDistanceMeters))
                Debug.LogWarning("[GameFlowController] Debug auto-start: couldn't spawn the basket (no camera or basket prefab).", this);

            ShowState(GameState.Playing);
        }

        private void Update()
        {
            if (CurrentState is GameState.Placing or GameState.Playing)
            {
                basketSpawnManager.HandleInput(
                    onPlaced: () => ShowState(GameState.Playing),
                    onDestroyed: () => ShowState(GameState.Placing));
            }
        }

        // Hardware back / Escape is handled by BackNavigationController, which fires the ONE state-aware
        // handler armed on entering Playing (OnGameplayBackPressed: pause while playing, quit while paused).
        // No direct Escape read here — that double-fired with the controller's own read and skipped the pause.

        private void OnEnable()
        {
            EventBus<BallMissedEvent>.Subscribe(OnBallMissed);
            EventBus<TogglePauseRequestedEvent>.Subscribe(OnTogglePauseRequested);
            EventBus<ResetScanRequestedEvent>.Subscribe(OnResetScanRequested);
            EventBus<ToggleVirtualEnvironmentRequestedEvent>.Subscribe(OnToggleVirtualEnvironmentRequested);
        }

        private void OnDisable()
        {
            EventBus<BallMissedEvent>.Unsubscribe(OnBallMissed);
            EventBus<TogglePauseRequestedEvent>.Unsubscribe(OnTogglePauseRequested);
            EventBus<ResetScanRequestedEvent>.Unsubscribe(OnResetScanRequested);
            EventBus<ToggleVirtualEnvironmentRequestedEvent>.Unsubscribe(OnToggleVirtualEnvironmentRequested);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (surfaceDetector) surfaceDetector.OnSurfaceRequirementMet -= OnSurfaceReady;
            if (_pauseSnapshot) { _pauseSnapshot.Release(); Destroy(_pauseSnapshot); _pauseSnapshot = null; }
        }

        #endregion

        #region pause / resume / quit

        public void TogglePause()
        {
            if (CurrentState == GameState.Paused) ResumeGame();
            else PauseGame();
        }

        private void PauseGame()
        {
            if (CurrentState == GameState.Paused) return;  // already paused — keep the stored pre-pause state

            AudioService.PlayUi(UiSound.Pause);  // played here so the pause button, back button and panel tap all match
            _stateBeforePause = CurrentState;  // remember where we paused from (Scanning/Placing/Playing)
            CurrentState  = GameState.Paused;
            Time.timeScale = 0f;  // freeze gameplay/physics immediately

            // Reconfigure the HUD to the paused layout NOW, so the frame we snapshot next matches the live HUD
            // that sits on top of the overlay (no ghost of the gameplay-only buttons baked into the snapshot).
            RaiseStateChanged(_stateBeforePause, GameState.Paused);

            // Capture the frozen camera frame at end-of-frame (after the paused layout renders, before the overlay
            // covers the screen), show it behind the dim, then stop the AR session — so the pause shows a still of
            // the exact moment instead of the black feed an idle AR session leaves behind.
            if (_pauseCaptureRoutine != null) StopCoroutine(_pauseCaptureRoutine);
            _pauseCaptureRoutine = StartCoroutine(CaptureFrozenFrameThenShowOverlay());
        }

        private IEnumerator CaptureFrozenFrameThenShowOverlay()
        {
            yield return new WaitForEndOfFrame();  // let the frozen, paused-layout frame finish rendering
            _pauseCaptureRoutine = null;
            if (CurrentState != GameState.Paused) yield break;  // resumed during the wait

            EnsurePauseSnapshotTexture();
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_pauseSnapshot);

            // CaptureScreenshotIntoRenderTexture comes out vertically flipped on top-left-origin APIs (Metal/Vulkan/D3D).
            ShowPauseOverlay(_pauseSnapshot, SystemInfo.graphicsUVStartsAtTop);
            SetAREnabled(false);  // AR can stop now; the snapshot keeps the view frozen
        }

        private void EnsurePauseSnapshotTexture()
        {
            if (_pauseSnapshot && (_pauseSnapshot.width != Screen.width || _pauseSnapshot.height != Screen.height))
            {
                _pauseSnapshot.Release();
                Destroy(_pauseSnapshot);
                _pauseSnapshot = null;
            }
            if (!_pauseSnapshot)
                _pauseSnapshot = new RenderTexture(Screen.width, Screen.height, 0) { name = "PauseSnapshot" };
        }

        private void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;

            AudioService.PlayUi(UiSound.Unpause);  // matches PauseGame — fires for the pause button, back button and panel tap

            if (_pauseCaptureRoutine != null) { StopCoroutine(_pauseCaptureRoutine); _pauseCaptureRoutine = null; }
            _pauseOverlay?.Hide();

            CurrentState  = _stateBeforePause;   // restore the state we paused from — not always Playing
            Time.timeScale = 1f;
            _lastResumeUnscaledTime = Time.unscaledTime;
            SetAREnabled(true);

            // The single gameplay back handler is state-aware (pauses while playing), so it survives a resume
            // unchanged — re-arm only if it somehow isn't registered (e.g. a window consumed it).
            if (BackNavigationController.Instance && !BackNavigationController.Instance.HasHandlers)
                ArmGameplayBackButton();

            RaiseStateChanged(GameState.Paused, CurrentState);
        }

        // Hardware-back during gameplay routes through this ONE state-aware handler (armed on entering Playing):
        // pause while playing, quit while paused. Being state-driven, it can't do the wrong thing regardless of
        // what else is on the back stack. BackNavigationController pops the handler when it fires, so re-arm
        // after pausing (but not after quitting).
        private void ArmGameplayBackButton()
        {
            BackNavigationController.Instance?.Clear();
            BackNavigationController.Instance?.Push(OnGameplayBackPressed);
        }

        private void OnGameplayBackPressed()
        {
            // Quit if paused — or if we resumed a moment ago: an edge-swipe back gesture taps the overlay
            // (which resumes) and then fires back, which would otherwise re-pause instead of quitting.
            if (CurrentState == GameState.Paused ||
                Time.unscaledTime - _lastResumeUnscaledTime < ResumeBackGraceSeconds)
            {
                SceneLoader.Quit();
                return;
            }
            PauseGame();
            ArmGameplayBackButton();
        }

        #endregion

        #region state machine

        private void ShowState(GameState newState)
        {
            var previous = CurrentState;
            CurrentState = newState;
            WindowManager.Instance?.CloseAll();
            RaiseStateChanged(previous, newState);

            switch (CurrentState)
            {
                case GameState.Welcome:
                    // First-run-only, gated on the same ManualViewed pref as the manual (which writes it
                    // when dismissed) — once the user has been through onboarding, skip straight to scanning.
                    // While shown it stays until the user taps the panel or header (no backdrop close, no
                    // automatic skip); the no-op backdrop handler overrides the config default so an outside
                    // tap can never dismiss it.
                    if (_manualViewed || !welcomeWindowConfig) { ShowState(GameState.Manual); return; }
                    WindowManager.Instance?.Show(welcomeWindowConfig,
                        new WindowShowOptions()
                            .SetOnPanelClick(OnWelcomeDismissed)
                            .SetOnHeaderClick(OnWelcomeDismissed)
                            .SetOnBackdropClick(() => { /* no-op */ }));
                    break;

                case GameState.Scanning:
                    surfaceDetector.ResetDetection();
                    WindowManager.Instance?.Show(scanWindowConfig);
                    ArmGameplayBackButton(); // back → pause from scanning too (armed after the hint window so it sits on top)
                    break;

                case GameState.Placing:
                    WindowManager.Instance?.Show(placeWindowConfig);
                    ArmGameplayBackButton(); // back → pause from placing too
                    break;

                case GameState.Manual:
                    // First-run-only (cached on dismiss), shown right after Welcome. If no config is wired,
                    // don't trap the flow here — go straight to scanning so onboarding continues.
                    if (_manualViewed || !manualWindowConfig) { ShowState(GameState.Scanning); return; }
                    WindowManager.Instance?.Show(manualWindowConfig,
                        new WindowShowOptions()
                            .SetOnPanelClick(OnManualDismissed)
                            .SetOnHeaderClick(OnManualDismissed)
                            .SetOnBackdropClick(() => { /* no-op */ }));
                    break;

                case GameState.Playing:
                    StartPlaySession();
                    ArmGameplayBackButton(); // back → pause; back again (while paused) → quit
                    break;
            }
        }

        #endregion

        #region window dismiss callbacks

        private void OnWelcomeDismissed()
        {
            if (CurrentState != GameState.Welcome) return; // already advanced
            ShowState(GameState.Manual);
        }

        private void OnManualDismissed()
        {
            if (CurrentState != GameState.Manual) return; // already advanced
            PlayerPrefs.SetInt(PrefKeys.ManualViewed, 1);
            PlayerPrefs.Save();
            _manualViewed = true;
            ShowState(GameState.Scanning);
        }

        #endregion

        #region event callbacks

        private void OnSurfaceReady()
        {
            if (CurrentState == GameState.Scanning) ShowState(GameState.Placing);
        }

        private void OnBallMissed(BallMissedEvent evt) { /* hook for haptics / miss overlay */ }

        private void OnTogglePauseRequested(TogglePauseRequestedEvent evt) => TogglePause();

        private void OnResetScanRequested(ResetScanRequestedEvent evt) => ResetScanning();

        private void OnToggleVirtualEnvironmentRequested(ToggleVirtualEnvironmentRequestedEvent evt) => RequestToggleVirtualMode();

        // Clears the placed basket and rescans. In virtual mode there's no surface to scan (the ground is always
        // present), so it goes straight back to placing on the existing virtual ground instead of scanning.
        private void ResetScanning()
        {
            basketSpawnManager.RemoveBasket();

            if (_virtualMode)
            {
                ShowState(GameState.Placing);
                return;
            }

            if (arSession) arSession.Reset();  // destroys all trackables (planes) and restarts tracking
            ShowState(GameState.Scanning);  // also calls surfaceDetector.ResetDetection()
        }

        #endregion

        #region virtual environment

        // Toggle from the HUD button. On devices without AR, virtual mode is the only mode — ignore turn-off.
        private void RequestToggleVirtualMode()
        {
            if (!ArSupported) return;

            var turnOn = !_virtualMode;
            PlayerPrefs.SetInt(PrefKeys.VirtualEnvironment, turnOn ? 1 : 0);  // remember the choice across scene loads / restarts
            PlayerPrefs.Save();
            ApplyVirtualMode(turnOn);
            ShowState(turnOn ? GameState.Placing : GameState.Scanning);
        }

        // Forced at startup when AR is unsupported: enter virtual mode and show the notice; dismissing it (or a
        // missing config) drops straight into placing on the virtual ground.
        private void EnterForcedVirtualMode()
        {
            ApplyVirtualMode(true);

            // First-run-only notice (like the welcome/manual windows): once dismissed it never shows again — the
            // device is simply always in virtual mode. Skip straight to placing when already seen or unconfigured.
            var noticeSeen = PlayerPrefs.GetInt(PrefKeys.ArUnsupportedNoticeViewed, 0) == 1;
            if (noticeSeen || !arUnsupportedWindowConfig)
            {
                ShowState(GameState.Placing);
                return;
            }

            WindowManager.Instance?.Show(arUnsupportedWindowConfig,
                new WindowShowOptions()
                    .SetOnPanelClick(OnArUnsupportedNoticeDismissed)
                    .SetOnHeaderClick(OnArUnsupportedNoticeDismissed)
                    .SetOnBackdropClick(OnArUnsupportedNoticeDismissed));
        }

        private void OnArUnsupportedNoticeDismissed()
        {
            PlayerPrefs.SetInt(PrefKeys.ArUnsupportedNoticeViewed, 1);
            PlayerPrefs.Save();
            ShowState(GameState.Placing);
        }

        // Switches between AR and virtual placement WITHOUT changing the flow state (the caller picks the next
        // state). Clears any placed basket, flips the camera feed/environment, and points the basket spawner at
        // the matching ground raycaster.
        private void ApplyVirtualMode(bool on)
        {
            _virtualMode = on;
            basketSpawnManager.RemoveBasket();

            if (on)
            {
                SetAREnabled(false);
                if (virtualEnvironment) virtualEnvironment.SetEnabled(true);
                basketSpawnManager.UseVirtualGround(0f, virtualEnvironment ? virtualEnvironment.GroundHalfExtent : 0f);
            }
            else
            {
                if (virtualEnvironment) virtualEnvironment.SetEnabled(false);
                basketSpawnManager.UseARSurface();
                SetAREnabled(true);
                if (arSession) arSession.Reset();
            }
        }

        #endregion

        #region helpers

        private void SetAREnabled(bool on)
        {
            if (arSession) arSession.enabled = on;
        }

        private void SetCameraEnabled(bool on)
        {
            var cam = ResolveArCamera();
            if (cam) cam.enabled = on;
        }

        // Resolve while the camera is still enabled — Camera.main returns null once it's off.
        private Camera ResolveArCamera()
        {
            if (arCamera) return arCamera;
            arCamera = Camera.main;
            if (!arCamera) arCamera = FindObjectOfType<Camera>();
            return arCamera;
        }

        // Entering Playing after a basket is placed: a run still in progress keeps its score (the player re-placed
        // mid-run); a finished or brand-new run starts fresh. Either way the selected (last-chosen) ball is put
        // back at the spawn point.
        private static void StartPlaySession()
        {
            if (ScoringSystem.Instance && !ScoringSystem.Instance.HasActiveRun)
                ScoringSystem.Instance.ResetRun();
            BallSpawnManager.Instance?.SpawnSelectedBallAtSpawnPoint();
        }

        private void ShowPauseOverlay(Texture snapshot, bool flipVertically)
        {
            if (!_pauseOverlay)
            {
                var hints = pauseHints ? pauseHints.hints : null;
                var interval = pauseHints ? pauseHints.hintInterval : 3f;
                _pauseOverlay = PauseOverlay.Create(pauseTitle, hints, interval, ResumeGame);
            }
            _pauseOverlay.Show(snapshot, flipVertically);
        }

        private static void RaiseStateChanged(GameState previous, GameState current) => EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent
        {
            Previous = previous,
            Current = current,
        });

        // If windows already registered by WindowManager object in the scene, it will override these. This is just a fallback for when the scene is loaded without a WindowManager object or there is no windows config defined in the registry (registeredWindows)
        private void RegisterWindowConfigs()
        {
            var wm = WindowManager.Instance;
            if (wm == null) return;
            wm.Register(welcomeWindowConfig);
            wm.Register(scanWindowConfig);
            wm.Register(placeWindowConfig);
            wm.Register(manualWindowConfig);
        }

        #endregion
    }
}
