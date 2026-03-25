namespace MVVM.Localization
{
    public static class BindingGeneratorExtensions
    {
        public static LocalizedTextMeshProBinding LocalizedTMP(this BindingGenerator generator, LocalizedTMP target, IObservableValue<LocalizedText> source)
            => generator.RegisterBinding(BindingGenerator.Cache.Get<LocalizedTextMeshProBinding, LocalizedTMP, LocalizedText>(target, source));

        public static LocalizedTextMeshProBinding LocalizedTMP(this BindingGenerator generator, LocalizedTMP target, LocalizedText value)
            => generator.RegisterBinding(BindingGenerator.Cache.Get<LocalizedTextMeshProBinding, LocalizedTMP, LocalizedText>(target, value));
    }
}