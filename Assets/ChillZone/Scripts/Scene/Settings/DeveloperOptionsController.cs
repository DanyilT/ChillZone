using ChillZone.Config;
using ChillZone.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChillZone.Scene.Settings
{
    /// <summary>
    /// Developer Options panel. Lets the developer switch the active throw mode
    /// (Straight / DragPath / Enhanced). Writes the choice into the shared
    /// <see cref="ThrowConfig"/> and persists it through <see cref="ThrowSettingsStore"/>
    /// so it survives scene loads and app restarts.
    ///
    /// Inspector wiring:
    ///   throwConfig — the shared ThrowConfig asset (same one BallSpawnManager uses)
    ///   straight/dragPath/enhanced Button — the three throw-mode buttons
    ///   modeLabel — optional TMP label showing the active mode name
    ///   selectedTint/normalTint — highlight colors for the active mode button
    /// </summary>
    public class DeveloperOptionsController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private ThrowConfig throwConfig;

        [Header("Throw Mode Buttons")]
        [SerializeField] private Button straightButton;
        [SerializeField] private Button dragPathButton;
        [SerializeField] private Button enhancedButton;

        [Header("Display (optional)")]
        [SerializeField] private TextMeshProUGUI modeLabel;

        [Header("Highlight")]
        [SerializeField] private Color selectedTint = new(0.20f, 0.80f, 0.40f, 1f);
        [SerializeField] private Color normalTint = new(1f, 1f, 1f, 0.40f);

        #region lifecycle

        private void Awake() => ThrowSettingsStore.ApplyTo(throwConfig);

        private void OnEnable()
        {
            if (straightButton) straightButton.onClick.AddListener(SelectStraight);
            if (dragPathButton) dragPathButton.onClick.AddListener(SelectDragPath);
            if (enhancedButton) enhancedButton.onClick.AddListener(SelectEnhanced);
            RefreshUI();
        }

        private void OnDisable()
        {
            if (straightButton) straightButton.onClick.RemoveListener(SelectStraight);
            if (dragPathButton) dragPathButton.onClick.RemoveListener(SelectDragPath);
            if (enhancedButton) enhancedButton.onClick.RemoveListener(SelectEnhanced);
        }

        #endregion

        #region ui actions

        private void SelectStraight() => SetMode(ThrowMode.Straight);
        private void SelectDragPath() => SetMode(ThrowMode.DragPath);
        private void SelectEnhanced() => SetMode(ThrowMode.Enhanced);

        private void SetMode(ThrowMode mode)
        {
            ThrowSettingsStore.SetMode(throwConfig, mode);
            RefreshUI();
        }

        #endregion

        #region view

        private void RefreshUI()
        {
            if (throwConfig == null) return;

            Tint(straightButton, throwConfig.mode == ThrowMode.Straight);
            Tint(dragPathButton, throwConfig.mode == ThrowMode.DragPath);
            Tint(enhancedButton, throwConfig.mode == ThrowMode.Enhanced);

            if (modeLabel) modeLabel.text = ModeName(throwConfig.mode);
        }

        private void Tint(Button button, bool selected)
        {
            if (!button) return;
            var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            if (image) image.color = selected ? selectedTint : normalTint;
        }

        private static string ModeName(ThrowMode mode) => mode switch
        {
            ThrowMode.Straight => "Straight",
            ThrowMode.DragPath => "Aimed",
            ThrowMode.Enhanced => "Enhanced (Spin)",
            _ => mode.ToString(),
        };

        #endregion
    }
}
