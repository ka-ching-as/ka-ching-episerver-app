using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using KachingPlugIn.Configuration;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Events;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KachingPlugIn.EventHandlers
{
    [ServiceConfiguration(typeof(CatalogEventListenerBase), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CatalogEventHandler : CatalogEventListenerBase
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CatalogEventHandler));
        private readonly IObjectInstanceCache _cache;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IRelationRepository _relationRepository;
        private readonly IContentLoader _contentLoader;
        private readonly CategoryExportService _categoryExportService;
        private readonly ProductExportService _productExportService;

        public CatalogEventHandler(
            IObjectInstanceCache cache,
            ReferenceConverter referenceConverter,
            IRelationRepository relationRepository,
            IContentLoader contentLoader,
            CategoryExportService categoryExportService,
            ProductExportService productExportService)
        {
            _cache = cache;
            _referenceConverter = referenceConverter;
            _relationRepository = relationRepository;
            _contentLoader = contentLoader;
            _categoryExportService = categoryExportService;
            _productExportService = productExportService;
        }

        public override void AssociationUpdated(object sender, AssociationEventArgs e)
        {
            Logger.Debug("AssociationUpdated raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.Changes);
            ICollection<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks).ToArray();

            // HACK: Episerver does not clear the deleted associations from cache until after this event has completed.
            // In order to load the list of associations after deletions/updates, force delete the association list from cache.
            foreach (ContentReference entryRef in affectedProductLinks)
            {
                _cache.Remove("EP:ECF:Ass:" + entryRef.ID);
            }

            _productExportService.ExportProductRecommendations(affectedProductLinks);
        }

        public override void EntryDeleted(object sender, DeletedEntryEventArgs e)
        {
            Logger.Debug("EntryDeleted raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.Changes);
            IEnumerable<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks);

            ProductContent[] products = _contentLoader.GetItems(affectedProductLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            _productExportService.DeleteProducts(products);
            _productExportService.DeleteProductAssets(products);
            _productExportService.DeleteProductRecommendations(products);
        }

        public override void EntryUpdated(object sender, EntryEventArgs e)
        {
            Logger.Debug("EntryUpdated raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.Changes);
            IEnumerable<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks);

            ProductContent[] products = _contentLoader.GetItems(affectedProductLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            foreach (ProductContent product in products)
            {
                // TODO: Refactor to batched export.
                _productExportService.ExportProduct(product, null);
            }

            _productExportService.ExportProductAssets(products);
        }

        public override void NodeDeleted(object sender, DeletedNodeEventArgs e)
        {
            Logger.Debug("NodeDeleted raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.Changes);

            NodeContent[] nodes = _contentLoader.GetItems(contentLinks, CultureInfo.InvariantCulture)
                .OfType<NodeContent>()
                .ToArray();

            foreach (NodeContent node in nodes)
            {
                _productExportService.DeleteChildProducts(node);
            }
        }

        public override void NodeUpdated(object sender, NodeEventArgs e)
        {
            Logger.Debug("NodeUpdated raised.");

            var configuration = KachingConfiguration.Instance;

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.Changes);

            NodeContent[] nodes = _contentLoader.GetItems(contentLinks, CultureInfo.InvariantCulture)
                .OfType<NodeContent>()
                .ToArray();

            // If this event was triggered by a move, re-export the complete category structure.
            if (e.HasChangedParent)
            {
                _categoryExportService.StartFullCategoryExport(
                    configuration.TagsImportUrl,
                    configuration.FoldersImportUrl);
            }

            foreach (NodeContent node in nodes)
            {
                _productExportService.ExportChildProducts(node);
            }
        }

        public override void RelationUpdated(object sender, RelationEventArgs e)
        {
            Logger.Debug("RelationUpdated raised.");

            // If an entry is moved to another node, export the entry.
            IEnumerable<ContentReference> contentLinks = GetContentLinks(e.NodeEntryRelationChanges);
            IEnumerable<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks).ToArray();

            ProductContent[] products = _contentLoader.GetItems(affectedProductLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            foreach (ProductContent product in products)
            {
                _productExportService.ExportProduct(product, null);
            }
        }

        private IEnumerable<ContentReference> GetContentLinks(
            IEnumerable<AssociationChange> associationChanges)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (AssociationChange associationChange in associationChanges.Where(c => c.ParentEntryId > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        associationChange.ParentEntryId,
                        CatalogContentType.CatalogEntry,
                        0));
            }

            return uniqueLinks;
        }

        private IEnumerable<ContentReference> GetContentLinks(
            IEnumerable<EntryChange> entryChanges)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (EntryChange entryChange in entryChanges.Where(c => c.EntryId > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        entryChange.EntryId,
                        CatalogContentType.CatalogEntry,
                        0));
            }

            return uniqueLinks;
        }

        private IEnumerable<ContentReference> GetContentLinks(
            IEnumerable<NodeChange> nodeChanges)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (NodeChange nodeChange in nodeChanges.Where(c => c.NodeId > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        nodeChange.NodeId,
                        CatalogContentType.CatalogNode,
                        0));
            }

            return uniqueLinks;
        }

        private IEnumerable<ContentReference> GetContentLinks(
            IEnumerable<NodeEntryRelationChange> nodeEntryRelationChanges)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (NodeEntryRelationChange nodeEntryRelationChange in nodeEntryRelationChanges
                .Where(c => c.EntryId > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        nodeEntryRelationChange.EntryId,
                        CatalogContentType.CatalogEntry,
                        0));
            }

            return uniqueLinks;
        }

        private IEnumerable<ContentReference> GetAffectedProductReferences(
            IEnumerable<ContentReference> contentLinks)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (IContent content in _contentLoader.GetItems(contentLinks, CultureInfo.InvariantCulture))
            {
                switch (content)
                {
                    case VariationContent variationContent:
                        foreach (ContentReference parentLink in _relationRepository
                            .GetParents<ProductVariation>(variationContent.ContentLink)
                            .Select(pv => pv.Parent))
                        {
                            uniqueLinks.Add(parentLink);
                        }

                        uniqueLinks.Add(variationContent.ParentLink);
                        break;
                    case ProductContent productContent:
                        uniqueLinks.Add(productContent.ContentLink);
                        break;
                }
            }

            return uniqueLinks;
        }
    }
}