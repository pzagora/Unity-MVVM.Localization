using System;
using UnityEngine;

namespace MVVM.Localization
{
    public class LanguagePersistence<TLanguage> where TLanguage : Enum
    {
        private readonly ILanguageMapper<TLanguage> _mapper;

        public LanguagePersistence(ILanguageMapper<TLanguage> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public TLanguage LoadOrDetect()
        {
            if (LocalizationPlayerPrefs.TryLoad(out var savedCode))
            {
                if (_mapper.TryGetLanguage(savedCode, out var savedLang))
                    return savedLang;

                LocalizationPlayerPrefs.Clear();
            }

            return Save(_mapper.MapSystemLanguage(Application.systemLanguage));
        }

        public TLanguage Save(TLanguage language)
        {
            LocalizationPlayerPrefs.Save(_mapper.GetCodeForLanguage(language));
            return language;
        }
    }
}