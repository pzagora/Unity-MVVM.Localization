#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MVVM.Localization.Editor
{
    [CustomEditor(typeof(Text))]
    internal class TextToLocalizedTMPEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor _internalEditor;

        private void OnEnable()
        {
            var textEditorType = Type.GetType("UnityEditor.UI.TextEditor, UnityEditor.UI");
            if (textEditorType != null)
            {
                _internalEditor = CreateEditor(target, textEditorType);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawConvertButton();
            EditorGUILayout.Space();

            if (_internalEditor != null)
                _internalEditor.OnInspectorGUI();
            else
                DrawDefaultInspector();
        }

        private void DrawConvertButton()
        {
            var go = ((Text)target).gameObject;
            if (go.GetComponent<LocalizedTMP>() != null)
                return;

            var bigButton = new GUIStyle(GUI.skin.button)
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
            var legacyText = (Text)target;
            var go = legacyText.gameObject;

            Undo.RegisterCompleteObjectUndo(go, "Convert Text to LocalizedTMP");

            var text = legacyText.text;
            var fontSize = legacyText.fontSize;
            var color = legacyText.color;
            var raycastTarget = legacyText.raycastTarget;
            var richText = legacyText.supportRichText;
            var lineSpacing = legacyText.lineSpacing;
            var bestFit = legacyText.resizeTextForBestFit;
            var bestFitMin = legacyText.resizeTextMinSize;
            var bestFitMax = legacyText.resizeTextMaxSize;
            var anchor = legacyText.alignment;
            var hWrap = legacyText.horizontalOverflow;
            var vWrap = legacyText.verticalOverflow;

            DestroyImmediate(legacyText);

            var localizedTMP = go.AddComponent<LocalizedTMP>();

            localizedTMP.text = text;
            localizedTMP.fontSize = fontSize;
            localizedTMP.color = color;
            localizedTMP.raycastTarget = raycastTarget;
            localizedTMP.richText = richText;

            if (TMP_Settings.defaultFontAsset != null)
                localizedTMP.font = TMP_Settings.defaultFontAsset;

            localizedTMP.textWrappingMode = (hWrap == HorizontalWrapMode.Wrap)
                ? TextWrappingModes.Normal
                : TextWrappingModes.NoWrap;
            localizedTMP.overflowMode = (vWrap == VerticalWrapMode.Overflow)
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;

            localizedTMP.lineSpacing = (lineSpacing - 1f) * 10f;

            localizedTMP.enableAutoSizing = bestFit;
            if (bestFit)
            {
                localizedTMP.fontSizeMin = bestFitMin;
                localizedTMP.fontSizeMax = bestFitMax;
            }

            localizedTMP.alignment = MapAnchorToTMP(anchor);

            EditorUtility.SetDirty(go);
            Selection.activeGameObject = go;

            Report.Event($"✅ Converted {go.name} to LocalizedTMP.");
        }

        private static TextAlignmentOptions MapAnchorToTMP(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;

                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;

                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;

                default: return TextAlignmentOptions.Center;
            }
        }

        private void OnDisable()
        {
            if (_internalEditor != null)
                DestroyImmediate(_internalEditor);
        }
    }
}
#endif