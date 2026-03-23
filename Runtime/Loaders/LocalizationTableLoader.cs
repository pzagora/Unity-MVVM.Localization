using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if MVVM_LOCALIZATION_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace MVVM.Localization
{
    public static class LocalizationTableLoader
    {
        public static async Task<IReadOnlyList<Translation>> LoadTranslations(
            LocalizationSetup setup,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            var output = new Dictionary<string, Translation>();

            var shouldUseLocal = Application.isEditor && setup.PreferLocalInEditor;
            if (shouldUseLocal)
            {
                MergeLocal(setup, output);
                return output.Values.ToList();
            }

#if MVVM_LOCALIZATION_ADDRESSABLES
            var loadedAnyAddressables = await TryMergeAddressables(setup, output);
            if (loadedAnyAddressables)
            {
                if (setup.UseLocalAsRuntimeFallback)
                    MergeLocalWithoutOverwriting(setup, output);

                return output.Values.ToList();
            }
#endif
            MergeLocal(setup, output);
            return output.Values.ToList();
        }

        private static void MergeLocal(LocalizationSetup setup, IDictionary<string, Translation> output)
        {
            if (setup?.Catalog == null)
                return;

            foreach (var table in setup.Catalog.LocalTables)
                MergeTable(table, output, overwrite: true);
        }

        private static void MergeLocalWithoutOverwriting(LocalizationSetup setup, IDictionary<string, Translation> output)
        {
            if (setup?.Catalog == null)
                return;

            foreach (var table in setup.Catalog.LocalTables)
                MergeTable(table, output, overwrite: false);
        }

#if MVVM_LOCALIZATION_ADDRESSABLES
        private static async Task<bool> TryMergeAddressables(LocalizationSetup setup, IDictionary<string, Translation> output)
        {
            if (setup?.Catalog == null || setup.Catalog.AddressableKeys == null || setup.Catalog.AddressableKeys.Count == 0)
                return false;

            var loadedAny = false;
            foreach (var key in setup.Catalog.AddressableKeys)
            {
                var handle = Addressables.LoadAssetAsync<LocalizationTableAsset>(key);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    loadedAny = true;
                    MergeTable(handle.Result, output, overwrite: true);
                }

                Addressables.Release(handle);
            }

            return loadedAny;
        }
#endif

        private static void MergeTable(LocalizationTableAsset table, IDictionary<string, Translation> output, bool overwrite)
        {
            if (table == null)
                return;

            foreach (var translation in table.EnumerateTranslations())
            {
                if (translation == null || string.IsNullOrWhiteSpace(translation.Id))
                    continue;

                if (!overwrite && output.ContainsKey(translation.Id))
                    continue;

                output[translation.Id] = translation;
            }
        }
    }
}
