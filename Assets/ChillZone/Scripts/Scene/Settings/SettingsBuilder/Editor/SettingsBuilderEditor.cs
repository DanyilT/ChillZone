using UnityEditor;
using UnityEngine;

namespace ChillZone.Scene.Settings.SettingsBuilder.Editor
{
    [CustomEditor(typeof(SettingsBuilder))]
    public class SettingsBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild"))
                ((SettingsBuilder)target).Rebuild();
        }
    }
}
