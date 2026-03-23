using System;
using System.Collections.Generic;

namespace MVVM.Localization
{
    [Serializable]
    public sealed class GoogleSheetManifest
    {
        public string spreadsheetId;
        public string spreadsheetName;
        public string exportedAtUtc;
        public List<GoogleSheetManifestTab> tabs = new();
    }

    [Serializable]
    public sealed class GoogleSheetManifestTab
    {
        public string title;
        public int gid;
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
