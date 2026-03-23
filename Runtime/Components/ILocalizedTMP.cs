using Commons;

namespace MVVM.Localization
{
    public interface ILocalizedTMP : ISetupable<LocalizedText>
    {
        void UpdateText();
    }
}