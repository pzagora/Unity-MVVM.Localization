using System;
using Commons.Editor.Helpers;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVVM.Localization.Editor
{
    [CustomEditor(typeof(LocalizationSetup))]
    public class LocalizationSetupEditor : UnityEditor.Editor
    {
        private ReorderableList _languageCodesList;

        private SerializedProperty _fallbackLanguageProperty;
        private SerializedProperty _languageCodesProperty;
        private SerializedProperty _translationsUrlProperty;

        private void OnEnable()
        {
            _fallbackLanguageProperty = serializedObject.FindProperty(nameof(LocalizationSetup.fallbackLanguage));
            _languageCodesProperty = serializedObject.FindProperty(nameof(LocalizationSetup.languageCodes));
            _translationsUrlProperty = serializedObject.FindProperty("translationsUrl");

            _languageCodesList = new ReorderableList(
                serializedObject,
                _languageCodesProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

            _languageCodesList.drawHeaderCallback = rect =>
            {
                const float spacing = 6f;
                float totalWidth = rect.width - spacing;
                float languageWidth = totalWidth * 0.7f;
                float codeWidth = totalWidth * 0.3f;

                var languageRect = new Rect(rect.x, rect.y, languageWidth, EditorGUIUtility.singleLineHeight);
                var codeRect = new Rect(languageRect.xMax + spacing, rect.y, codeWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(languageRect, "Language");
                EditorGUI.LabelField(codeRect, "Code");
            };

            _languageCodesList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = _languageCodesProperty.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;

                EditorGUI.PropertyField(rect, element, GUIContent.none, includeChildren: true);
            };

            _languageCodesList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;

            _languageCodesList.onAddCallback = _ =>
            {
                int newIndex = _languageCodesProperty.arraySize;
                _languageCodesProperty.InsertArrayElementAtIndex(newIndex);

                var element = _languageCodesProperty.GetArrayElementAtIndex(newIndex);
                element.FindPropertyRelative(nameof(LanguageCodeEntry.language)).enumValueIndex = 0;
                element.FindPropertyRelative(nameof(LanguageCodeEntry.code)).stringValue = string.Empty;

                serializedObject.ApplyModifiedProperties();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTranslationsUrlSection();
            EditorGUILayout.Space(8f);

            EditorHelper.CenteredLabel(nameof(LanguageMapper).GetInspectorDisplayName());
            EditorGUILayout.PropertyField(_fallbackLanguageProperty);
            EditorGUILayout.Space(8f);
            _languageCodesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTranslationsUrlSection()
        {
            if (_translationsUrlProperty == null)
                return;

            var hasValue = !string.IsNullOrWhiteSpace(_translationsUrlProperty.stringValue);
            var isValid = IsValidUrl(_translationsUrlProperty.stringValue);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField(new GUIContent(
                    "Handy URL", EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image),
                EditorHelper.GetCenteredStyle(true),
                GUILayout.ExpandWidth(true));
            
            EditorGUILayout.Space(2f);

            var previousColor = GUI.color;

            if (hasValue)
            {
                GUI.color = isValid
                    ? new Color(0.85f, 1f, 0.85f)
                    : new Color(1f, 0.85f, 0.85f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_translationsUrlProperty, GUIContent.none);

                GUI.color = previousColor;

                using (new EditorGUI.DisabledScope(!hasValue || !isValid))
                {
                    if (GUILayout.Button("Open", GUILayout.Width(60f)))
                    {
                        Application.OpenURL(_translationsUrlProperty.stringValue);
                    }
                }
            }

            GUI.color = previousColor;

            if (hasValue && !isValid)
            {
                EditorGUILayout.HelpBox("Enter a valid http or https URL.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private static bool IsValidUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Uri.TryCreate(value, UriKind.Absolute, out Uri uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}