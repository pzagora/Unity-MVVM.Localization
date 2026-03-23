using System;

namespace MVVM.Localization
{
    [Serializable]
    public sealed class LocalizationImportSource
    {
        public string sourceId = "default";
        public string spreadsheetUrl;
        public string manifestUrl;
        public string targetRootFolder;
        public bool enabled = true;
        public int priority;
        public bool convertToAddressables = true;
        public bool allowOverrideExistingKeys;
        public bool prefixSourceIdToKeys = true;
    }
}
