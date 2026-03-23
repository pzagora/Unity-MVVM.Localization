using System;
using System.Xml.Serialization;
using Commons;
using Commons.Attributes;

namespace MVVM.Localization
{
    [Serializable]
    [XmlRoot("translation")]
    [XmlCollectionRoot("translations")]
    public class Translation : Indexed
    {
        [XmlElement("value")]
        public TranslationValue[] values;
    }
}
