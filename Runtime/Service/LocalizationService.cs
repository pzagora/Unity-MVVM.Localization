using System;
using System.Collections.Generic;
using System.Text;
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
            : this(LocalizationMapperFactory.Create(setup))
        {
        }

        private LocalizationService(ILanguageMapper<Language> mapper)
            : base(mapper)
        {
        }

        public async Task RegisterTranslations(IEnumerable<Translation> translations)
            => await Register(translations);
    }

    public class LocalizationService<TLanguage> : ILocalizationService<TLanguage>
        where TLanguage : Enum
    {
        public TLanguage CurrentLanguage { get; private set; }

        private readonly ILanguageMapper<TLanguage> _mapper;
        private readonly LanguagePersistence<TLanguage> _languagePersistence;
        private readonly HashSet<ILocalizedTMP> _registeredTexts = new();
        private readonly Dictionary<string, Dictionary<TLanguage, string>> _translations = new();

        private const int TEXT_UPDATE_BATCH_SIZE = 20;

        private const string LOG_REGISTERED_TRANSLATIONS = "Registered {0} translation keys";
        private const string LOG_LANGUAGE_CHANGED = "Language changed: {0} -> {1}";
        private const string LOG_MISSING_TRANSLATION = "Missing translation in {0}: {1}";
        private const string LOG_UNKNOWN_LANGUAGE = "Unknown language code: {0} ({1} entries){2}";
        private const string LOG_TEXTS_UPDATED = "Text components updated: {0} | Batch amount: {1})";

        protected LocalizationService(ILanguageMapper<TLanguage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _languagePersistence = new LanguagePersistence<TLanguage>(_mapper);

            CurrentLanguage = _languagePersistence.LoadOrDetect();
        }

        public void Register(ILocalizedTMP component)
            => _registeredTexts.Add(component);

        public void Unregister(ILocalizedTMP component)
            => _registeredTexts.Remove(component);

        public async Task Register(IEnumerable<Translation> translations)
        {
            _translations.Clear();

            var unknownLanguages = new Dictionary<string, UnknownLanguageInfo>();

            foreach (var translation in translations)
            {
                _translations[translation.Id] = BuildTranslationMap(translation, unknownLanguages);
                await Task.Yield();
            }
            
            Report.Event<LocalizationService>(string.Format(LOG_REGISTERED_TRANSLATIONS, _translations.Keys.Count));
            LogUnknownLanguages(unknownLanguages);

            await BatchUpdate();
        }

        public async Task ChangeLanguage(TLanguage language)
        {
            if (Equals(CurrentLanguage, language))
                return;

            Report.Event<LocalizationService>(string.Format(LOG_LANGUAGE_CHANGED, CurrentLanguage, language));

            CurrentLanguage = language;
            _languagePersistence.Save(CurrentLanguage);

            await BatchUpdate();
        }

        public bool HasLocalized(string key)
            => !string.IsNullOrWhiteSpace(key) && _translations.ContainsKey(key.ToLowerInvariant());

        public string GetLocalized(string key)
            => GetLocalized(key, CurrentLanguage);

        public string GetLocalized(string key, TLanguage language)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            key = key.ToLowerInvariant();

            var hasMap = _translations.TryGetValue(key, out var langMap);
            if (hasMap && langMap.TryGetValue(language, out var text))
            {
                return text;
            }

            Report.Warning<LocalizationService>(string.Format(LOG_MISSING_TRANSLATION, language, key));

            return key;
        }
        
        private Dictionary<TLanguage, string> BuildTranslationMap(Translation translation, Dictionary<string, UnknownLanguageInfo> unknownLanguages)
        {
            var map = new Dictionary<TLanguage, string>();

            foreach (var translationValue in translation.values)
            {
                if (_mapper.TryGetLanguage(translationValue.lang, out var lang))
                {
                    map[lang] = translationValue.text;
                    continue;
                }

                RegisterUnknownLanguage(translation.Id, translationValue.lang, unknownLanguages);
            }

            return map;
        }

        private static void RegisterUnknownLanguage(
            string translationKey,
            string languageCode,
            Dictionary<string, UnknownLanguageInfo> unknownLanguages)
        {
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

        private static string BuildUnknownLanguageKeysLog(HashSet<string> translationKeys)
        {
            if (translationKeys.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            builder.AppendLine();
            
            foreach (var key in translationKeys)
            {
                builder.Append("- ");
                builder.AppendLine(key);
            }

            return builder.ToString();
        }

        private async Task BatchUpdate()
        {
            if (_registeredTexts.Count == NumericExtensions.Zero)
                return;

            var count = NumericExtensions.Zero;
            var batchCount = NumericExtensions.One;

            foreach (var text in _registeredTexts)
            {
                text.UpdateText();
                count++;

                if (count % TEXT_UPDATE_BATCH_SIZE != NumericExtensions.Zero || count >= _registeredTexts.Count) 
                    continue;
                
                await Task.Yield();
                batchCount++;
            }

            Report.Warning<LocalizationService>(string.Format(LOG_TEXTS_UPDATED, count, batchCount));
        }
    }
}