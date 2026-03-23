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
    internal sealed class LocalizationSetupEditor : UnityEditor.Editor
    {
        private const string PREFS_SYNC_SECTION_EXPANDED = "MVVM.Localization.LocalizationSetupEditor.SyncSectionExpanded";
        private const string PREFS_RUNTIME_SECTION_EXPANDED = "MVVM.Localization.LocalizationSetupEditor.RuntimeSectionExpanded";
        private const string PREFS_VALIDATION_SECTION_EXPANDED = "MVVM.Localization.LocalizationSetupEditor.ValidationSectionExpanded";
        private const string PREFS_MAPPER_SECTION_EXPANDED = "MVVM.Localization.LocalizationSetupEditor.MapperSectionExpanded";

        private static readonly Color ValidColor = new(0.75f, 1f, 0.75f, 1f);
        private static readonly Color InvalidColor = new(1f, 0.75f, 0.75f, 1f);
        private static readonly Color DefaultColor = Color.white;
        private static readonly Color SyncButtonColor = new(0.35f, 0.75f, 0.35f, 1f);
        private static readonly Color DangerColor = new(0.95f, 0.50f, 0.50f, 1f);

        private ReorderableList _languageCodesList;

        private SerializedProperty _fallbackLanguageProperty;
        private SerializedProperty _languageCodesProperty;
        private SerializedProperty _catalogProperty;
        private SerializedProperty _preferLocalInEditorProperty;
        private SerializedProperty _useLocalAsRuntimeFallbackProperty;
        private SerializedProperty _failOnDuplicateKeysProperty;
        private SerializedProperty _failOnUnknownLanguageCodeProperty;
        private SerializedProperty _failOnEmptyFallbackValueProperty;
        private SerializedProperty _importSourcesProperty;
        private SerializedProperty _targetRootFolderProperty;
        private SerializedProperty _catalogAssetPathProperty;
        private SerializedProperty _addressablesGroupNameProperty;

        private bool _syncSectionExpanded;
        private bool _runtimeSectionExpanded;
        private bool _validationSectionExpanded;
        private bool _mapperSectionExpanded;

        private void OnEnable()
        {
            _fallbackLanguageProperty = serializedObject.FindProperty(nameof(LocalizationSetup.fallbackLanguage));
            _languageCodesProperty = serializedObject.FindProperty(nameof(LocalizationSetup.languageCodes));
            _catalogProperty = serializedObject.FindProperty("catalog");
            _preferLocalInEditorProperty = serializedObject.FindProperty("preferLocalInEditor");
            _useLocalAsRuntimeFallbackProperty = serializedObject.FindProperty("useLocalAsRuntimeFallback");
            _failOnDuplicateKeysProperty = serializedObject.FindProperty("failOnDuplicateKeys");
            _failOnUnknownLanguageCodeProperty = serializedObject.FindProperty("failOnUnknownLanguageCode");
            _failOnEmptyFallbackValueProperty = serializedObject.FindProperty("failOnEmptyFallbackValue");
            _importSourcesProperty = serializedObject.FindProperty("importSources");
            _targetRootFolderProperty = serializedObject.FindProperty("targetRootFolder");
            _catalogAssetPathProperty = serializedObject.FindProperty("catalogAssetPath");
            _addressablesGroupNameProperty = serializedObject.FindProperty("addressablesGroupName");

            _syncSectionExpanded = EditorPrefs.GetBool(PREFS_SYNC_SECTION_EXPANDED, true);
            _runtimeSectionExpanded = EditorPrefs.GetBool(PREFS_RUNTIME_SECTION_EXPANDED, true);
            _validationSectionExpanded = EditorPrefs.GetBool(PREFS_VALIDATION_SECTION_EXPANDED, true);
            _mapperSectionExpanded = EditorPrefs.GetBool(PREFS_MAPPER_SECTION_EXPANDED, true);

            _languageCodesList = new ReorderableList(serializedObject, _languageCodesProperty, true, true, true, true);
            _languageCodesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Language Codes");
            _languageCodesList.drawElementCallback = DrawLanguageCodeElement;
            _languageCodesList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSyncToolbar();
            EditorGUILayout.Space(8f);

            DrawSyncSection();
            EditorGUILayout.Space(8f);
            DrawValidationSection();
            EditorGUILayout.Space(8f);
            DrawRuntimeSection();
            EditorGUILayout.Space(8f);
            DrawMapperSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSyncToolbar()
        {
            var setup = (LocalizationSetup)target;

            EditorGUILayout.BeginVertical("box");
            EditorHelper.CenteredLabel("Localization Sync");

            using (new EditorGUI.DisabledScope(!CanSync(setup)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var previousColor = GUI.backgroundColor;

                    GUI.backgroundColor = SyncButtonColor;
                    if (GUILayout.Button(
                            GetIconContent("Refresh", "d_Refresh", " Sync",
                                "Download configured manifests and synchronize localization assets."),
                            GUILayout.Height(36f)))
                    {
                        serializedObject.ApplyModifiedProperties();
                        LocalizationSheetImporter.Sync(setup, forceRebuild: false);
                    }

                    GUI.backgroundColor = DangerColor;
                    if (GUILayout.Button(
                            GetIconContent("Refresh", "d_Refresh", " Rebuild & Sync",
                                "Delete each source output folder and rebuild imported localization assets from scratch."),
                            GUILayout.Height(36f)))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Rebuild Localization",
                                "This will delete imported localization assets and rebuild them from scratch.\n\nContinue?",
                                "Rebuild",
                                "Cancel"))
                        {
                            serializedObject.ApplyModifiedProperties();
                            LocalizationSheetImporter.Sync(setup, forceRebuild: true);
                        }
                    }

                    GUI.backgroundColor = previousColor;
                }
            }

            EditorGUILayout.Space(4f);

            if (!CanSync(setup))
            {
                EditorGUILayout.HelpBox(
                    "Sync requires a Target Root Folder, Catalog Asset Path, and at least one enabled import source.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSyncSection()
        {
            DrawFoldoutSection(ref _syncSectionExpanded, PREFS_SYNC_SECTION_EXPANDED, "Google Sheets Sync", () =>
            {
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
                EditorGUILayout.Space(4f);
                
                DrawImportSources();
            });
        }

        private void DrawRuntimeSection()
        {
            DrawFoldoutSection(ref _runtimeSectionExpanded, PREFS_RUNTIME_SECTION_EXPANDED, "Runtime", () =>
            {
                EditorGUILayout.PropertyField(_catalogProperty);
                EditorGUILayout.PropertyField(_preferLocalInEditorProperty);
                EditorGUILayout.PropertyField(_useLocalAsRuntimeFallbackProperty);
            });
        }

        private void DrawValidationSection()
        {
            DrawFoldoutSection(ref _validationSectionExpanded, PREFS_VALIDATION_SECTION_EXPANDED, "Validation", () =>
            {
                EditorGUILayout.PropertyField(_failOnDuplicateKeysProperty);
                EditorGUILayout.PropertyField(_failOnUnknownLanguageCodeProperty);
                EditorGUILayout.PropertyField(_failOnEmptyFallbackValueProperty);
            });
        }

        private void DrawMapperSection()
        {
            DrawFoldoutSection(ref _mapperSectionExpanded, PREFS_MAPPER_SECTION_EXPANDED, nameof(LanguageMapper).GetInspectorDisplayName(), () =>
            {
                EditorGUILayout.PropertyField(_fallbackLanguageProperty);
                EditorGUILayout.Space(6f);
                _languageCodesList.DoLayoutList();
            });
        }

        private void DrawImportSources()
        {
            EditorHelper.CenteredLabel("Import Sources");

            if (_importSourcesProperty == null)
            {
                EditorGUILayout.HelpBox("Import Sources property was not found. Replace LocalizationSetup.cs together with this editor file.", MessageType.Error);
                return;
            }

            if (_importSourcesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No sources configured. Add at least one manifest source below.", MessageType.Warning);
            }

            for (var i = 0; i < _importSourcesProperty.arraySize; i++)
            {
                var element = _importSourcesProperty.GetArrayElementAtIndex(i);
                DrawImportSourceCard(i, element);
                EditorGUILayout.Space(6f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent(" Add Source", EditorGUIUtility.IconContent("Toolbar Plus").image), GUILayout.Height(24f), GUILayout.Width(140f)))
                {
                    AddImportSource();
                }
            }
        }

        private void DrawImportSourceCard(int index, SerializedProperty element)
        {
            var sourceIdProperty = element.FindPropertyRelative("sourceId");
            var spreadsheetUrlProperty = element.FindPropertyRelative("spreadsheetUrl");
            var manifestUrlProperty = element.FindPropertyRelative("manifestUrl");
            var targetRootFolderProperty = element.FindPropertyRelative("targetRootFolder");
            var enabledProperty = element.FindPropertyRelative("enabled");
            var priorityProperty = element.FindPropertyRelative("priority");
            var convertToAddressablesProperty = element.FindPropertyRelative("convertToAddressables");
            var allowOverrideExistingKeysProperty = element.FindPropertyRelative("allowOverrideExistingKeys");
            var prefixSourceIdToKeysProperty = element.FindPropertyRelative("prefixSourceIdToKeys");

            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                var title = string.IsNullOrWhiteSpace(sourceIdProperty.stringValue)
                    ? $"Source {index + 1}"
                    : sourceIdProperty.stringValue.Trim();

                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                enabledProperty.boolValue = GUILayout.Toggle(enabledProperty.boolValue, new GUIContent("Enabled"), "Button", GUILayout.Width(72f));

                using (new EditorGUI.DisabledScope(index <= 0))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_scrollup"), GUILayout.Width(28f)))
                        _importSourcesProperty.MoveArrayElement(index, index - 1);
                }

                using (new EditorGUI.DisabledScope(index >= _importSourcesProperty.arraySize - 1))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_scrolldown"), GUILayout.Width(28f)))
                        _importSourcesProperty.MoveArrayElement(index, index + 1);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(28f)))
                {
                    _importSourcesProperty.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(sourceIdProperty, new GUIContent("Source Id"));
            EditorGUILayout.PropertyField(priorityProperty, new GUIContent("Priority"));
            EditorGUILayout.PropertyField(prefixSourceIdToKeysProperty, new GUIContent("Prefix Source Id To Keys"));
            EditorGUILayout.PropertyField(convertToAddressablesProperty, new GUIContent("Convert To Addressables"));
            EditorGUILayout.PropertyField(allowOverrideExistingKeysProperty, new GUIContent("Allow Override Existing Keys"));

            DrawColoredUrlProperty(spreadsheetUrlProperty, "Spreadsheet Url");
            DrawColoredUrlProperty(manifestUrlProperty, "Manifest Url");

            DrawInlinePathFieldWithButton(
                targetRootFolderProperty,
                "Target Root Folder Override",
                "Select Target Root Folder",
                PickFolderPath);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!IsValidHttpUrl(spreadsheetUrlProperty.stringValue)))
                {
                    if (GUILayout.Button(GetIconContent("UnityEditor.InspectorWindow", "d_UnityEditor.InspectorWindow", " Open sheet"), GUILayout.Height(22f)))
                        Application.OpenURL(spreadsheetUrlProperty.stringValue);
                }

                using (new EditorGUI.DisabledScope(!IsValidHttpUrl(manifestUrlProperty.stringValue)))
                {
                    if (GUILayout.Button(GetIconContent("_Help", "_Help", " Open manifest"), GUILayout.Height(22f)))
                        Application.OpenURL(manifestUrlProperty.stringValue);
                }
            }

            if (!enabledProperty.boolValue)
            {
                EditorGUILayout.HelpBox("This source is disabled and will be ignored during sync.", MessageType.None);
            }
            else if (string.IsNullOrWhiteSpace(manifestUrlProperty.stringValue))
            {
                EditorGUILayout.HelpBox("Manifest Url is required for an enabled source.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLanguageCodeElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _languageCodesProperty.GetArrayElementAtIndex(index);
            var languageProperty = element.FindPropertyRelative(nameof(LanguageCodeEntry.language));
            var codeProperty = element.FindPropertyRelative(nameof(LanguageCodeEntry.code));

            rect.y += 2f;
            var halfWidth = rect.width * 0.48f;
            var gap = rect.width * 0.04f;

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, halfWidth, EditorGUIUtility.singleLineHeight), languageProperty, GUIContent.none);
            EditorGUI.PropertyField(new Rect(rect.x + halfWidth + gap, rect.y, halfWidth - gap, EditorGUIUtility.singleLineHeight), codeProperty, GUIContent.none);
        }

        private void AddImportSource()
        {
            var index = _importSourcesProperty.arraySize;
            _importSourcesProperty.InsertArrayElementAtIndex(index);
            var element = _importSourcesProperty.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("sourceId").stringValue = $"source_{index + 1}";
            element.FindPropertyRelative("spreadsheetUrl").stringValue = string.Empty;
            element.FindPropertyRelative("manifestUrl").stringValue = string.Empty;
            element.FindPropertyRelative("targetRootFolder").stringValue = string.Empty;
            element.FindPropertyRelative("enabled").boolValue = true;
            element.FindPropertyRelative("priority").intValue = 0;
            element.FindPropertyRelative("convertToAddressables").boolValue = true;
            element.FindPropertyRelative("allowOverrideExistingKeys").boolValue = false;
            element.FindPropertyRelative("prefixSourceIdToKeys").boolValue = true;
        }

        private static GUIContent GetIconContent(string lightIcon, string darkIcon, string text, string tooltip = null)
        {
            var iconName = EditorGUIUtility.isProSkin ? darkIcon : lightIcon;
            var icon = EditorGUIUtility.IconContent(iconName).image;
            return new GUIContent(text, icon, tooltip);
        }

        private static void DrawFoldoutSection(ref bool expanded, string prefsKey, string title, Action drawBody)
        {
            EditorGUILayout.BeginVertical("box");

            var style = new GUIStyle(EditorStyles.foldoutHeader)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            var nextExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title, style);
            if (nextExpanded != expanded)
            {
                expanded = nextExpanded;
                EditorPrefs.SetBool(prefsKey, expanded);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            if (expanded)
            {
                EditorGUILayout.Space(4f);
                drawBody?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawColoredUrlProperty(SerializedProperty property, string label)
        {
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = string.IsNullOrWhiteSpace(property.stringValue)
                ? DefaultColor
                : (IsValidHttpUrl(property.stringValue) ? ValidColor : InvalidColor);

            EditorGUILayout.PropertyField(property, new GUIContent(label));
            GUI.backgroundColor = previousColor;
        }

        private static void DrawPathFieldWithButton(SerializedProperty property, string label, string panelTitle, Func<string, string> picker)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
                if (GUILayout.Button("...", GUILayout.Width(32f)))
                {
                    var picked = picker(property.stringValue);
                    if (!string.IsNullOrWhiteSpace(picked))
                        property.stringValue = picked;
                }
            }
        }

        private static void DrawInlinePathFieldWithButton(SerializedProperty property, string label, string panelTitle, Func<string, string> picker)
        {
            DrawPathFieldWithButton(property, label, panelTitle, picker);
        }

        private static bool CanSync(LocalizationSetup setup)
        {
            if (setup == null || string.IsNullOrWhiteSpace(setup.CatalogAssetPath))
                return false;

            var sources = setup.GetEffectiveImportSources();
            return sources.Count > 0 && !string.IsNullOrWhiteSpace(setup.TargetRootFolder);
        }

        private static bool IsValidHttpUrl(string value)
            => Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static string PickFolderPath(string currentRelativePath)
        {
            var absoluteStartPath = GetAbsoluteFolderStartPath(currentRelativePath);
            var selectedAbsolutePath = EditorUtility.OpenFolderPanel("Select Target Root Folder", absoluteStartPath, string.Empty);
            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
                return null;

            var dataPath = Application.dataPath.Replace('\\', '/');
            selectedAbsolutePath = selectedAbsolutePath.Replace('\\', '/');

            if (!selectedAbsolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project's Assets folder.", "OK");
                return null;
            }

            return "Assets" + selectedAbsolutePath[dataPath.Length..];
        }

        private static string PickAssetPath(string currentAssetPath)
        {
            var absoluteStartPath = GetAbsoluteFileStartPath(currentAssetPath);
            var selectedAbsolutePath = EditorUtility.OpenFilePanel("Select Catalog Asset", absoluteStartPath, "asset");
            if (string.IsNullOrWhiteSpace(selectedAbsolutePath))
                return null;

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName?.Replace('\\', '/');
            selectedAbsolutePath = selectedAbsolutePath.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(projectRoot) || !selectedAbsolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Invalid Asset", "Please select an asset inside this Unity project.", "OK");
                return null;
            }

            return selectedAbsolutePath[(projectRoot.Length + 1)..];
        }

        private static string GetAbsoluteFolderStartPath(string currentRelativePath)
        {
            if (!string.IsNullOrWhiteSpace(currentRelativePath))
            {
                var full = Path.GetFullPath(currentRelativePath);
                if (Directory.Exists(full))
                    return full;
            }

            return Application.dataPath;
        }

        private static string GetAbsoluteFileStartPath(string currentAssetPath)
        {
            if (!string.IsNullOrWhiteSpace(currentAssetPath))
            {
                var full = Path.GetFullPath(currentAssetPath);
                var directory = Path.GetDirectoryName(full);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    return directory;
            }

            return Application.dataPath;
        }
    }
}
#endif
