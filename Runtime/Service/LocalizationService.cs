using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Commons.Extensions;
using Commons.Injection;
using UnityEngine;

namespace MVVM.Localization
{
    [Install(typeof(ILocalizationService))]
    public class LocalizationService : LocalizationService<Language>, ILocalizationService
    {
        public LocalizationService(LocalizationSetup setup)
            : base(setup, LocalizationMapperFactory.Create(setup))
        {
        }
    }

    public class LocalizationService<TLanguage> : ILocalizationService<TLanguage>
        where TLanguage : Enum
    {
        public TLanguage CurrentLanguage { get; private set; }

        private readonly LocalizationSetup _setup;
        private readonly ILanguageMapper<TLanguage> _mapper;
        private readonly LanguagePersistence<TLanguage> _languagePersistence;
        private readonly HashSet<ILocalizedTMP> _registeredTexts = new();
        private readonly Dictionary<string, Dictionary<TLanguage, string>> _translations = new();

        private const int TEXT_UPDATE_BATCH_SIZE = 20;

        private const float PROGRESS_START = 0f;
        private const float PROGRESS_LOAD_WEIGHT = 0.5f;
        private const float PROGRESS_MAP_WEIGHT = 0.4f;
        private const float PROGRESS_BATCH_UPDATE_START = 0.95f;
        private const float PROGRESS_COMPLETE = 1f;
        private const float PROGRESS_EMPTY_COMPLETE = 1f;

        private const string LOG_REGISTERED_TRANSLATIONS = "Registered {0} translation keys";
        private const string LOG_LANGUAGE_CHANGED = "Language changed: {0} -> {1}";
        private const string LOG_MISSING_TRANSLATION = "Missing translation in {0}: {1}";
        private const string LOG_MISSING_KEY = "Missing translation key: {0}";
        private const string LOG_FALLBACK_USED = "Fallback translation used: requested={0}, fallback={1}, key={2}";
        private const string LOG_UNKNOWN_LANGUAGE = "Unknown language code: {0} ({1} entries){2}";
        private const string LOG_TEXTS_UPDATED = "Text components updated: {0} | Batch amount: {1}";

        protected LocalizationService(LocalizationSetup setup, ILanguageMapper<TLanguage> mapper)
        {
            _setup = setup ?? throw new ArgumentNullException(nameof(setup));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _languagePersistence = new LanguagePersistence<TLanguage>(_mapper);

            CurrentLanguage = _languagePersistence.LoadOrDetect();
        }

        public async Task Initialize(IProgress<float> progress = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            _translations.Clear();
            progress?.Report(PROGRESS_START);

            var loadProgress = new Progress<float>(value => { progress?.Report(value * PROGRESS_LOAD_WEIGHT); });

            var loadedTranslations = await LocalizationTableLoader.LoadTranslations(_setup, loadProgress, ct);
            ct.ThrowIfCancellationRequested();

            var unknownLanguages = new Dictionary<string, UnknownLanguageInfo>();
            var translationCount = loadedTranslations.Count;

            if (translationCount == NumericExtensions.Zero)
            {
                Report.Event<LocalizationService>(string.Format(LOG_REGISTERED_TRANSLATIONS, NumericExtensions.Zero));
                LogUnknownLanguages(unknownLanguages);

                progress?.Report(PROGRESS_EMPTY_COMPLETE);
                return;
            }

            for (var i = NumericExtensions.Zero; i < translationCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var translation = loadedTranslations[i];
                var normalizedId = LocalizationKeyUtility.NormalizeKey(translation?.Id);
                if (string.IsNullOrWhiteSpace(normalizedId))
                    continue;

                _translations[normalizedId] = BuildTranslationMap(translation, unknownLanguages);

                var itemProgress = (i + 1f) / translationCount;
                progress?.Report(PROGRESS_LOAD_WEIGHT + itemProgress * PROGRESS_MAP_WEIGHT);

                await Task.Yield();
            }

            Report.Event<LocalizationService>(string.Format(LOG_REGISTERED_TRANSLATIONS, _translations.Keys.Count));
            LogUnknownLanguages(unknownLanguages);

            ct.ThrowIfCancellationRequested();

            progress?.Report(PROGRESS_BATCH_UPDATE_START);
            await BatchUpdate(ct);

            progress?.Report(PROGRESS_COMPLETE);
        }

        public void Register(ILocalizedTMP component)
            => _registeredTexts.Add(component);

        public void Unregister(ILocalizedTMP component)
            => _registeredTexts.Remove(component);

        public async Task ChangeLanguage(TLanguage language, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (Equals(CurrentLanguage, language))
                return;

            Report.Event<LocalizationService>(string.Format(LOG_LANGUAGE_CHANGED, CurrentLanguage, language));

            CurrentLanguage = language;
            _languagePersistence.Save(CurrentLanguage);

            await BatchUpdate(ct);
        }

        public bool HasLocalized(string key)
            => !string.IsNullOrWhiteSpace(key) && _translations.ContainsKey(LocalizationKeyUtility.NormalizeKey(key));

        public string GetLocalized(string key)
            => GetLocalized(key, CurrentLanguage);

        public string GetLocalized(string key, TLanguage language)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = LocalizationKeyUtility.NormalizeKey(key);

            if (!_translations.TryGetValue(key, out var langMap) || langMap == null || langMap.Count == 0)
            {
                Report.Warning<LocalizationService>(string.Format(LOG_MISSING_KEY, key));
                return key;
            }

            if (TryGetNonEmpty(langMap, language, out var text))
                return text;

            if (TryGetFallbackLanguage(langMap, language, key, out text))
                return text;

            foreach (var pair in langMap)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                    return pair.Value;
            }

            Report.Warning<LocalizationService>(string.Format(LOG_MISSING_TRANSLATION, language, key));
            return key;
        }

        private Dictionary<TLanguage, string> BuildTranslationMap(
            Translation translation,
            Dictionary<string, UnknownLanguageInfo> unknownLanguages)
        {
            var map = new Dictionary<TLanguage, string>();
            if (translation?.values == null)
                return map;

            foreach (var translationValue in translation.values)
            {
                var languageCode = LocalizationKeyUtility.NormalizeLanguageCode(translationValue.lang);
                if (_mapper.TryGetLanguage(languageCode, out var lang))
                {
                    map[lang] = translationValue.text ?? string.Empty;
                    continue;
                }

                RegisterUnknownLanguage(translation.Id, languageCode, unknownLanguages);
            }

            return map;
        }

        private bool TryGetFallbackLanguage(
            Dictionary<TLanguage, string> langMap,
            TLanguage requestedLanguage,
            string key,
            out string text)
        {
            text = null;

            if (!TryGetConfiguredFallbackLanguage(out var fallbackLanguage))
                return false;

            if (Equals(fallbackLanguage, requestedLanguage))
                return false;

            if (!TryGetNonEmpty(langMap, fallbackLanguage, out text))
                return false;

            Report.Event<LocalizationService>(string.Format(LOG_FALLBACK_USED, requestedLanguage, fallbackLanguage, key));
            return true;
        }

        private bool TryGetConfiguredFallbackLanguage(out TLanguage fallbackLanguage)
        {
            fallbackLanguage = default;

            if (typeof(TLanguage) != typeof(Language))
                return false;

            fallbackLanguage = (TLanguage)(object)_setup.fallbackLanguage;
            return true;
        }

        private static bool TryGetNonEmpty(Dictionary<TLanguage, string> map, TLanguage language, out string text)
        {
            if (map.TryGetValue(language, out text) && !string.IsNullOrWhiteSpace(text))
                return true;

            text = null;
            return false;
        }

        private static void RegisterUnknownLanguage(
            string translationKey,
            string languageCode,
            Dictionary<string, UnknownLanguageInfo> unknownLanguages)
        {
            languageCode ??= string.Empty;

            if (!unknownLanguages.TryGetValue(languageCode, out var info))
            {
                info = new UnknownLanguageInfo();
                unknownLanguages[languageCode] = info;
            }

            info.Count++;
            info.TranslationKeys.Add(translationKey);
        }

        private static void LogUnknownLanguages(Dictionary<string, UnknownLanguageInfo> unknownLanguages)
        {
            foreach (var pair in unknownLanguages)
            {
                var keys = BuildUnknownLanguageKeysLog(pair.Value.TranslationKeys);
                Report.Warning<LocalizationService>(string.Format(LOG_UNKNOWN_LANGUAGE, pair.Key, pair.Value.Count, keys));
            }
        }

        private static string BuildUnknownLanguageKeysLog(IEnumerable<string> translationKeys)
        {
            var keys = translationKeys?.Where(x => !string.IsNullOrWhiteSpace(x)).Take(5).ToArray();
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var builder = new StringBuilder();
            builder.Append(" | Keys: ");
            builder.Append(string.Join(", ", keys));
            return builder.ToString();
        }

        private async Task BatchUpdate(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (_registeredTexts.Count == 0)
                return;

            var snapshot = _registeredTexts.Where(x => x != null).ToArray();
            var count = 0;
            var batchCount = 1;

            foreach (var text in snapshot)
            {
                ct.ThrowIfCancellationRequested();

                text.UpdateText();
                count++;

                if (count % TEXT_UPDATE_BATCH_SIZE == 0 && count < snapshot.Length)
                {
                    await Task.Yield();
                    batchCount++;
                }
            }

            Report.Event<LocalizationService>(string.Format(LOG_TEXTS_UPDATED, count, batchCount));
        }
    }
}
