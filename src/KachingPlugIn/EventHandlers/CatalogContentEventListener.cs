using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Events;
using EPiServer.Events.Clients;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using KachingPlugIn.Configuration;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Events;
using Mediachase.Commerce.Engine.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace KachingPlugIn.EventHandlers
{
    [ServiceConfiguration(typeof(CatalogContentEventListener), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CatalogContentEventListener
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CatalogContentEventListener));
        private readonly IObjectInstanceCache _cache;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IRelationRepository _relationRepository;
        private readonly IContentLoader _contentLoader;
        private readonly CategoryExportService _categoryExportService;
        private readonly ProductExportService _productExportService;

        public CatalogContentEventListener(
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

        public void Initialize()
        {
            Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised += CatalogContentUpdated;
            Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid).Raised += CatalogKeyEventUpdated;
        }

        public void Uninitialize()
        {
            Event.Get(CatalogEventBroadcaster.CommerceProductUpdated).Raised -= CatalogContentUpdated;
            Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid).Raised -= CatalogKeyEventUpdated;
        }

        private void CatalogContentUpdated(object sender, EventNotificationEventArgs e)
        {
            if (!KachingConfiguration.Instance.ListenToRemoteEvents && !IsLocalEvent(e))
            {
                return;
            }

            if (!(Deserialize(e) is CatalogContentUpdateEventArgs e1))
            {
                return;
            }

            switch (e1.EventType)
            {
                case CatalogEventBroadcaster.AssociationUpdatedEventType:
                    AssociationUpdated(e1);
                    break;
                case CatalogEventBroadcaster.CatalogEntryDeletedEventType:
                    EntryDeleted(e1);
                    break;
                case CatalogEventBroadcaster.CatalogEntryUpdatedEventType:
                    EntryUpdated(e1);
                    break;
                case CatalogEventBroadcaster.CatalogNodeUpdatedEventType:
                    NodeUpdated(e1);
                    break;
                case CatalogEventBroadcaster.CatalogNodeDeletedEventType:
                    NodeDeleted(e1);
                    break;
                case CatalogEventBroadcaster.RelationUpdatedEventType:
                    RelationUpdated(e1);
                    break;
            }
        }

        private void CatalogKeyEventUpdated(object sender, EventNotificationEventArgs e)
        {
            if (!KachingConfiguration.Instance.ListenToRemoteEvents && !IsLocalEvent(e))
            {
                return;
            }

            if (!(Deserialize(e) is PriceUpdateEventArgs e1))
            {
                return;
            }

            Logger.Debug("PriceUpdated raised.");

            var contentLinks = new HashSet<ContentReference>(
                e1.CatalogKeys.Select(key => _referenceConverter.GetContentLink(key.CatalogEntryCode)));

            IEnumerable<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks);

            ProductContent[] products = _contentLoader.GetItems(affectedProductLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            foreach (ProductContent productContent in products)
            {
                _productExportService.ExportProduct(productContent, null);
            }
        }

        private void AssociationUpdated(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("AssociationUpdated raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);
            ICollection<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks).ToArray();

            // HACK: Episerver does not clear the deleted associations from cache until after this event has completed.
            // In order to load the list of associations after deletions/updates, force delete the association list from cache.
            foreach (ContentReference entryRef in affectedProductLinks)
            {
                _cache.Remove("EP:ECF:Ass:" + entryRef.ID);
            }

            _productExportService.ExportProductRecommendations(affectedProductLinks, null);
        }

        public void EntryDeleted(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("EntryDeleted raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);
            IEnumerable<ContentReference> affectedProductLinks = GetAffectedProductReferences(contentLinks);

            ProductContent[] products = _contentLoader.GetItems(affectedProductLinks, CultureInfo.InvariantCulture)
                .OfType<ProductContent>()
                .ToArray();

            _productExportService.DeleteProducts(products);
            _productExportService.DeleteProductAssets(products);
            _productExportService.DeleteProductRecommendations(products);
        }

        public void EntryUpdated(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("EntryUpdated raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);
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

        public void NodeDeleted(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("NodeDeleted raised.");

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);

            NodeContent[] nodes = _contentLoader.GetItems(contentLinks, CultureInfo.InvariantCulture)
                .OfType<NodeContent>()
                .ToArray();

            foreach (NodeContent node in nodes)
            {
                _productExportService.DeleteChildProducts(node);
            }
        }

        public void NodeUpdated(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("NodeUpdated raised.");

            var configuration = KachingConfiguration.Instance;

            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);

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

        public void RelationUpdated(CatalogContentUpdateEventArgs e)
        {
            Logger.Debug("RelationUpdated raised.");

            // If an entry is moved to another node, export the entry.
            IEnumerable<ContentReference> contentLinks = GetContentLinks(e);
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
            CatalogContentUpdateEventArgs eventArgs)
        {
            var uniqueLinks = new HashSet<ContentReference>();

            foreach (int entryId in eventArgs.CatalogEntryIds.Where(i => i > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        entryId,
                        CatalogContentType.CatalogEntry,
                        0));
            }

            foreach (int nodeId in eventArgs.CatalogNodeIds.Where(i => i > 0))
            {
                uniqueLinks.Add(
                    _referenceConverter.GetContentLink(
                        nodeId,
                        CatalogContentType.CatalogNode,
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

        private static EventArgs Deserialize(EventNotificationEventArgs eventArgs)
        {
            var buffer = eventArgs.Param as byte[];
            if (buffer == null)
            {
                return EventArgs.Empty;
            }

            var binaryFormatter = new BinaryFormatter();
            using (var serializationStream = new MemoryStream(buffer))
            {
                return binaryFormatter.Deserialize(serializationStream) as EventArgs;
            }
        }

        private static bool IsLocalEvent(EventNotificationEventArgs e)
        {
            return e.RaiserId == CatalogKeyEventBroadcaster.EventRaiserId ||
                   e.RaiserId == CatalogEventBroadcaster.EventRaiserId;
        }
    }
}