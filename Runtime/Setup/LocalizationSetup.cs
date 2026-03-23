using System.Collections.Generic;
using UnityEngine;

namespace MVVM.Localization
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Localization/Setup")]
    public class LocalizationSetup : ScriptableObject
    {
        public Language fallbackLanguage = Language.English;
        public List<LanguageCodeEntry> languageCodes = new();

        [Header("Asset sources")]
        [SerializeField] private LocalizationCatalogAsset catalog;
        [SerializeField] private bool preferLocalInEditor = true;
        [SerializeField] private bool useLocalAsRuntimeFallback = true;

#if UNITY_EDITOR
        [SerializeField] private string spreadsheetUrl;
        [SerializeField] private string manifestUrl;
        [SerializeField] private string targetRootFolder;
        [SerializeField] private string catalogAssetPath;
        [SerializeField] private string addressablesGroupName = "Localization";
#endif

        public LocalizationCatalogAsset Catalog => catalog;
        public bool PreferLocalInEditor => preferLocalInEditor;
        public bool UseLocalAsRuntimeFallback => useLocalAsRuntimeFallback;

#if UNITY_EDITOR
        public string SpreadsheetUrl => spreadsheetUrl;
        public string ManifestUrl => manifestUrl;
        public string TargetRootFolder => targetRootFolder;
        public string CatalogAssetPath => catalogAssetPath;
        public string AddressablesGroupName => addressablesGroupName;
#endif
    }
}
