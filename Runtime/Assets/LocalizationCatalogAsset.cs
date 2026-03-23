using System.Collections.Generic;
using UnityEngine;

namespace MVVM.Localization
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Localization/Catalog")]
    public sealed class LocalizationCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<LocalizationTableAsset> localTables = new();
#if MVVM_LOCALIZATION_ADDRESSABLES
        [SerializeField] private List<string> addressableKeys = new();
#endif

        public IReadOnlyList<LocalizationTableAsset> LocalTables => localTables;
#if MVVM_LOCALIZATION_ADDRESSABLES
        public IReadOnlyList<string> AddressableKeys => addressableKeys;
#endif

#if UNITY_EDITOR
        public void EditorSetLocalTables(List<LocalizationTableAsset> tables)
            => localTables = tables ?? new List<LocalizationTableAsset>();

#if MVVM_LOCALIZATION_ADDRESSABLES
        public void EditorSetAddressableKeys(List<string> keys)
            => addressableKeys = keys ?? new List<string>();
#endif
#endif
    }
}
