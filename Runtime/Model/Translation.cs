using System;
using Commons;

namespace MVVM.Localization
{
    [Serializable]
    public class Translation : Indexed
    {
        public TranslationValue[] values;
    }
}
