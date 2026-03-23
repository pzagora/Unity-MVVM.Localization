using System;
using System.Threading;
using System.Threading.Tasks;
using Commons;

namespace MVVM.Localization
{
    public interface ILocalizationService : ILocalizationService<Language>
    {
        /// <summary>
        /// Initialize service with translations from setup.
        /// </summary>
        Task Initialize(IProgress<float> progress = null, CancellationToken ct = default);
    }
    
    public interface ILocalizationService<TLanguage> : IService
        where TLanguage : Enum
    {
        /// <summary>
        /// Get currently selected language.
        /// </summary>
        TLanguage CurrentLanguage { get; }
        
        /// <summary>
        /// Register a ILocalizedText into the service.
        /// </summary>
        void Register(ILocalizedTMP component);
        
        /// <summary>
        /// Unregister a ILocalizedText from the service.
        /// </summary>
        void Unregister(ILocalizedTMP component);

        /// <summary>
        /// Change current language.
        /// </summary>
        Task ChangeLanguage(TLanguage language, CancellationToken ct = default);

        /// <summary>
        /// Check if translation for given key exists in any language
        /// </summary>
        bool HasLocalized(string key);
        
        /// <summary>
        /// Get translated text for a key in current language.
        /// </summary>
        string GetLocalized(string key);

        /// <summary>
        /// Get translated text for a key in specific language.
        /// </summary>
        string GetLocalized(string key, TLanguage language);
    }
}