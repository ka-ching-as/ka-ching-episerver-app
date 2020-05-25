using System.Configuration;

namespace KachingPlugIn.Configuration
{
    public class SystemMappingElement : ConfigurationElement
    {
        [ConfigurationProperty("barcodeMetaField")]
        public string BarcodeMetaField => (string)base["barcodeMetaField"];

        [ConfigurationProperty("descriptionMetaField")]
        public string DescriptionMetaField => (string)base["descriptionMetaField"];

        [ConfigurationProperty("priceUnitMetaField")]
        public string PriceUnitMetaField => (string) base["priceUnitMetaField"];
    }
}
