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
        private static readonly HttpClient HttpClient = new();

        public static void Sync(LocalizationSetup setup)
        {
            if (setup == null)
                throw new ArgumentNullException(nameof(setup));

            if (string.IsNullOrWhiteSpace(setup.ManifestUrl))
                throw new InvalidOperationException("LocalizationSetup.ManifestUrl is empty.");

            var manifest = DownloadManifest(setup.ManifestUrl);
            ValidateManifest(manifest);

            var assets = UpsertAssets(setup, manifest);
            DeleteMissingAssets(setup, assets);
            RefreshCatalog(setup, assets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Localization sync complete. Spreadsheet: {manifest.spreadsheetId}, tabs: {manifest.tabs.Count}, assets: {assets.Count}.");
        }

        private static GoogleSheetManifest DownloadManifest(string manifestUrl)
        {
            var json = HttpClient.GetStringAsync(manifestUrl).GetAwaiter().GetResult();
            var manifest = JsonUtility.FromJson<GoogleSheetManifest>(json);
            if (manifest == null)
                throw new InvalidOperationException("Manifest endpoint returned invalid JSON.");

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

        private static void ValidateManifest(GoogleSheetManifest manifest)
        {
            if (manifest.tabs == null || manifest.tabs.Count == 0)
                throw new InvalidOperationException("Manifest does not contain any tabs.");

            var duplicateTitles = manifest.tabs
                .Where(x => !string.IsNullOrWhiteSpace(x.title))
                .GroupBy(x => x.title.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateTitles.Count > 0)
                throw new InvalidOperationException($"Manifest contains duplicate tab titles: {string.Join(", ", duplicateTitles)}");
        }

        private static List<LocalizationTableAsset> UpsertAssets(LocalizationSetup setup, GoogleSheetManifest manifest)
        {
            Directory.CreateDirectory(setup.TargetRootFolder);
            var assets = new List<LocalizationTableAsset>();

            foreach (var tab in manifest.tabs)
            {
                if (string.IsNullOrWhiteSpace(tab.title))
                    continue;

                var assetPath = BuildAssetPath(setup.TargetRootFolder, tab.title);
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);

                var asset = AssetDatabase.LoadAssetAtPath<LocalizationTableAsset>(assetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<LocalizationTableAsset>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                }

                asset.EditorSetMetadata(manifest.spreadsheetId, tab.title, tab.gid, tab.checksum);
                asset.EditorSetEntries(BuildEntries(tab));
                EditorUtility.SetDirty(asset);
                assets.Add(asset);

#if MVVM_LOCALIZATION_ADDRESSABLES
                EnsureAddressable(setup.AddressablesGroupName, assetPath, BuildAddressKey(tab.title));
#endif
            }

            return assets;
        }

        private static void DeleteMissingAssets(LocalizationSetup setup, IReadOnlyCollection<LocalizationTableAsset> syncedAssets)
        {
            var expected = new HashSet<string>(syncedAssets.Select(AssetDatabase.GetAssetPath), StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:LocalizationTableAsset", new[] { setup.TargetRootFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (expected.Contains(path))
                    continue;

                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void RefreshCatalog(LocalizationSetup setup, List<LocalizationTableAsset> assets)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LocalizationCatalogAsset>(setup.CatalogAssetPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(setup.CatalogAssetPath)!);
                catalog = ScriptableObject.CreateInstance<LocalizationCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, setup.CatalogAssetPath);
            }

            catalog.EditorSetLocalTables(assets.OrderBy(x => x.SheetName).ToList());
#if MVVM_LOCALIZATION_ADDRESSABLES
            catalog.EditorSetAddressableKeys(assets.Select(x => BuildAddressKey(x.SheetName)).OrderBy(x => x).ToList());
#endif
            EditorUtility.SetDirty(catalog);

            var setupObject = new SerializedObject(setup);
            setupObject.FindProperty("catalog").objectReferenceValue = catalog;
            setupObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setup);
        }

        private static List<LocalizationTableEntry> BuildEntries(GoogleSheetManifestTab tab)
        {
            var entries = new List<LocalizationTableEntry>();
            if (tab.headers == null || tab.headers.Count < 2)
                return entries;

            var languages = tab.headers.Skip(1).Select(x => (x ?? string.Empty).Trim()).ToList();
            foreach (var row in tab.rows)
            {
                var rawKey = (row?.key ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(rawKey))
                    continue;

                var entry = new LocalizationTableEntry
                {
                    id = $"{tab.title.ToLowerInvariant()}.{rawKey}"
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

        private static string BuildAssetPath(string root, string sheetName)
        {
            var segments = sheetName.Split('.').Select(SanitizeSegment).ToArray();
            var folder = segments.Length == 1 ? root : Path.Combine(new[] { root }.Concat(segments.Take(segments.Length - 1)).ToArray());
            var assetName = segments[^1] + ".asset";
            return Path.Combine(folder, assetName).Replace('\\', '/');
        }

        private static string BuildAddressKey(string sheetName)
            => $"localization/{sheetName.Trim().ToLowerInvariant()}";

        private static string SanitizeSegment(string value)
            => string.Concat((value ?? string.Empty).Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

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
    }
}
#endif
