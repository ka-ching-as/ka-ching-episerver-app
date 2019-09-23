using EPiServer.Data;
using EPiServer.Data.Dynamic;
using System;

namespace KachingPlugIn.Models
{
    public class KachingPlugInConfiguration : IDynamicData
    {
        private static Identity ID = Identity.NewIdentity(new Guid("459f9041-e454-4bc3-ab92-b7867ab7c863"));

        private static DynamicDataStore Store()
        {
            return DynamicDataStoreFactory.Instance.CreateStore("KachingPlugInConfiguration", typeof(KachingPlugInConfiguration));
        } 

        public static KachingPlugInConfiguration Instance()
        {
            var result = Store().Load(ID) as KachingPlugInConfiguration;
            if (result == null)
            {
                result = new KachingPlugInConfiguration();
                result.Save();
            }
            return result;
        }

        public Identity Id { get; set; }

        public string ProductsImportUrl { get; set; }
        public string TagsImportUrl { get; set; }
        public string FoldersImportUrl { get; set; }

        public KachingPlugInConfiguration()
        {
            Initialize();
        }

        private void Initialize()
        {
            Id = KachingPlugInConfiguration.ID;
            ProductsImportUrl = string.Empty;
            TagsImportUrl = string.Empty;
            FoldersImportUrl = string.Empty;
        }

        public void Save()
        {
            Store().Save(this);
        }
    }
}