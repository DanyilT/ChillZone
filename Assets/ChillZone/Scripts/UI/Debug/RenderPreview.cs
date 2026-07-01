#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ChillZone.UI.Preview
{
    public abstract class RenderPreview : MonoBehaviour
    {
        protected GameObject PreviewRoot { get; private set; }

        #region abstract/virtual

        protected abstract string PreviewName { get; }

        protected virtual void Rebuild()
        {
            ClearPreview();

            PreviewRoot = new GameObject(PreviewName);
            PreviewRoot.transform.SetParent(transform, false);
            PreviewRoot.hideFlags = HideFlags.DontSave; // DontSave prevents this subtree from being written to the scene file

            // continue in override function...
        }

        #endregion

        #region event

        //OnValidate fires when the config field itself is reassigned in the Inspector.
        private void OnValidate() => ScheduleRebuild();

        private void OnEnable()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            ScheduleRebuild();
        }

        private void OnDisable()
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
            ClearPreview();
        }

        #endregion

        #region core

        //Fires whenever any asset or scene object is modified in the editor.
        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeAssetObjectProperties) continue;
                stream.GetChangeAssetObjectPropertiesEvent(i, out var data);
                var changed = EditorUtility.InstanceIDToObject(data.instanceId);
                if (changed is not (Window.Config.WindowConfig or Window.Config.WindowGlobalConfig or Button1.Config.ButtonConfig or Button1.ButtonManager or Header.Config.AdaptiveHeaderConfig)) continue;
                ScheduleRebuild();
                return;
            }
        }

        //Defer one frame — calling DestroyImmediate from OnValidate / event handlers is not safe; delayCall executes at the start of the next editor update.
        private void ScheduleRebuild() => EditorApplication.delayCall += SafeRebuild;

        // Guard against the component being destroyed before the deferred call fires.
        private void SafeRebuild() { if (this != null) Rebuild(); }

        private void ClearPreview()
        {
            if (PreviewRoot == null) return;
            if (!Application.isPlaying)
                DestroyImmediate(PreviewRoot);
            else
                Destroy(PreviewRoot);
        }

        #endregion
    }
}
#endif
