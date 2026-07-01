using ChillZone.Core;
using ChillZone.Gameplay;
using ChillZone.Player;
using ChillZone.UI.Window;
using ChillZone.UI.Window.Config;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.UI.Game
{
    /// <summary>
    /// Header score button: opens the best-score window and feeds it the player's current best score via
    /// <see cref="WindowShowOptions.PrimaryText"/>. Attach to the score container's Button in the header.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BestScoreButton : MonoBehaviour
    {
        [SerializeField, Tooltip("BestScoreWindowConfig to show. Preferred — no WindowManager registration needed.")]
        private BestScoreWindowConfig windowConfig;
        [SerializeField, Tooltip("Fallback windowId (a registered WindowConfig) used when no config is assigned above.")]
        private string windowId = "best-score";

        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(Show);
            button.onClick.AddListener(AudioService.PlayButtonClick);   // not a ButtonManager button, so wire the click SFX here
        }

        private void OnDestroy()
        {
            if (TryGetComponent<Button>(out var button)) button.onClick.RemoveListener(Show);
        }

        private void Show()
        {
            if (WindowManager.Instance == null) return;

            var options = new WindowShowOptions().SetPrimaryText(BestScore().ToString());

            // Prefer the directly-assigned config; otherwise fall back to the registered windowId.
            if (windowConfig != null) WindowManager.Instance.Show(windowConfig, options);
            else if (!string.IsNullOrEmpty(windowId)) WindowManager.Instance.Show(windowId, options);
            else Debug.LogError("BestScoreButton.Show() — no windowConfig or windowId assigned. Cannot show best-score window.");
        }

        // Max of the saved best and the current (uncommitted) run, so a new high shows immediately — the run
        // only commits to the profile on a miss, which is why this previously read 0 right after scoring.
        private static int BestScore()
        {
            var profile = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.EnsureProfile() : null;
            var savedBest  = profile?.BestScore ?? 0;
            var currentRun = ScoringSystem.Instance != null ? ScoringSystem.Instance.CurrentRunScore : 0;
            return Mathf.Max(savedBest, currentRun);
        }
    }
}
