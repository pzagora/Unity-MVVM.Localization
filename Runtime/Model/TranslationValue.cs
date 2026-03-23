using System;
using System.Xml.Serialization;

namespace MVVM.Localization
{
    [Serializable]
    public class TranslationValue
    {
        [XmlAttribute("lang")]
        public string lang;

        [XmlText]
        public string text;
    }
}
