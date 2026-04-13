#if UNITY_EDITOR
using Commons.Editor;
using UnityEditor;
using UnityEngine;

namespace MVVM.Localization.Editor
{
    [CustomEditor(typeof(LocalizedTMP))]
    internal class LocalizedTMPEditor : StyleLinkedTMPEditor
    {
        public override void OnInspectorGUI()
        {
            DrawKeyAndArgs();
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }

        private void DrawKeyAndArgs()
        {
            var localizedTMP = (LocalizedTMP)target;

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Localization Key", EditorStyles.boldLabel);

            var key  = localizedTMP.Model?.Key;
            var args = localizedTMP.Model?.Args;

            if (string.IsNullOrEmpty(key))
            {
                EditorGUILayout.HelpBox("Visible in Play Mode only. Assign localization key via ViewModel if needed.", MessageType.Warning);
            }
            else
            {
                var keyStyle = new GUIStyle(EditorStyles.textField)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Normal,
                    normal = { textColor = EditorStyles.label.normal.textColor }
                };

                EditorGUILayout.BeginHorizontal();

                GUI.enabled = false;
                EditorGUILayout.TextField(key, keyStyle, GUILayout.Height(20));
                GUI.enabled = true;

                if (GUILayout.Button("📋 Copy", GUILayout.Width(60)))
                {
                    EditorGUIUtility.systemCopyBuffer = key;
                    Report.Event($"Localization key copied: {key}");
                }

                EditorGUILayout.EndHorizontal();

                if (args is { Length: > 0 })
                {
                    EditorGUILayout.Space();
                    GUILayout.Label("Format Arguments ({0}, {1}, ...)", EditorStyles.miniBoldLabel);

                    GUI.enabled = false;
                    for (int i = 0; i < args.Length; i++)
                    {
                        EditorGUILayout.TextField(new GUIContent($"Arg {i}"), args[i]?.ToString() ?? string.Empty);
                    }
                    GUI.enabled = true;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}
#endif
