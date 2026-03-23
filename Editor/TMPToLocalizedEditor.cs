#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MVVM.Localization.Editor
{
    [CustomEditor(typeof(TextMeshProUGUI))]
    internal class TMPToLocalizedEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _internalEditor;

        private void OnEnable()
        {
            var tmpEditorType = Type.GetType("TMPro.EditorUtilities.TMP_EditorPanelUI, Unity.TextMeshPro.Editor");
            if (tmpEditorType != null)
            {
                _internalEditor = CreateEditor(target, tmpEditorType);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawConvertButton();

            EditorGUILayout.Space();

            if (_internalEditor != null)
            {
                _internalEditor.OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox("Could not find TMP_EditorPanelUI. Showing default inspector.", MessageType.Warning);
                DrawDefaultInspector();
            }
        }

        private void DrawConvertButton()
        {
            EditorGUILayout.Space();

            GUIStyle bigButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fixedHeight = 40,
                fontStyle = FontStyle.Bold
            };

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Localization Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Convert to LocalizedTMP", bigButton))
            {
                Convert();
            }

            EditorGUILayout.EndVertical();

            GUI.backgroundColor = originalColor;

            EditorGUILayout.Space();
        }

        private void Convert()
        {
            var tmp = (TextMeshProUGUI)target;
            var go = tmp.gameObject;

            Undo.RegisterCompleteObjectUndo(go, "Convert to LocalizedTMP");

            // Save properties
            var text = tmp.text;
            var font = tmp.font;
            var fontSize = tmp.fontSize;
            var color = tmp.color;
            var fontStyle = tmp.fontStyle;
            var alignment = tmp.alignment;

            DestroyImmediate(tmp);

            var localizedTMP = go.AddComponent<LocalizedTMP>();

            localizedTMP.text = text;
            localizedTMP.font = font;
            localizedTMP.fontSize = fontSize;
            localizedTMP.color = color;
            localizedTMP.fontStyle = fontStyle;
            localizedTMP.alignment = alignment;

            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;

            Report.Event($"✅ Converted {go.name} to LocalizedTMP.");
        }

        private void OnDisable()
        {
            if (_internalEditor != null)
            {
                DestroyImmediate(_internalEditor);
            }
        }
    }
}
#endif