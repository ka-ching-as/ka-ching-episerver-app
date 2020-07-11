using System.Configuration;

namespace KachingPlugIn.Configuration
{
    public class KachingConfiguration : ConfigurationSection
    {
        private static KachingConfiguration _instance;

        [ConfigurationProperty("foldersImportUrl", IsRequired = false)]
        public string FoldersImportUrl => (string)base["foldersImportUrl"];

        [ConfigurationProperty("productsImportUrl", IsRequired = true)]
        public string ProductsImportUrl => (string)base["productsImportUrl"];

        [ConfigurationProperty("productRecommendationsImportUrl", IsRequired = false)]
        public string ProductRecommendationsImportUrl => (string)base["productRecommendationsImportUrl"];

        [ConfigurationProperty("tagsImportUrl", IsRequired = false)]
        public string TagsImportUrl => (string)base["tagsImportUrl"];

        [ConfigurationProperty("exportSingleVariantAsProduct", IsRequired = false)]
        public bool ExportSingleVariantAsProduct => (bool)base["exportSingleVariantAsProduct"];

        [ConfigurationProperty("attributeMappings", IsDefaultCollection = false)]
        public AttributeMappingCollection AttributeMappings => (AttributeMappingCollection)base["attributeMappings"];

        [ConfigurationProperty("systemMappings", IsRequired = false)]
        public SystemMappingElement SystemMappings => (SystemMappingElement)base["systemMappings"];

        public static KachingConfiguration Instance =>
            _instance ?? (_instance = ConfigurationManager.GetSection("kaching") as KachingConfiguration ??
                                      new KachingConfiguration());
    }
}