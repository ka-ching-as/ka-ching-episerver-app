using System.Configuration;

namespace KachingPlugIn.Configuration
{
    public class SystemMappingElement : ConfigurationElement
    {
        [ConfigurationProperty("barcodeMetaField")]
        public string BarcodeMetaField => (string)base["barcodeMetaField"];
    }
}
