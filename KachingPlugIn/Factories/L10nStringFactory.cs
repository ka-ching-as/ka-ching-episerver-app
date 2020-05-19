using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using KachingPlugIn.Models;
using System.Collections.Generic;

namespace KachingPlugIn.Factories
{
    public class L10nStringFactory
    {
        private readonly IContentLoader _contentLoader;

        public L10nStringFactory(
            IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public L10nString LocalizedProductName(ProductContent product)
        {
            return LocalizedContentDisplayName(product);
        }

        public L10nString LocalizedVariantName(VariationContent variation)
        {
            return LocalizedContentDisplayName(variation);
        }

        public L10nString LocalizedCategoryName(NodeContent category)
        {
            return LocalizedContentDisplayName(category);
        }

        private L10nString LocalizedContentDisplayName(CatalogContentBase content)
        {
            var languageDictionary = new Dictionary<string, string>();
            var cultures = content.ExistingLanguages;
            
            foreach (var culture in cultures)
            {
                string displayName = null;
                switch (content)
                {
                    case NodeContent _:
                    {
                        var c = _contentLoader.Get<NodeContent>(content.ContentLink, culture);
                        displayName = c?.DisplayName;
                        break;
                    }
                    case EntryContentBase _:
                    {
                        var c = _contentLoader.Get<EntryContentBase>(content.ContentLink, culture);
                        displayName = c?.DisplayName;
                        break;
                    }
                }

                if (displayName != null && culture != null)
                {
                    languageDictionary[culture.TwoLetterISOLanguageName] = displayName;
                }
            }

            return new L10nString(languageDictionary);
        }
    }
}