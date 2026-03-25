using UnityEngine.Assertions;

namespace MVVM.Localization
{
    public class LocalizedTextMeshProBinding : TargetBinding<LocalizedTMP, LocalizedText>
    {
        public LocalizedTextMeshProBinding(LocalizedTMP target, IObservableValue<LocalizedText> source) 
            : base(target, source) 
            => Assert.IsNotNull(target);

        public LocalizedTextMeshProBinding(LocalizedTMP target, LocalizedText value) 
            : base(target, value) 
            => Assert.IsNotNull(target);

        protected override void UpdateValue(LocalizedText value) 
            => Target.Setup(value);
    }
}