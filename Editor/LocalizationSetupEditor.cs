#if UNITY_EDITOR
using System;
using System.IO;
using Commons.Editor.Helpers;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MVVM.Localization.Editor
{
    [CustomEditor(typeof(LocalizationSetup))]
    internal class LocalizationSetupEditor : UnityEditor.Editor
    {
        private const string PREFS_SYNC_SECTION_EXPANDED = "MVVM.Localization.LocalizationSetupEditor.SyncSectionExpanded";

        private static readonly Color VALID_COLOR = new(0.75f, 1f, 0.75f, 1f);
        private static readonly Color INVALID_COLOR = new(1f, 0.75f, 0.75f, 1f);
        private static readonly Color DEFAULT_COLOR = Color.white;
        private static readonly Color SYNC_BUTTON_COLOR = new(0.35f, 0.75f, 0.35f, 1f);

        private ReorderableList _languageCodesList;

        private SerializedProperty _fallbackLanguageProperty;
        private SerializedProperty _languageCodesProperty;
        private SerializedProperty _catalogProperty;
        private SerializedProperty _preferLocalInEditorProperty;
        private SerializedProperty _useLocalAsRuntimeFallbackProperty;
        private SerializedProperty _spreadsheetUrlProperty;
        private SerializedProperty _manifestUrlProperty;
        private SerializedProperty _targetRootFolderProperty;
        private SerializedProperty _catalogAssetPathProperty;
        private SerializedProperty _addressablesGroupNameProperty;

        private bool _syncSectionExpanded;

        private void OnEnable()
        {
            _fallbackLanguageProperty = serializedObject.FindProperty(nameof(LocalizationSetup.fallbackLanguage));
            _languageCodesProperty = serializedObject.FindProperty(nameof(LocalizationSetup.languageCodes));
            _catalogProperty = serializedObject.FindProperty("catalog");
            _preferLocalInEditorProperty = serializedObject.FindProperty("preferLocalInEditor");
            _useLocalAsRuntimeFallbackProperty = serializedObject.FindProperty("useLocalAsRuntimeFallback");
            _spreadsheetUrlProperty = serializedObject.FindProperty("spreadsheetUrl");
            _manifestUrlProperty = serializedObject.FindProperty("manifestUrl");
            _targetRootFolderProperty = serializedObject.FindProperty("targetRootFolder");
            _catalogAssetPathProperty = serializedObject.FindProperty("catalogAssetPath");
            _addressablesGroupNameProperty = serializedObject.FindProperty("addressablesGroupName");

            _syncSectionExpanded = EditorPrefs.GetBool(PREFS_SYNC_SECTION_EXPANDED, true);

            _languageCodesList = new ReorderableList(serializedObject, _languageCodesProperty, true, true, true, true);
            _languageCodesList.drawHeaderCallback = DrawHeader;
            _languageCodesList.drawElementCallback = DrawElement;
            _languageCodesList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSyncSection();
            EditorGUILayout.Space(10f);

            EditorHelper.CenteredLabel(nameof(LanguageMapper).GetInspectorDisplayName());
            EditorGUILayout.PropertyField(_fallbackLanguageProperty);
            EditorGUILayout.PropertyField(_catalogProperty);
            EditorGUILayout.PropertyField(_preferLocalInEditorProperty);
            EditorGUILayout.PropertyField(_useLocalAsRuntimeFallbackProperty);
            EditorGUILayout.Space(8f);
            _languageCodesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSyncSection()
        {
            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                var foldoutStyle = new GUIStyle(EditorStyles.foldoutHeader)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };

                var nextExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                    _syncSectionExpanded,
                    "Google Sheets Sync Setup",
                    foldoutStyle);

                if (nextExpanded != _syncSectionExpanded)
                {
                    _syncSectionExpanded = nextExpanded;
                    EditorPrefs.SetBool(PREFS_SYNC_SECTION_EXPANDED, _syncSectionExpanded);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            EditorGUILayout.Space(4f);

            if (_syncSectionExpanded)
            {
                DrawColoredUrlProperty(_spreadsheetUrlProperty, "Spreadsheet Url");
                DrawColoredUrlProperty(_manifestUrlProperty, "Manifest Url");

                DrawPathFieldWithButton(
                    _targetRootFolderProperty,
                    "Target Root Folder",
                    "Select Target Root Folder",
                    PickFolderPath);

                DrawPathFieldWithButton(
                    _catalogAssetPathProperty,
                    "Catalog Asset Path",
                    "Select Catalog Asset",
                    PickAssetPath);

                EditorGUILayout.PropertyField(_addressablesGroupNameProperty);
                EditorGUILayout.Space(10f);
            }

            DrawSyncButtons();
            
            if (_syncSectionExpanded)
            {
                EditorGUILayout.HelpBox(
                    "Sync downloads a JSON manifest from Apps Script endpoint, creates or updates one ScriptableObject per tab, deletes stale assets, refreshes the catalog, and updates Addressables when the package is installed.",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSyncButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!IsValidHttpUrl(_spreadsheetUrlProperty.stringValue)))
                {
                    if (GUILayout.Button(
                            GetIconContent(
                                "UnityEditor.InspectorWindow",
                                "d_UnityEditor.InspectorWindow",
                                " Open sheet",
                                "Open spreadsheet in browser"),
                            GUILayout.Height(24f)))
                    {
                        Application.OpenURL(_spreadsheetUrlProperty.stringValue);
                    }
                }

                using (new EditorGUI.DisabledScope(!IsValidHttpUrl(_manifestUrlProperty.stringValue)))
                {
                    if (GUILayout.Button(
                            GetIconContent(
                                "_Help",
                                "_Help",
                                " Open manifest",
                                "Open manifest in browser"),
                            GUILayout.Height(24f)))
                    {
                        Application.OpenURL(_manifestUrlProperty.stringValue);
                    }
                }
            }

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = SYNC_BUTTON_COLOR;

            using (new EditorGUI.DisabledScope(!CanSync()))
            {
                if (GUILayout.Button(
                        GetIconContent(
                            "Refresh",
                            "d_Refresh",
                            " Sync now",
                            "Download the manifest and synchronize localization assets."),
                        GUILayout.Height(32f),
                        GUILayout.ExpandWidth(true)))
                {
                    serializedObject.ApplyModifiedProperties();
                    LocalizationSheetImporter.Sync((LocalizationSetup)target);
                }
            }

            GUI.backgroundColor = previousColor;
        }

        private static GUIContent GetIconContent(string lightIcon, string darkIcon, string text, string tooltip = null)
        {
            var iconName = EditorGUIUtility.isProSkin ? darkIcon : lightIcon;
            var icon = EditorGUIUtility.IconContent(iconName).image;
            return new GUIContent(text, icon, tooltip);
        }

        private bool CanSync()
        {
            return IsValidHttpUrl(_manifestUrlProperty.stringValue)
                   && !string.IsNullOrWhiteSpace(_targetRootFolderProperty.stringValue)
                   && !string.IsNullOrWhiteSpace(_catalogAssetPathProperty.stringValue);
        }

        private static void DrawHeader(Rect rect)
        {
            const float spacing = 6f;
            float totalWidth = rect.width - spacing;
            float languageWidth = totalWidth * 0.7f;
            float codeWidth = totalWidth * 0.3f;
            var languageRect = new Rect(rect.x, rect.y, languageWidth, EditorGUIUtility.singleLineHeight);
            var codeRect = new Rect(languageRect.xMax + spacing, rect.y, codeWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(languageRect, "Language");
            EditorGUI.LabelField(codeRect, "Code");
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _languageCodesProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, element, GUIContent.none, true);
        }

        private static bool IsValidHttpUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static void DrawColoredUrlProperty(SerializedProperty property, string label)
        {
            var previousColor = GUI.backgroundColor;

            GUI.backgroundColor = string.IsNullOrWhiteSpace(property.stringValue)
                ? DEFAULT_COLOR
                : IsValidHttpUrl(property.stringValue)
                    ? VALID_COLOR
                    : INVALID_COLOR;

            EditorGUILayout.PropertyField(property, new GUIContent(label));
            GUI.backgroundColor = previousColor;
        }

        private static void DrawPathFieldWithButton(
            SerializedProperty property,
            string label,
            string buttonLabel,
            Func<string, string> picker)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));

                if (GUILayout.Button(buttonLabel, GUILayout.Width(150f)))
                {
                    var selectedPath = picker(property.stringValue);
                    if (!string.IsNullOrWhiteSpace(selectedPath))
                    {
                        property.stringValue = selectedPath;
                    }
                }
            }
        }

        private static string PickFolderPath(string currentUnityPath)
        {
            var absoluteStartPath = GetAbsoluteFolderStartPath(currentUnityPath);
            var selectedAbsolutePath = EditorUtility.OpenFolderPanel(
                "Select Target Root Folder",
                absoluteStartPath,
                string.Empty);

            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
                return null;

            var dataPath = Application.dataPath.Replace('\\', '/');
            selectedAbsolutePath = selectedAbsolutePath.Replace('\\', '/');

            if (!selectedAbsolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Please select a folder inside this Unity project's Assets folder.",
                    "OK");
                return null;
            }

            return "Assets" + selectedAbsolutePath[dataPath.Length..];
        }

        private static string PickAssetPath(string currentAssetPath)
        {
            var absoluteStartPath = GetAbsoluteFileStartPath(currentAssetPath);
            var selectedAbsolutePath = EditorUtility.OpenFilePanel(
                "Select Catalog Asset",
                absoluteStartPath,
                "asset");

            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
                return null;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName?.Replace('\\', '/');
            selectedAbsolutePath = selectedAbsolutePath.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(projectRoot) ||
                !selectedAbsolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    "Invalid Asset",
                    "Please select an asset inside this Unity project.",
                    "OK");
                return null;
            }

            return selectedAbsolutePath[(projectRoot.Length + 1)..];
        }

        private static string GetAbsoluteFolderStartPath(string unityPath)
        {
            if (string.IsNullOrWhiteSpace(unityPath))
                return Application.dataPath;

            if (!unityPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                return Application.dataPath;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Application.dataPath;

            var combined = Path.Combine(projectRoot, unityPath).Replace('\\', '/');
            return Directory.Exists(combined) ? combined : Application.dataPath;
        }

        private static string GetAbsoluteFileStartPath(string unityPath)
        {
            if (string.IsNullOrWhiteSpace(unityPath))
                return Application.dataPath;

            if (!unityPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                return Application.dataPath;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(projectRoot))
                return Application.dataPath;

            var combined = Path.Combine(projectRoot, unityPath).Replace('\\', '/');
            if (File.Exists(combined))
                return Path.GetDirectoryName(combined)?.Replace('\\', '/') ?? Application.dataPath;

            return Application.dataPath;
        }
    }
}
#endif