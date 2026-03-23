using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MVVM.Localization
{
    [CreateAssetMenu(menuName = "Scriptable Objects/MVVM/Localization/Table")]
    public sealed class LocalizationTableAsset : ScriptableObject
    {
        [SerializeField] private string sourceId;
        [SerializeField] private string tableId;
        [SerializeField] private string displayName;
        [SerializeField] private string checksum;
        [SerializeField] private List<LocalizationTableEntry> entries = new();
        
        public string SourceId => sourceId;
        public string TableId => tableId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? tableId : displayName;
        public string ContentHash => checksum;
        public string Checksum => checksum;

        public IEnumerable<Translation> EnumerateEntries()
        {
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;

                yield return entry.ToTranslation();
            }
        }

#if UNITY_EDITOR
        public void EditorSetMetadata(string newChecksum, string newSourceId, string newTableId, string newDisplayName)
        {
            checksum = newChecksum;
            sourceId = LocalizationKeyUtility.NormalizeSourceId(newSourceId);
            tableId = LocalizationKeyUtility.NormalizeTableId(newTableId);
            displayName = newDisplayName;
        }

        public void EditorSetEntries(List<LocalizationTableEntry> newEntries)
        {
            entries = newEntries ?? new List<LocalizationTableEntry>();
        }
#endif
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
                Id = LocalizationKeyUtility.NormalizeKey(id),
                values = values?
                    .Select(x => new TranslationValue
                    {
                        lang = LocalizationKeyUtility.NormalizeLanguageCode(x.languageCode),
                        text = x.text ?? string.Empty
                    })
                    .ToArray() ?? Array.Empty<TranslationValue>()
            };
        }
    }
    
    [Serializable]
    public sealed class LocalizationTableValue
    {
        public string languageCode;
        [TextArea(1, 5)] public string text;
    }
}
