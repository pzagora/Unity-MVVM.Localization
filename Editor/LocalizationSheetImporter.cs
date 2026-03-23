#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

#if MVVM_LOCALIZATION_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

namespace MVVM.Localization.Editor
{
    internal static class LocalizationSheetImporter
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public static void Sync(LocalizationSetup setup, bool forceRebuild)
        {
            if (setup == null)
                throw new ArgumentNullException(nameof(setup));

            var sources = setup.GetEffectiveImportSources();
            if (sources.Count == 0)
                throw new InvalidOperationException("No enabled localization import sources are configured.");

            var sourceResults = new List<SourceManifestResult>(sources.Count);
            foreach (var source in sources)
            {
                var manifest = DownloadManifest(source.manifestUrl);
                sourceResults.Add(new SourceManifestResult(source, manifest));
            }

            var report = LocalizationManifestValidator.Validate(setup, sourceResults);
            if (report.HasErrors || report.HasWarnings)
                report.LogToUnityConsole();
            report.ThrowIfErrors();

            var orderedResults = sourceResults
                .OrderByDescending(x => x.Source.priority)
                .ThenBy(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var assetRecords = new List<ImportedAssetRecord>();
            var isMultiSource = orderedResults.Count > 1;

            if (forceRebuild)
            {
                RebuildTranslationTrees(setup, orderedResults, isMultiSource);
                AssetDatabase.Refresh();
            }

            foreach (var result in orderedResults)
            {
                var importedAssets = UpsertAssets(setup, result.Source, result.Manifest, isMultiSource);
                assetRecords.AddRange(importedAssets);
            }

            RefreshCatalog(setup, assetRecords);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var summary = $"Localization sync complete. Sources: {orderedResults.Count}, tables: {assetRecords.Count}.";
            if (report.HasWarnings)
                Debug.LogWarning(summary + " Completed with validation warnings. See console for details.");
            else
                Debug.Log(summary);
        }

        private static GoogleSheetManifest DownloadManifest(string manifestUrl)
        {
            if (string.IsNullOrWhiteSpace(manifestUrl))
                throw new InvalidOperationException("Manifest URL is empty.");

            using var response = HttpClient.GetAsync(manifestUrl).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var manifest = JsonUtility.FromJson<GoogleSheetManifest>(json);
            if (manifest == null)
                throw new InvalidOperationException($"Manifest endpoint returned invalid JSON: {manifestUrl}");

            manifest.tabs ??= new List<GoogleSheetManifestTab>();
            foreach (var tab in manifest.tabs)
            {
                tab.headers ??= new List<string>();
                tab.rows ??= new List<GoogleSheetManifestRow>();
                foreach (var row in tab.rows)
                    row.values ??= new List<string>();
            }

            return manifest;
        }

        private static List<ImportedAssetRecord> UpsertAssets(
            LocalizationSetup setup,
            LocalizationImportSource source,
            GoogleSheetManifest manifest,
            bool isMultiSource)
        {
            var rootFolder = ResolveTargetRootFolder(setup, source, isMultiSource);
            EnsureAssetFolderExists(rootFolder);
            var imported = new List<ImportedAssetRecord>();

            foreach (var tab in manifest.tabs)
            {
                if (string.IsNullOrWhiteSpace(tab.title))
                    continue;

                var tableId = GetTableId(tab);
                var assetPath = BuildAssetPath(rootFolder, tableId);
                var directory = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrWhiteSpace(directory))
                    EnsureAssetFolderExists(directory);

                var asset = AssetDatabase.LoadAssetAtPath<LocalizationTableAsset>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<LocalizationTableAsset>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }

                asset.EditorSetMetadata(tab.checksum, source.sourceId, tableId, tab.title);
                asset.EditorSetEntries(BuildEntries(source, tab));
                EditorUtility.SetDirty(asset);

#if MVVM_LOCALIZATION_ADDRESSABLES
                if (source.convertToAddressables)
                    EnsureAddressable(setup.AddressablesGroupName, assetPath, BuildAddressKey(source, tableId));
#endif

                imported.Add(new ImportedAssetRecord(source, asset));
            }

            return imported;
        }

        private static void RebuildTranslationTrees(
            LocalizationSetup setup,
            IEnumerable<SourceManifestResult> results,
            bool isMultiSource)
        {
            var deletedRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in results)
            {
                var rootFolder = ResolveTargetRootFolder(setup, result.Source, isMultiSource);
                if (string.IsNullOrWhiteSpace(rootFolder))
                    continue;

                rootFolder = rootFolder.Replace('\\', '/');
                if (!deletedRoots.Add(rootFolder))
                    continue;

                if (!AssetDatabase.IsValidFolder(rootFolder))
                    continue;

                AssetDatabase.DeleteAsset(rootFolder);
            }
        }

        private static void RefreshCatalog(LocalizationSetup setup, List<ImportedAssetRecord> records)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalogAsset>(setup.CatalogAssetPath);
            if (catalog == null)
            {
                var directory = Path.GetDirectoryName(setup.CatalogAssetPath);
                if (!string.IsNullOrWhiteSpace(directory)) 
                    EnsureAssetFolderExists(directory);

                catalog = ScriptableObject.CreateInstance<LocalizationCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, setup.CatalogAssetPath);
            }

            var orderedAssets = records
                .OrderByDescending(x => x.Source.priority)
                .ThenBy(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Asset.TableId, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Asset)
                .ToList();

            catalog.EditorSetLocalTables(orderedAssets);
#if MVVM_LOCALIZATION_ADDRESSABLES
            catalog.EditorSetAddressableKeys(records
                .Where(x => x.Source.convertToAddressables)
                .OrderByDescending(x => x.Source.priority)
                .ThenBy(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Asset.TableId, StringComparer.OrdinalIgnoreCase)
                .Select(x => BuildAddressKey(x.Source, x.Asset.TableId))
                .ToList());
#endif
            EditorUtility.SetDirty(catalog);

            var setupObject = new SerializedObject(setup);
            setupObject.FindProperty("catalog").objectReferenceValue = catalog;
            setupObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
        }

        private static List<LocalizationTableEntry> BuildEntries(LocalizationImportSource source, GoogleSheetManifestTab tab)
        {
            var entries = new List<LocalizationTableEntry>();
            if (tab.headers == null || tab.headers.Count < 2)
                return entries;

            var tableId = GetTableId(tab);
            var languages = tab.headers.Skip(1).Select(LocalizationKeyUtility.NormalizeLanguageCode).ToList();
            foreach (var row in tab.rows)
            {
                var rawKey = LocalizationKeyUtility.NormalizeKey(row?.key);
                if (string.IsNullOrWhiteSpace(rawKey))
                    continue;

                var entry = new LocalizationTableEntry
                {
                    id = LocalizationKeyUtility.BuildTranslationId(source.sourceId, tableId, rawKey, source.prefixSourceIdToKeys)
                };

                for (var i = 0; i < languages.Count; i++)
                {
                    entry.values.Add(new LocalizationTableValue
                    {
                        languageCode = languages[i],
                        text = row.values.Count > i ? row.values[i] ?? string.Empty : string.Empty
                    });
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static string ResolveTargetRootFolder(LocalizationSetup setup, LocalizationImportSource source, bool isMultiSource)
        {
            if (!string.IsNullOrWhiteSpace(source.targetRootFolder))
                return source.targetRootFolder.Replace('\\', '/');

            var baseRoot = string.IsNullOrWhiteSpace(setup.TargetRootFolder)
                ? "Assets/Localization/Imported"
                : setup.TargetRootFolder.Replace('\\', '/');

            if (!isMultiSource)
                return baseRoot;

            return $"{baseRoot}/{LocalizationKeyUtility.SanitizeAssetPathSegment(LocalizationKeyUtility.NormalizeSourceId(source.sourceId))}";
        }

        private static string BuildAssetPath(string root, string tableId)
        {
            var segments = LocalizationKeyUtility.NormalizeTableId(tableId)
                .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(LocalizationKeyUtility.SanitizeAssetPathSegment)
                .ToArray();

            if (segments.Length == 0)
                segments = new[] { "table" };

            var folder = segments.Length == 1 ? root : Path.Combine(new[] { root }.Concat(segments.Take(segments.Length - 1)).ToArray());
            var assetName = segments[^1] + ".asset";
            return Path.Combine(folder, assetName).Replace('\\', '/');
        }

        private static string BuildAddressKey(LocalizationImportSource source, string tableId)
            => $"localization/{LocalizationKeyUtility.NormalizeSourceId(source.sourceId)}/{LocalizationKeyUtility.NormalizeTableId(tableId)}";

        private static string GetTableId(GoogleSheetManifestTab tab)
            => LocalizationKeyUtility.NormalizeTableId(string.IsNullOrWhiteSpace(tab.tableId) ? tab.title : tab.tableId);
        
        private static void EnsureAssetFolderExists(string assetFolderPath)
        {
            if (string.IsNullOrWhiteSpace(assetFolderPath) || AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            var normalized = assetFolderPath.Replace('\\', '/');
            var parts = normalized.Split('/');

            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException($"Asset folder must be under Assets: {assetFolderPath}");

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

#if MVVM_LOCALIZATION_ADDRESSABLES
        private static void EnsureAddressable(string groupName, string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;

            var group = settings.FindGroup(groupName) ?? settings.CreateGroup(groupName, false, false, false, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(groupName, true);
        }
#endif

        private readonly struct ImportedAssetRecord
        {
            public ImportedAssetRecord(LocalizationImportSource source, LocalizationTableAsset asset)
            {
                Source = source;
                Asset = asset;
            }

            public LocalizationImportSource Source { get; }
            public LocalizationTableAsset Asset { get; }
        }
    }
}
#endif
