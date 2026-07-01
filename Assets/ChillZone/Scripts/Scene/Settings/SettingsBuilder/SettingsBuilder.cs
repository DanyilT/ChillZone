using UnityEngine;

namespace ChillZone.Scene.Settings.SettingsBuilder
{
    /// <summary>
    /// Builds the settings screen from a <see cref="SettingsCellRegistry"/>. Runs with
    /// [ExecuteAlways] so the screen renders in edit mode (regenerated each time, never
    /// serialised into the scene — see SettingsCellBuilder's DontSave flag).
    ///
    /// Runtime build happens in Start (after <see cref="SettingsActions"/> has registered
    /// its handlers in Awake); edit-mode build happens on enable and on validation.
    /// </summary>
    [ExecuteAlways]
    public class SettingsBuilder : MonoBehaviour
    {
        [SerializeField] private SettingsCellRegistry registry;
        [SerializeField] private SettingsCellBuilder cellBuilder;
        [SerializeField] private SettingsActions actions;
        [SerializeField] private bool buildOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool renderInEditMode = true;

        #region lifecycle

        private void OnEnable()
        {
            if (!Application.isPlaying && renderInEditMode) Rebuild(); // edit-mode preview
        }

        private void Start()
        {
            if (Application.isPlaying && buildOnStart) Rebuild();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || !renderInEditMode) return;
            UnityEditor.EditorApplication.delayCall += DelayedEditorRebuild;
        }

        private void DelayedEditorRebuild()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedEditorRebuild;
            if (this == null || Application.isPlaying) return;
            Rebuild();
        }
#endif

        #endregion

        #region build (public)

        public void Rebuild()
        {
            if (!cellBuilder || !registry) return;
            if (!gameObject.scene.IsValid()) return; // skip prefab-asset import / stage edge cases

            cellBuilder.ClearAllCells();
            foreach (var cell in registry.cells)
                if (cell && cell.ShouldBuild) cell.Build(cellBuilder, actions);
        }

        #endregion
    }
}
