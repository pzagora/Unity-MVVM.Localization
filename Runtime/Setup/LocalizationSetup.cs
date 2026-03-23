using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MVVM.Localization
{
    [CreateAssetMenu(menuName = "Scriptable Objects/MVVM/Localization/Setup")]
    public class LocalizationSetup : ScriptableObject
    {
        public Language fallbackLanguage = Language.English;
        public List<LanguageCodeEntry> languageCodes = new();

        [SerializeField] private LocalizationCatalogAsset catalog;
        [SerializeField] private bool preferLocalInEditor = true;
        [SerializeField] private bool useLocalAsRuntimeFallback = true;

        [SerializeField] private bool failOnDuplicateKeys = true;
        [SerializeField] private bool failOnUnknownLanguageCode = true;
        [SerializeField] private bool failOnEmptyFallbackValue = true;

#if UNITY_EDITOR
        [Header("Multi-source Google Sheets Import")]
        [SerializeField] private List<LocalizationImportSource> importSources = new();
        [SerializeField] private string targetRootFolder;
        [SerializeField] private string catalogAssetPath;
        [SerializeField] private string addressablesGroupName = "Localization";
#endif

        public LocalizationCatalogAsset Catalog => catalog;
        public bool PreferLocalInEditor => preferLocalInEditor;
        public bool UseLocalAsRuntimeFallback => useLocalAsRuntimeFallback;
        public bool FailOnDuplicateKeys => failOnDuplicateKeys;
        public bool FailOnUnknownLanguageCode => failOnUnknownLanguageCode;
        public bool FailOnEmptyFallbackValue => failOnEmptyFallbackValue;

#if UNITY_EDITOR
        public IReadOnlyList<LocalizationImportSource> ImportSources => importSources;
        public string TargetRootFolder => targetRootFolder;
        public string CatalogAssetPath => catalogAssetPath;
        public string AddressablesGroupName => addressablesGroupName;

        public List<LocalizationImportSource> GetEffectiveImportSources()
        {
            var normalized = new List<LocalizationImportSource>();
            if (importSources == null)
                return normalized;

            foreach (var source in importSources)
            {
                if (source == null || !source.enabled || string.IsNullOrWhiteSpace(source.manifestUrl))
                    continue;

                normalized.Add(new LocalizationImportSource
                {
                    sourceId = string.IsNullOrWhiteSpace(source.sourceId) ? "default" : source.sourceId.Trim(),
                    spreadsheetUrl = source.spreadsheetUrl?.Trim(),
                    manifestUrl = source.manifestUrl?.Trim(),
                    targetRootFolder = source.targetRootFolder?.Trim(),
                    enabled = source.enabled,
                    priority = source.priority,
                    convertToAddressables = source.convertToAddressables,
                    allowOverrideExistingKeys = source.allowOverrideExistingKeys,
                    prefixSourceIdToKeys = source.prefixSourceIdToKeys
                });
            }

            return normalized;
        }

        public bool HasConfiguredImportSources()
            => importSources != null && importSources.Any(x => x != null);
#endif
    }
}
