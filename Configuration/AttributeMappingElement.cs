using System.Configuration;

namespace KachingPlugIn.Configuration
{
    public class AttributeMappingElement : ConfigurationElement
    {
        [ConfigurationProperty("metaField", IsKey = true, IsRequired = true)]
        public string MetaField => (string)base["metaField"];

        [ConfigurationProperty("attributeId", IsRequired = true)]
        public string AttributeId => (string)base["attributeId"];
    }
}
