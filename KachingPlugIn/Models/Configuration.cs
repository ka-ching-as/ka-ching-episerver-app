using EPiServer.Data;
using EPiServer.Data.Dynamic;
using System;

namespace KachingPlugIn.Models
{
    public class Configuration : IDynamicData
    {
        private static Identity ID = Identity.NewIdentity(new Guid("9f070fad-4c4c-49c6-9443-510ab091500a"));

        private static DynamicDataStore Store()
        {
            return DynamicDataStoreFactory.Instance.CreateStore("Kaching.AddOn.Configuration", typeof(Configuration));
        } 

        public static Configuration Instance()
        {
            var result = Store().Load(ID) as Configuration;
            if (result == null)
            {
                result = new Configuration();
                result.Save();
            }
            return result;
        }

        public Identity Id { get; set; }

        public string ProductsImportUrl { get; set; }
        public string TagsImportUrl { get; set; }
        public string FoldersImportUrl { get; set; }

        public Configuration()
        {
            Initialize();
        }

        private void Initialize()
        {
            Id = Configuration.ID;
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