using System.Collections.Generic;
using UnityEngine;

namespace MVVM.Localization
{
    [CreateAssetMenu(menuName = "Scriptable Objects/Localization/Setup")]
    public class LocalizationSetup : ScriptableObject
    {
        public Language fallbackLanguage = Language.English;
        public List<LanguageCodeEntry> languageCodes = new();

#if UNITY_EDITOR
        [SerializeField] private string translationsUrl;
        public string TranslationsUrl => translationsUrl;
#endif
    }
}