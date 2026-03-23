using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MVVM.Localization
{
    internal class LanguageMapper : ILanguageMapper<Language>
    {
        private readonly Dictionary<string, Language> _map;
        private readonly Language _fallbackLanguage;

        public LanguageMapper(Dictionary<string, Language> map = null, Language fallbackLanguage = Language.English)
        {
            _fallbackLanguage = fallbackLanguage;
            _map = map ?? new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetLanguage(string code, out Language language)
            => _map.TryGetValue(code, out language);

        public Language MapSystemLanguage(SystemLanguage sysLang)
            => sysLang switch
            {
                SystemLanguage.Afrikaans => Language.Afrikaans,
                SystemLanguage.Arabic => Language.Arabic,
                SystemLanguage.Basque => Language.Basque,
                SystemLanguage.Belarusian => Language.Belarusian,
                SystemLanguage.Bulgarian => Language.Bulgarian,
                SystemLanguage.Catalan => Language.Catalan,
                SystemLanguage.Chinese => Language.Chinese,
                SystemLanguage.ChineseSimplified => Language.ChineseSimplified,
                SystemLanguage.ChineseTraditional => Language.ChineseTraditional,
                SystemLanguage.Czech => Language.Czech,
                SystemLanguage.Danish => Language.Danish,
                SystemLanguage.Dutch => Language.Dutch,
                SystemLanguage.English => Language.English,
                SystemLanguage.Estonian => Language.Estonian,
                SystemLanguage.Faroese => Language.Faroese,
                SystemLanguage.Finnish => Language.Finnish,
                SystemLanguage.French => Language.French,
                SystemLanguage.German => Language.German,
                SystemLanguage.Greek => Language.Greek,
                SystemLanguage.Hebrew => Language.Hebrew,
                SystemLanguage.Hungarian => Language.Hungarian,
                SystemLanguage.Icelandic => Language.Icelandic,
                SystemLanguage.Indonesian => Language.Indonesian,
                SystemLanguage.Italian => Language.Italian,
                SystemLanguage.Japanese => Language.Japanese,
                SystemLanguage.Korean => Language.Korean,
                SystemLanguage.Latvian => Language.Latvian,
                SystemLanguage.Lithuanian => Language.Lithuanian,
                SystemLanguage.Norwegian => Language.Norwegian,
                SystemLanguage.Polish => Language.Polish,
                SystemLanguage.Portuguese => Language.Portuguese,
                SystemLanguage.Romanian => Language.Romanian,
                SystemLanguage.Russian => Language.Russian,
                SystemLanguage.SerboCroatian => Language.SerboCroatian,
                SystemLanguage.Slovak => Language.Slovak,
                SystemLanguage.Slovenian => Language.Slovenian,
                SystemLanguage.Spanish => Language.Spanish,
                SystemLanguage.Swedish => Language.Swedish,
                SystemLanguage.Thai => Language.Thai,
                SystemLanguage.Turkish => Language.Turkish,
                SystemLanguage.Ukrainian => Language.Ukrainian,
                SystemLanguage.Vietnamese => Language.Vietnamese,
                SystemLanguage.Hindi => Language.Hindi,
                _ => _fallbackLanguage
            };

        public string GetCodeForLanguage(Language language)
            => _map.FirstOrDefault(x => x.Value.Equals(language)).Key ?? _map.First().Key;
    }
}