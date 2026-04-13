using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Commons;
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
                progress?.Report(NumericExtensions.One);
                return output.Values.ToList();
            }

#if MVVM_LOCALIZATION_ADDRESSABLES
            var loadedAnyAddressables = await TryMergeAddressables(setup, output, progress, ct);
            if (loadedAnyAddressables)
            {
                if (setup.UseLocalAsRuntimeFallback)
                    MergeLocalWithoutOverwriting(setup, output);

                progress?.Report(NumericExtensions.One);
                return output.Values.ToList();
            }
#endif
            MergeLocal(setup, output);
            progress?.Report(NumericExtensions.One);
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
        private static async Task<bool> TryMergeAddressables(
            LocalizationSetup setup,
            IDictionary<string, Translation> output,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (setup?.Catalog == null || setup.Catalog.AddressableKeys == null || setup.Catalog.AddressableKeys.Count == 0)
                return false;

            var handles = new List<AsyncOperationHandle<LocalizationTableAsset>>();

            try
            {
                foreach (var key in setup.Catalog.AddressableKeys)
                {
                    ct.ThrowIfCancellationRequested();
                    handles.Add(Addressables.LoadAssetAsync<LocalizationTableAsset>(key));
                }

                var tasks = handles.Select(x => x.Task).ToArray();
                for (var i = NumericExtensions.Zero; i < tasks.Length; i++)
                {
                    await tasks[i];
                    progress?.Report((i + 1f) / tasks.Length);
                }

                var loadedAny = false;
                foreach (var handle in handles)
                {
                    ct.ThrowIfCancellationRequested();

                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    {
                        loadedAny = true;
                        MergeTable(handle.Result, output, overwrite: true);
                    }
                }

                return loadedAny;
            }
            finally
            {
                foreach (var handle in handles)
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                }
            }
        }
#endif

        private static void MergeTable(LocalizationTableAsset table, IDictionary<string, Translation> output, bool overwrite)
        {
            if (table == null)
                return;

            foreach (var translation in table.EnumerateEntries())
            {
                var id = LocalizationKeyUtility.NormalizeKey(translation?.Id);
                if (translation == null || string.IsNullOrWhiteSpace(id))
                    continue;

                translation.Id = id;

                if (!overwrite && output.ContainsKey(id))
                    continue;

                output[id] = translation;
            }
        }
    }
}
