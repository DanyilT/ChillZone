using ChillZone.Core;
using ChillZone.Core.Events;
using ChillZone.Game;
using ChillZone.UI.Button1;
using UnityEngine;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// Drives HUD button visibility from the game-flow state (listens to <see cref="GameStateChangedEvent"/>),
    /// so gameplay buttons are hidden by STATE rather than relying on a window's hideOtherUI:
    /// <list type="bullet">
    /// <item>Welcome / Manual — full-screen windows: ALL groups hidden (canvas off).</item>
    /// <item>Paused — only the <see cref="mainGroupName"/> group (settings + pause).</item>
    /// <item>Scanning / Placing — main + the <see cref="pickerGroupName"/> group (so a ball/basket can be picked).</item>
    /// <item>Playing — main + EVERY gameplay group (resets, pickers, …).</item>
    /// </list>
    /// Also keeps the pause toggle icon synced to the pause state. Event-driven so the gameplay controller stays
    /// decoupled from the generic ButtonManager. Window <c>hideOtherUI</c> is left intact for its own job —
    /// hiding canvases BELOW a window's sort order.
    /// </summary>
    public class HudVisibilityController : MonoBehaviour
    {
        [SerializeField, Tooltip("Buttons whose groups are toggled by state. If empty, found in the scene.")]
        private ButtonManager buttonManager;
        [SerializeField, Tooltip("Name of the always-on group (settings + pause) that stays visible whenever the HUD is up (gameplay + paused).")]
        private string mainGroupName = "main";
        [SerializeField, Tooltip("Name of the picker group (ball/basket picker triggers). Shown during Scanning, Placing AND Playing; hidden when Paused / in Welcome/Manual.")]
        private string pickerGroupName = "picker";
        [SerializeField, Tooltip("buttonId of the pause toggle (an 'externally driven' isToggle ButtonConfig). Its icon is synced to the pause state here, so it also updates on overlay tap-to-resume / back button. Empty = no sync.")]
        private string pauseButtonId = "pause-toggle";
        [SerializeField, Tooltip("buttonId of the virtual-environment toggle. Hidden on devices that don't support AR (they're locked into virtual mode, so the toggle would be a no-op). Empty = never hidden.")]
        private string virtualToggleButtonId = "virtual-environment-toggle";

        private GameState _currentState = GameState.Welcome;

        private void Awake()
        {
            if (!buttonManager) buttonManager = FindObjectOfType<ButtonManager>();
        }

        private void OnEnable()
        {
            EventBus<GameStateChangedEvent>.Subscribe(OnStateChanged);
            if (buttonManager) buttonManager.Rendered += OnButtonsRendered;
        }

        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnStateChanged);
            if (buttonManager) buttonManager.Rendered -= OnButtonsRendered;
        }

        // Sync to the current state in case this was enabled after the first transition already fired.
        private void Start()
        {
            if (GameFlowController.Instance != null) Apply(GameFlowController.Instance.CurrentState);
        }

        private void OnStateChanged(GameStateChangedEvent evt) => Apply(evt.Current);

        // A re-render recreates the buttons, so re-sync the pause icon + virtual-toggle visibility afterwards.
        private void OnButtonsRendered()
        {
            SyncPauseIcon(_currentState);
            SyncVirtualToggleVisibility();
        }

        private void Apply(GameState state)
        {
            _currentState = state;

            var hud = state is GameState.Scanning or GameState.Placing or GameState.Playing or GameState.Paused;
            var gameplay = state is GameState.Scanning or GameState.Placing or GameState.Playing;
            var playing = state is GameState.Playing;

            if (buttonManager)
            {
                buttonManager.SetButtonsVisible(hud);  // whole canvas off during Welcome/Manual
                if (hud)
                {
                    // Reset/other gameplay groups show ONLY while Playing; the picker group ALSO shows during
                    // Scanning + Placing; main stays whenever the HUD is up. Apply main LAST so it always wins
                    // even if a group name overlaps.
                    buttonManager.SetAllGroupsVisible(playing);
                    buttonManager.SetGroupVisible(pickerGroupName, gameplay);
                    buttonManager.SetGroupVisible(mainGroupName, true);
                }
            }

            SyncPauseIcon(state);
            SyncVirtualToggleVisibility();
        }

        // On devices without AR support the game is locked into the virtual environment, so the virtual-env
        // toggle would be a no-op — hide it. Re-applied after every render (a re-render recreates the button).
        private void SyncVirtualToggleVisibility()
        {
            if (!buttonManager || string.IsNullOrEmpty(virtualToggleButtonId)) return;
            if (!GameFlowController.Instance || GameFlowController.Instance.ArSupported) return;

            var go = buttonManager.GetRenderedButton(virtualToggleButtonId);
            if (go) go.SetActive(false);
        }

        // Keep the pause button's toggle icon reflecting the pause state. The pause ButtonConfig is an
        // 'externally driven' toggle (no flip-on-click), so this is the single source of truth — the icon
        // updates whether the state changed via the pause button, the overlay tap-to-resume, or the back button.
        private void SyncPauseIcon(GameState state)
        {
            if (!buttonManager || string.IsNullOrEmpty(pauseButtonId)) return;
            var go = buttonManager.GetRenderedButton(pauseButtonId);
            if (go && go.TryGetComponent<ButtonToggleVisual>(out var toggle))
                toggle.SetState(state != GameState.Paused);  // ON (pause icon) while playing, OFF (play icon) while paused
        }
    }
}
