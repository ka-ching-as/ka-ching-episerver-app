using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Events;
using Mediachase.Commerce.Engine.Events;

namespace KachingPlugIn.EventHandlers
{
    [ServiceConfiguration(Lifecycle = ServiceInstanceScope.Singleton)]
    public class CatalogContentEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CatalogContentEvents));
        private readonly IObjectInstanceCache _cache;
        private readonly IContentLoader _contentLoader;
        private readonly CategoryExportService _categoryExportService;
        private readonly ProductExportService _productExportService;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IRelationRepository _relationRepository;

        public CatalogContentEvents(
            IObjectInstanceCache cache,
            IContentLoader contentLoader,
            CategoryExportService categoryExportService,
            ProductExportService productExportService,
            ReferenceConverter referenceConverter,
            IRelationRepository relationRepository)
        {
            _cache = cache;
            _contentLoader = contentLoader;
            _categoryExportService = categoryExportService;
            _productExportService = productExportService;
            _referenceConverter = referenceConverter;
            _relationRepository = relationRepository;
        }

        public void Initialize(
            CatalogKeyEventBroadcaster catalogKeyEventBroadcaster,
            ICatalogEvents catalogEvents,
            IContentEvents contentEvents)
        {
            catalogKeyEventBroadcaster.PriceUpdated += OnPriceUpdated;

            catalogEvents.AssociationUpdating += OnAssociationUpdated;

            contentEvents.CreatedContent += OnCreatedContent;
            contentEvents.DeletedContent += OnDeletedContent;
            contentEvents.DeletingContent += OnDeletingContent;
            contentEvents.MovedContent += OnMovedContent;
            contentEvents.PublishedContent += OnPublishedContent;
        }

        public void Uninitialize(
            CatalogKeyEventBroadcaster catalogKeyEventBroadcaster,
            ICatalogEvents catalogEvents,
            IContentEvents contentEvents)
        {
            catalogKeyEventBroadcaster.PriceUpdated -= OnPriceUpdated;

            catalogEvents.AssociationUpdating -= OnAssociationUpdated;

            contentEvents.CreatedContent -= OnCreatedContent;
            contentEvents.DeletedContent -= OnDeletedContent;
            contentEvents.DeletingContent -= OnDeletingContent;
            contentEvents.MovedContent -= OnMovedContent;
            contentEvents.PublishedContent -= OnPublishedContent;
        }

        private void OnAssociationUpdated(object sender, AssociationEventArgs e)
        {
            Logger.Debug("OnAssociationUpdated raised.");

            ICollection<ProductContent> products = GetProductsAffected(e);

            // HACK: Episerver does not clear the deleted associations from cache until after this event has completed.
            // In order to load the list of associations after deletions/updates, force delete the association list from cache.
            foreach (ProductContent entry in products)
            {
                _cache.Remove("EP:ECF:Ass:" + entry.ContentLink.ID);
            }

            _productExportService.ExportProductRecommendations(products, null);
        }

        private void OnCreatedContent(object sender, ContentEventArgs e)
        {
            Logger.Debug("OnCreatedContent raised.");

            if (!(e is CopyContentEventArgs))
            {
                // Return now if the was anything other than a copy action.
                return;
            }

            ContentReference newContentLink = e.ContentLink;
            if (ContentReference.IsNullOrEmpty(newContentLink) ||
                newContentLink.ProviderName != ReferenceConverter.CatalogProviderKey ||
                !_contentLoader.TryGet(newContentLink, CultureInfo.InvariantCulture, out CatalogContentBase catalogContentBase))
            {
                Logger.Debug("Copied content is not catalog content.");
                return;
            }

            switch (catalogContentBase)
            {
                case EntryContentBase entryContent:
                    ICollection<ProductContent> products = GetProductsAffected(entryContent);

                    foreach (ProductContent product in products)
                    {
                        _productExportService.ExportProduct(product, null);
                    }

                    _productExportService.ExportProductAssets(products);
                    _productExportService.ExportProductRecommendations(products, null);

                    break;
                case NodeContent _:
                    // No need to export child entries on a copy/duplicate action, as there will not be any.
                    // Only re-publish the category structure.
                    _categoryExportService.StartFullCategoryExport();
                    break;
            }
        }

        private void OnDeletedContent(object sender, DeleteContentEventArgs e)
        {
            Logger.Debug("OnDeletedContent raised.");

            ContentReference deletedContentLink = e.ContentLink;
            if (ContentReference.IsNullOrEmpty(deletedContentLink) ||
                deletedContentLink.ProviderName != ReferenceConverter.CatalogProviderKey)
            {
                Logger.Debug("Deleted content is not catalog content.");
                return;
            }

            CatalogContentType catalogContentType = _referenceConverter.GetContentType(deletedContentLink);
            if (catalogContentType != CatalogContentType.CatalogNode)
            {
                Logger.Debug("Deleted content is not a catalog node.");
                return;
            }

            // Force a full category structure re-export. This has to be done AFTER the category has been deleted.
            _categoryExportService.StartFullCategoryExport();
        }

        private void OnDeletingContent(object sender, DeleteContentEventArgs e)
        {
            Logger.Debug("OnDeletingContent raised.");

            ContentReference deletingContentLink = e.ContentLink;
            if (ContentReference.IsNullOrEmpty(deletingContentLink) ||
                deletingContentLink.ProviderName != ReferenceConverter.CatalogProviderKey ||
                !_contentLoader.TryGet(deletingContentLink, out CatalogContentBase catalogContentBase))
            {
                Logger.Debug("Deleted content is not catalog content.");
                return;
            }

            switch (catalogContentBase)
            {
                case EntryContentBase entryContent:
                    ICollection<EntryContentBase> entries = GetEntriesAffected(entryContent, false, true);

                    _productExportService.DeleteProducts(entries);
                    _productExportService.DeleteProductAssets(entries);
                    _productExportService.DeleteProductRecommendations(entries);
                    break;
                case NodeContent nodeContent:
                    _productExportService.DeleteChildProducts(nodeContent);
                    break;
            }
        }

        private void OnMovedContent(object sender, ContentEventArgs e)
        {
            Logger.Debug("OnPublishedContent raised.");

            if (!(e.Content is CatalogContentBase catalogContentBase))
            {
                Logger.Debug("Moved content is not a catalog entry.");
                return;
            }

            switch (catalogContentBase)
            {
                case EntryContentBase entryContent:
                    ICollection<ProductContent> entries = GetProductsAffected(entryContent);
                    foreach (ProductContent productContent in entries)
                    {
                        _productExportService.ExportProduct(productContent, null);
                    }

                    break;
                case NodeContent nodeContent:
                    _productExportService.ExportChildProducts(nodeContent);
                    _categoryExportService.StartFullCategoryExport();
                    break;
            }
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            Logger.Debug("OnPublishedContent raised.");

            if (!(e.Content is CatalogContentBase catalogContentBase))
            {
                Logger.Debug("Published content is not catalog content.");
                return;
            }

            switch (catalogContentBase)
            {
                case EntryContentBase entryContent:
                    ICollection<ProductContent> products = GetProductsAffected(entryContent);

                    foreach (ProductContent product in products)
                    {
                        _productExportService.ExportProduct(product, null);
                    }

                    _productExportService.ExportProductAssets(products);
                    _productExportService.ExportProductRecommendations(products, null);

                    break;
                case NodeContent nodeContent:
                    _categoryExportService.StartFullCategoryExport();
                    _productExportService.ExportChildProducts(nodeContent);
                    break;
            }
        }

        private void OnPriceUpdated(object sender, PriceUpdateEventArgs e)
        {
            Logger.Debug("OnPriceUpdated raised.");

            var contentLinks = new HashSet<ContentReference>(
                e.CatalogKeys.Select(key => _referenceConverter.GetContentLink(key.CatalogEntryCode)));

            IEnumerable<ProductContent> products = GetProductsAffected(contentLinks);

            foreach (ProductContent productContent in products)
            {
                _productExportService.ExportProduct(productContent, null);
            }
        }

        private ICollection<EntryContentBase> GetEntriesAffected(
            EntryContentBase entryContent,
            bool includeParentProducts,
            bool includeChildVariants)
        {
            var uniqueLinks = new HashSet<ContentReference>(ContentReferenceComparer.Default);

            switch (entryContent)
            {
                case VariationContent variationContent:
                    if (includeParentProducts)
                    {
                        foreach (ContentReference parentLink in _relationRepository
                            .GetParents<ProductVariation>(variationContent.ContentLink)
                            .Select(pv => pv.Parent))
                        {
                            uniqueLinks.Add(parentLink);
                        }
                    }

                    break;
                case ProductContent productContent:
                    if (includeChildVariants)
                    {
                        foreach (ContentReference childLink in _relationRepository
                        .GetParents<ProductVariation>(productContent.ContentLink)
                        .Select(pv => pv.Child))
                        {
                            uniqueLinks.Add(childLink);
                        }
                    }

                    uniqueLinks.Add(productContent.ContentLink);
                    break;
            }

            ICollection<EntryContentBase> entries = _contentLoader
                .GetItems(uniqueLinks, CultureInfo.InvariantCulture)
                .OfType<EntryContentBase>()
                .ToArray();

            return entries;
        }

        private ICollection<ProductContent> GetProductsAffected(AssociationEventArgs e)
        {
            ICollection<ContentReference> entryLinks = new HashSet<ContentReference>(ContentReferenceComparer.Default);

            foreach (AssociationChange change in e.Changes)
            {
                entryLinks.Add(
                    _referenceConverter.GetContentLink(change.ParentEntryId, CatalogContentType.CatalogEntry, 0));
            }

            ICollection<ProductContent> entries = _contentLoader
                .GetItems(entryLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            return entries;
        }

        private ICollection<ProductContent> GetProductsAffected(
            IEnumerable<ContentReference> contentLinks)
        {
            IEnumerable<EntryContentBase> entryContents = _contentLoader
                .GetItems(
                    contentLinks.Distinct(ContentReferenceComparer.Default),
                    CultureInfo.InvariantCulture)
                .OfType<EntryContentBase>();

            return GetProductsAffected(entryContents);
        }

        private ICollection<ProductContent> GetProductsAffected(
            EntryContentBase entryContent)
        {
            return GetProductsAffected(new[] {entryContent});
        }

        private ICollection<ProductContent> GetProductsAffected(
            IEnumerable<EntryContentBase> entryContents)
        {
            ICollection<ContentReference> productLinks = new HashSet<ContentReference>(ContentReferenceComparer.Default);

            foreach (EntryContentBase entryContent in entryContents)
            {
                switch (entryContent)
                {
                    case VariationContent variationContent:
                        IEnumerable<ProductVariation> parentProductLinks =
                            _relationRepository.GetParents<ProductVariation>(variationContent.ContentLink);
                        foreach (ProductVariation parentProductLink in parentProductLinks)
                        {
                            productLinks.Add(parentProductLink.Parent);
                        }

                        break;
                    case ProductContent productContent:
                        productLinks.Add(productContent.ContentLink);
                        break;
                }
            }

            ICollection<ProductContent> products = _contentLoader
                .GetItems(productLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            return products;
        }
    }
}
