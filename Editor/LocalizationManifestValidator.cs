#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace MVVM.Localization.Editor
{
    internal static class LocalizationManifestValidator
    {
        public static LocalizationImportReport Validate(LocalizationSetup setup, IReadOnlyList<SourceManifestResult> results)
        {
            var report = new LocalizationImportReport
            {
                sourceCount = results?.Count ?? 0,
                tableCount = results?.Sum(x => x.Manifest?.tabs?.Count ?? 0) ?? 0
            };

            if (setup == null)
            {
                report.AddError("LocalizationSetup is null.");
                return report;
            }

            if (results == null || results.Count == 0)
            {
                report.AddError("No enabled localization import sources are configured.");
                return report;
            }

            ValidateSetup(setup, results, report);

            foreach (var result in results)
                ValidateSource(setup, result, report);

            ValidateCrossSourceKeys(setup, results, report);
            return report;
        }

        private static void ValidateSetup(LocalizationSetup setup, IReadOnlyList<SourceManifestResult> results, LocalizationImportReport report)
        {
            if (string.IsNullOrWhiteSpace(setup.CatalogAssetPath))
                report.AddError("LocalizationSetup.CatalogAssetPath is empty.");

            var hasAnyRoot =
                !string.IsNullOrWhiteSpace(setup.TargetRootFolder) ||
                results.Any(x => !string.IsNullOrWhiteSpace(x.Source.targetRootFolder));

            if (!hasAnyRoot)
                report.AddError("LocalizationSetup.TargetRootFolder is empty and no source overrides provide a target root folder.");

            var sourceIds = results
                .Select(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var duplicateSourceIds = sourceIds
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            if (duplicateSourceIds.Count > 0)
                report.AddError($"Duplicate import source ids: {string.Join(", ", duplicateSourceIds)}");

            var fallbackCode = TryGetFallbackCode(setup);
            if (setup.FailOnEmptyFallbackValue && string.IsNullOrWhiteSpace(fallbackCode))
            {
                report.AddError(
                    $"Fallback language '{setup.fallbackLanguage}' does not have a configured language code in LocalizationSetup.languageCodes. " +
                    "FailOnEmptyFallbackValue cannot be enforced without a fallback column mapping.");
            }
        }

        private static void ValidateSource(LocalizationSetup setup, SourceManifestResult result, LocalizationImportReport report)
        {
            var source = result.Source;
            var manifest = result.Manifest;

            if (source == null)
            {
                report.AddError("A localization import source is null.");
                return;
            }

            if (manifest == null)
            {
                report.AddError($"Source '{source.sourceId}' returned a null manifest.");
                return;
            }

            if (manifest.tabs == null || manifest.tabs.Count == 0)
            {
                report.AddError($"Source '{source.sourceId}' does not contain any tabs.");
                return;
            }

            var duplicateTableIds = manifest.tabs
                .Select(GetTableId)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            if (duplicateTableIds.Count > 0)
                report.AddError($"Source '{source.sourceId}' contains duplicate table ids: {string.Join(", ", duplicateTableIds)}");

            var knownLanguageCodes = new HashSet<string>(
                setup.languageCodes
                    .Select(x => LocalizationKeyUtility.NormalizeLanguageCode(x.code))
                    .Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);

            var fallbackCode = TryGetFallbackCode(setup);
            var seenSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var duplicateSourceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tab in manifest.tabs)
            {
                ValidateTab(setup, source, tab, knownLanguageCodes, fallbackCode, seenSourceKeys, duplicateSourceKeys, report);
            }

            if (duplicateSourceKeys.Count > 0)
            {
                report.Add(
                    setup.FailOnDuplicateKeys,
                    $"Source '{source.sourceId}' contains duplicate final localization keys: {string.Join(", ", duplicateSourceKeys.OrderBy(x => x))}");
            }
        }

        private static void ValidateTab(
            LocalizationSetup setup,
            LocalizationImportSource source,
            GoogleSheetManifestTab tab,
            HashSet<string> knownLanguageCodes,
            string fallbackCode,
            HashSet<string> seenSourceKeys,
            HashSet<string> duplicateSourceKeys,
            LocalizationImportReport report)
        {
            if (tab == null)
            {
                report.AddError($"Source '{source.sourceId}' contains a null tab entry.");
                return;
            }

            if (string.IsNullOrWhiteSpace(tab.title))
            {
                report.AddError($"Source '{source.sourceId}' contains a tab with empty title.");
                return;
            }

            if (tab.headers == null || tab.headers.Count < 2)
            {
                report.AddError($"Source '{source.sourceId}', tab '{tab.title}' must contain at least a key column and one language column.");
                return;
            }

            var tableId = GetTableId(tab);
            var normalizedHeaders = tab.headers
                .Skip(1)
                .Select(LocalizationKeyUtility.NormalizeLanguageCode)
                .ToList();

            var duplicateHeaders = normalizedHeaders
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .OrderBy(x => x)
                .ToList();

            if (duplicateHeaders.Count > 0)
                report.AddError($"Source '{source.sourceId}', tab '{tab.title}' contains duplicate language headers: {string.Join(", ", duplicateHeaders)}");

            var blankHeaders = normalizedHeaders
                .Select((code, index) => new { code, index })
                .Where(x => string.IsNullOrWhiteSpace(x.code))
                .Select(x => x.index + 2)
                .ToList();

            if (blankHeaders.Count > 0)
                report.AddError($"Source '{source.sourceId}', tab '{tab.title}' contains blank language header cells at columns: {string.Join(", ", blankHeaders)}");

            var unknownHeaders = normalizedHeaders
                .Where(x => !string.IsNullOrWhiteSpace(x) && !knownLanguageCodes.Contains(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (unknownHeaders.Count > 0)
            {
                report.Add(
                    setup.FailOnUnknownLanguageCode,
                    $"Source '{source.sourceId}', tab '{tab.title}' contains unknown language codes: {string.Join(", ", unknownHeaders)}");
            }

            var fallbackIndex = -1;
            if (!string.IsNullOrWhiteSpace(fallbackCode))
                fallbackIndex = normalizedHeaders.FindIndex(x => string.Equals(x, fallbackCode, StringComparison.OrdinalIgnoreCase));

            if (setup.FailOnEmptyFallbackValue && !string.IsNullOrWhiteSpace(fallbackCode) && fallbackIndex < 0)
            {
                report.AddError(
                    $"Source '{source.sourceId}', tab '{tab.title}' is missing the fallback language column '{fallbackCode}' for fallback language '{setup.fallbackLanguage}'.");
            }

            tab.rows ??= new List<GoogleSheetManifestRow>();
            for (var rowIndex = 0; rowIndex < tab.rows.Count; rowIndex++)
            {
                var row = tab.rows[rowIndex] ?? new GoogleSheetManifestRow();
                row.values ??= new List<string>();

                var rawKey = LocalizationKeyUtility.NormalizeKey(row.key);
                if (string.IsNullOrWhiteSpace(rawKey))
                    continue;

                report.keyCount++;

                if (row.values.Count > normalizedHeaders.Count)
                {
                    report.AddWarning(
                        $"Source '{source.sourceId}', tab '{tab.title}', row {rowIndex + 2} key '{rawKey}' contains extra values beyond the declared language columns. Extra cells will be ignored.");
                }

                var finalKey = LocalizationKeyUtility.BuildTranslationId(source.sourceId, tableId, rawKey, source.prefixSourceIdToKeys);
                if (!seenSourceKeys.Add(finalKey))
                    duplicateSourceKeys.Add(finalKey);

                if (setup.FailOnEmptyFallbackValue && fallbackIndex >= 0)
                {
                    var fallbackValue = row.values.Count > fallbackIndex ? row.values[fallbackIndex] : string.Empty;
                    if (string.IsNullOrWhiteSpace(fallbackValue))
                    {
                        report.AddError(
                            $"Source '{source.sourceId}', tab '{tab.title}', row {rowIndex + 2} key '{finalKey}' has an empty fallback value for language '{fallbackCode}'.");
                    }
                }

                var emptyLanguages = new List<string>();
                for (var i = 0; i < normalizedHeaders.Count; i++)
                {
                    if (i == fallbackIndex)
                        continue;

                    var languageCode = normalizedHeaders[i];
                    if (string.IsNullOrWhiteSpace(languageCode))
                        continue;

                    var value = row.values.Count > i ? row.values[i] : string.Empty;
                    if (string.IsNullOrWhiteSpace(value))
                        emptyLanguages.Add(languageCode);
                }

                if (emptyLanguages.Count > 0)
                {
                    report.AddWarning(
                        $"Source '{source.sourceId}', tab '{tab.title}', row {rowIndex + 2} key '{finalKey}' has empty values for languages: {string.Join(", ", emptyLanguages)}");
                }
            }
        }

        private static void ValidateCrossSourceKeys(LocalizationSetup setup, IReadOnlyList<SourceManifestResult> results, LocalizationImportReport report)
        {
            var ownersByKey = new Dictionary<string, List<KeyOwner>>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in results)
            {
                foreach (var tab in result.Manifest.tabs)
                {
                    var tableId = GetTableId(tab);
                    foreach (var row in tab.rows)
                    {
                        var rawKey = LocalizationKeyUtility.NormalizeKey(row?.key);
                        if (string.IsNullOrWhiteSpace(rawKey))
                            continue;

                        var finalKey = LocalizationKeyUtility.BuildTranslationId(
                            result.Source.sourceId,
                            tableId,
                            rawKey,
                            result.Source.prefixSourceIdToKeys);

                        if (!ownersByKey.TryGetValue(finalKey, out var owners))
                        {
                            owners = new List<KeyOwner>();
                            ownersByKey[finalKey] = owners;
                        }

                        owners.Add(new KeyOwner(result.Source, tab.title, tableId));
                    }
                }
            }

            foreach (var pair in ownersByKey.Where(x => x.Value.Count > 1))
            {
                var distinctSourceIds = pair.Value
                    .Select(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctSourceIds.Count <= 1)
                    continue;

                var orderedOwners = pair.Value
                    .OrderByDescending(x => x.Source.priority)
                    .ThenBy(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var winner = orderedOwners[0];
                var topPriorityOwners = orderedOwners
                    .Where(x => x.Source.priority == winner.Source.priority)
                    .ToList();

                var distinctTopPrioritySourceIds = topPriorityOwners
                    .Select(x => LocalizationKeyUtility.NormalizeSourceId(x.Source.sourceId))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctTopPrioritySourceIds.Count > 1)
                {
                    report.AddError(
                        $"Localization key '{pair.Key}' is defined by multiple sources with the same priority: {string.Join(", ", distinctTopPrioritySourceIds)}. Increase one source priority or prefix keys by source.");
                    continue;
                }

                if (!winner.Source.allowOverrideExistingKeys)
                {
                    report.Add(
                        setup.FailOnDuplicateKeys,
                        $"Localization key '{pair.Key}' exists in multiple sources. Winning source '{winner.Source.sourceId}' must enable Allow Override Existing Keys, or use source-prefixed keys.");
                }
            }
        }

        private static string TryGetFallbackCode(LocalizationSetup setup)
        {
            if (setup?.languageCodes == null)
                return null;

            foreach (var entry in setup.languageCodes)
            {
                if (entry.language != setup.fallbackLanguage)
                    continue;

                var code = LocalizationKeyUtility.NormalizeLanguageCode(entry.code);
                if (!string.IsNullOrWhiteSpace(code))
                    return code;
            }

            return null;
        }

        private static string GetTableId(GoogleSheetManifestTab tab)
            => LocalizationKeyUtility.NormalizeTableId(string.IsNullOrWhiteSpace(tab?.tableId) ? tab?.title : tab.tableId);
    }

    internal readonly struct SourceManifestResult
    {
        public SourceManifestResult(LocalizationImportSource source, GoogleSheetManifest manifest)
        {
            Source = source;
            Manifest = manifest;
        }

        public LocalizationImportSource Source { get; }
        public GoogleSheetManifest Manifest { get; }
    }

    internal readonly struct KeyOwner
    {
        public KeyOwner(LocalizationImportSource source, string displayName, string tableId)
        {
            Source = source;
            DisplayName = displayName;
            TableId = tableId;
        }

        public LocalizationImportSource Source { get; }
        public string DisplayName { get; }
        public string TableId { get; }
    }
}
#endif
