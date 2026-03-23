#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MVVM.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LanguageCodeEntry))]
    internal class LanguageCodeEntryDrawer : PropertyDrawer
    {
        private const float SPACING = 6f;
        private const float LANGUAGE_WIDTH_PERCENT = 0.7f;
        private const float CODE_WIDTH_PERCENT = 0.3f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var languageProperty = property.FindPropertyRelative(nameof(LanguageCodeEntry.language));
            var codeProperty = property.FindPropertyRelative(nameof(LanguageCodeEntry.code));

            var rowRect = new Rect(
                position.x,
                position.y + 2f,
                position.width,
                EditorGUIUtility.singleLineHeight);

            var totalWidth = rowRect.width - SPACING;
            var languageWidth = totalWidth * LANGUAGE_WIDTH_PERCENT;
            var codeWidth = totalWidth * CODE_WIDTH_PERCENT;

            var languageRect = new Rect(
                rowRect.x,
                rowRect.y,
                languageWidth,
                rowRect.height);

            var codeRect = new Rect(
                languageRect.xMax + SPACING,
                rowRect.y,
                codeWidth,
                rowRect.height);

            EditorGUI.PropertyField(languageRect, languageProperty, GUIContent.none);
            codeProperty.stringValue = EditorGUI.TextField(codeRect, codeProperty.stringValue);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + 4f;
        }
    }
}
#endif