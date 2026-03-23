using Commons;

namespace MVVM.Localization
{
    public interface ILocalizedTMP : ISetupable<LocalizedText>
    {
        LocalizedText Model { get; }
        void UpdateText();
    }
}