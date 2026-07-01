#if UNITY_EDITOR
using ChillZone.UI.Window.Config;
using UnityEngine;

namespace ChillZone.UI.Window.Preview
{
    /// <summary>
    /// Attach to any GameObject in the scene to preview a WindowConfig in edit mode.
    /// Changes to the assigned config are reflected immediately without entering Play Mode.
    ///
    /// The preview is non-destructive: the generated objects use HideFlags.DontSave
    /// so they are never written to the scene file.
    /// </summary>
    [ExecuteAlways]
    public class WindowPreview : UI.Preview.RenderPreview
    {
        [SerializeField, Tooltip("The WindowConfig to preview. Changes to the config will be reflected immediately in the scene view.")]
        private WindowConfig config;

        protected override string PreviewName => "_WindowPreview";

        protected override void Rebuild()
        {
            base.Rebuild();

            if (config == null) return;
            WindowObject.Create(config, null, PreviewRoot.transform);
        }
    }
}
#endif
