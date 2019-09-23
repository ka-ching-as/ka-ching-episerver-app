using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction.Internal;
using EPiServer.Logging;
using KachingPlugIn.Models;
using System.Collections.Generic;

namespace KachingPlugIn.Factories
{
    public class L10nStringFactory
    {
        private readonly LanguageBranchRepository _languageBranchRepository;
        private readonly IContentLoader _contentLoader;
        private readonly ILogger _log = LogManager.GetLogger(typeof(L10nStringFactory));

        public L10nStringFactory(
            LanguageBranchRepository languageBranchRepository,
            IContentLoader contentLoader)
        {
            _languageBranchRepository = languageBranchRepository;
            _contentLoader = contentLoader;
        }

        public L10nString LocalizedProductName(ProductContent product)
        {
            return LocalizedContentDisplayName(product as IContent);
        }

        public L10nString LocalizedVariantName(VariationContent variation)
        {
            return LocalizedContentDisplayName(variation as IContent);
        }

        public L10nString LocalizedCategoryName(NodeContent category)
        {
            return LocalizedContentDisplayName(category as IContent);
        }

        private L10nString LocalizedContentDisplayName(IContent content)
        {
            var languageDictionary = new Dictionary<string, string>();
            var languages = _languageBranchRepository.ListAll();
            foreach (var language in languages)
            {
                var languageOption = LanguageLoaderOption.Specific(language.Culture);
                var options = new LoaderOptions() { languageOption };
                if (content is NodeContent)
                {
                    var c = _contentLoader.Get<NodeContent>(content.ContentLink, options);
                    if (c != null && c.DisplayName != null && language.Culture != null && language.Culture.TwoLetterISOLanguageName != null)
                    {
                        languageDictionary[language.Culture.TwoLetterISOLanguageName] = c.DisplayName;
                    }
                }
                else if (content is EntryContentBase)
                {
                    var c = _contentLoader.Get<EntryContentBase>(content.ContentLink, options);
                    if (c != null && c.DisplayName != null && language.Culture != null && language.Culture.TwoLetterISOLanguageName != null)
                    {
                        languageDictionary[language.Culture.TwoLetterISOLanguageName] = c.DisplayName;
                    }
                }
            }
            return new L10nString(languageDictionary);
        }
    }
}