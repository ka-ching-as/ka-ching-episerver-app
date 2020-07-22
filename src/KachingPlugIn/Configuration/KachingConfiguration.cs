using System.Configuration;

namespace KachingPlugIn.Configuration
{
    public class KachingConfiguration : ConfigurationSection
    {
        public static KachingConfiguration Instance { get; } =
            ConfigurationManager.GetSection("kaching") as KachingConfiguration ??
            new KachingConfiguration();

        [ConfigurationProperty("foldersImportUrl", IsRequired = false)]
        public string FoldersImportUrl => (string)base["foldersImportUrl"];

        [ConfigurationProperty("productsImportUrl", IsRequired = true)]
        public string ProductsImportUrl => (string)base["productsImportUrl"];

        [ConfigurationProperty("productAssetsImportUrl", IsRequired = false)]
        public string ProductAssetsImportUrl => (string)base["productAssetsImportUrl"];

        [ConfigurationProperty("productRecommendationsImportUrl", IsRequired = false)]
        public string ProductRecommendationsImportUrl => (string)base["productRecommendationsImportUrl"];

        [ConfigurationProperty("tagsImportUrl", IsRequired = false)]
        public string TagsImportUrl => (string)base["tagsImportUrl"];

        [ConfigurationProperty("exportSingleVariantAsProduct", IsRequired = false)]
        public bool ExportSingleVariantAsProduct => (bool)base["exportSingleVariantAsProduct"];

        [ConfigurationProperty("listenToRemoteEvents", DefaultValue = true, IsRequired = false)]
        public bool ListenToRemoteEvents => (bool)base["listenToRemoteEvents"];

        [ConfigurationProperty("attributeMappings", IsDefaultCollection = false)]
        public AttributeMappingCollection AttributeMappings => (AttributeMappingCollection)base["attributeMappings"];

        [ConfigurationProperty("systemMappings", IsRequired = false)]
        public SystemMappingElement SystemMappings => (SystemMappingElement)base["systemMappings"];
    }
}