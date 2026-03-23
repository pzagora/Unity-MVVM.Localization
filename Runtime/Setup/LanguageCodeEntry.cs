using System;

namespace MVVM.Localization
{
    [Serializable]
    public struct LanguageCodeEntry
    {
        public Language language;
        public string code;
    }
}