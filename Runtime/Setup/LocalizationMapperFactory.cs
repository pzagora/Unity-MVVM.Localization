using System;
using System.Collections.Generic;

namespace MVVM.Localization
{
    public static class LocalizationMapperFactory
    {
        public static ILanguageMapper<Language> Create(LocalizationSetup setup)
        {
            var map = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in setup.languageCodes)
            {
                var code = LocalizationKeyUtility.NormalizeLanguageCode(entry.code);
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                map[code] = entry.language;
            }

            return new LanguageMapper(map, setup.fallbackLanguage);
        }
    }
}
