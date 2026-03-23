using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MVVM.Localization
{
    [Serializable]
    public sealed class LocalizationTableValue
    {
        public string languageCode;
        [TextArea(1, 5)] public string text;
    }

    [Serializable]
    public sealed class LocalizationTableEntry
    {
        public string id;
        public List<LocalizationTableValue> values = new();

        public Translation ToTranslation()
        {
            return new Translation
            {
                Id = id?.Trim().ToLowerInvariant(),
                values = values?
                    .Select(x => new TranslationValue
                    {
                        lang = x.languageCode?.Trim(),
                        text = x.text ?? string.Empty
                    })
                    .ToArray() ?? Array.Empty<TranslationValue>()
            };
        }
    }

    [CreateAssetMenu(menuName = "Scriptable Objects/Localization/Table")]
    public sealed class LocalizationTableAsset : ScriptableObject
    {
        [SerializeField] private string sheetName;
        [SerializeField] private string spreadsheetId;
        [SerializeField] private int gid = -1;
        [SerializeField] private string checksum;
        [SerializeField] private List<LocalizationTableEntry> entries = new();

        public string SheetName => sheetName;
        public string SpreadsheetId => spreadsheetId;
        public int Gid => gid;
        public string Checksum => checksum;
        public IReadOnlyList<LocalizationTableEntry> Entries => entries;

        public IEnumerable<Translation> EnumerateTranslations()
        {
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                yield return entry.ToTranslation();
            }
        }

#if UNITY_EDITOR
        public void EditorSetMetadata(string newSpreadsheetId, string newSheetName, int newGid, string newChecksum)
        {
            spreadsheetId = newSpreadsheetId;
            sheetName = newSheetName;
            gid = newGid;
            checksum = newChecksum;
        }

        public void EditorSetEntries(List<LocalizationTableEntry> newEntries)
        {
            entries = newEntries ?? new List<LocalizationTableEntry>();
        }
#endif
    }
}
