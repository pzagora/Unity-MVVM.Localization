using System.Collections.Generic;

namespace MVVM.Localization
{
    internal sealed class UnknownLanguageInfo
    {
        public int Count { get; set; }
        public HashSet<string> TranslationKeys { get; } = new();
    }
}