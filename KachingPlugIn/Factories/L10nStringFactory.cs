using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using KachingPlugIn.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using EPiServer.Core;

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

        public L10nString GetLocalizedString(CatalogContentBase content, string propertyName)
        {
            if (!content.Property.Contains(propertyName))
            {
                return L10nString.EmptyLocalized;
            }

            Debug.Assert(content.ExistingLanguages is ICollection<CultureInfo>);
            var languageDictionary = new Dictionary<string, string>();
            var cultures = content.ExistingLanguages;

            foreach (var culture in cultures)
            {
                var localizedContent = _contentLoader.Get<CatalogContentBase>(content.ContentLink, culture);

                PropertyData data = localizedContent.Property[propertyName];
                if (data == null || data.IsNull)
                {
                    continue;
                }

                languageDictionary[culture.TwoLetterISOLanguageName] = (string)data.Value;
            }

            return new L10nString(languageDictionary);
        }
    }
}