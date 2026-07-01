using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChillZone.Content
{
    [CreateAssetMenu(fileName = "ContentRegistry", menuName = "ChillZone/Content/Content Registry")]
    public class UnlockableContentRegistry : ScriptableObject
    {
        public ContentTypes contentType;
        public List<UnlockableContent> content = new();

        public UnlockableContent GetById(string contentId) => string.IsNullOrWhiteSpace(contentId) ? null : content.Find(item => item && item.GetStableId() == contentId);
        public UnlockableContent GetDefaultContent() => content.Find(item => item);
        public List<UnlockableContent> GetUnlocked(IPlayerProgress profile) => content.FindAll(item => item != null && ((profile != null && profile.HasContentUnlocked(contentType, item.GetStableId())) || item.unlockCriteria == null || item.unlockCriteria.IsUnlocked(profile)));
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UnlockableContentRegistry))]
    public class UnlockableContentRegistryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var registry = (UnlockableContentRegistry)target;

            if (!GUILayout.Button("Auto Populate")) return;
            Undo.RecordObject(registry, "Auto Populate Content");
            registry.content.Clear();

            var filter = registry.contentType switch
            {
                ContentTypes.Ball => "t:BallData",
                ContentTypes.Basket => "t:BasketData",
                _ => null
            };

            if (filter == null) return;

            var guids = AssetDatabase.FindAssets(filter);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UnlockableContent>(path);
                if (asset) registry.content.Add(asset);
            }

            EditorUtility.SetDirty(registry);
        }
    }
#endif
}
