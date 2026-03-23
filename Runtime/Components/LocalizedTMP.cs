using Commons;
using Commons.Injection;
using UnityEngine;

namespace MVVM.Localization
{
    public class LocalizedTMP : StyleLinkedTMP, ILocalizedTMP
    {
        [Inject] private readonly ILocalizationService _localization;

        public LocalizedText Model { get; private set; }
        private bool _hasKey;

        public void Setup(LocalizedText model)
        {
            Model = model;
            Register();
        }

        public void UpdateText()
        {
            if (!this || _localization == null || Model == null)
                return;

            var template = _localization.GetLocalized(Model.Key);
            text = LocalizationFormatter.Format(Model.Key, template, Model.Args);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Register();
        }

        protected override void OnDisable()
        {
            _localization?.Unregister(this);
            base.OnDisable();
        }

        private void Register()
        {
            if (!Application.isPlaying)
                return;

            if (_localization == null) 
                return;

            if (Model == null)
                return;
            
            _localization.Register(this);
            UpdateText();
        }
    }
}