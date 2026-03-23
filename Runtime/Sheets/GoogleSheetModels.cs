using System;
using System.Collections.Generic;

namespace MVVM.Localization
{
    [Serializable]
    public sealed class GoogleSheetManifest
    {
        public List<GoogleSheetManifestTab> tabs = new();
    }

    [Serializable]
    public sealed class GoogleSheetManifestTab
    {
        public string title;
        public string tableId;
        public string checksum;
        public List<string> headers = new();
        public List<GoogleSheetManifestRow> rows = new();
    }

    [Serializable]
    public sealed class GoogleSheetManifestRow
    {
        public string key;
        public List<string> values = new();
    }
}
