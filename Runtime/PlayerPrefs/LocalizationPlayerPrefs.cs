using Commons.PlayerPrefs;

namespace MVVM.Localization
{
    public abstract class LocalizationPlayerPrefs : ATargetedPlayerPrefs
    {
        private const string KEY_LANGUAGE = "localization_language";
        
        public static void Save(string languageCode)
        {
            SetString(KEY_LANGUAGE, languageCode);
            Save();
        }

        public static bool TryLoad(out string languageCode)
        {
            languageCode = null;

            if (!HasKey(KEY_LANGUAGE))
                return false;

            languageCode = GetString(KEY_LANGUAGE);
            return true;
        }

        public static void Clear()
        {
            DeleteKey(KEY_LANGUAGE);
            Save();
        }
    }
}