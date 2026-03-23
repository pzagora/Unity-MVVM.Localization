using System;
using UnityEngine;

namespace MVVM.Localization
{
    public interface ILanguageMapper<TLanguage>
        where TLanguage : Enum
    {
        bool TryGetLanguage(string code, out TLanguage language);
        TLanguage MapSystemLanguage(SystemLanguage sysLang);
        string GetCodeForLanguage(TLanguage language);
    }
}