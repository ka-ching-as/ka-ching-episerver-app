using System.Configuration;

namespace KachingPlugIn.Configuration
{
    [ConfigurationCollection(
        typeof(AttributeMappingCollection),
        CollectionType = ConfigurationElementCollectionType.AddRemoveClearMap)]
    public class AttributeMappingCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new AttributeMappingElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((AttributeMappingElement)element).MetaField;
        }
    }
}
