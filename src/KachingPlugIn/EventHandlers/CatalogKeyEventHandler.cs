using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Events;
using EPiServer.Events.Clients;
using EPiServer.Logging;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;

namespace KachingPlugIn.EventHandlers
{
    public class CatalogKeyEventHandler
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CatalogKeyEventHandler));
        private readonly IContentLoader _contentLoader;
        private readonly ProductExportService _productExportService;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IRelationRepository _relationRepository;

        public CatalogKeyEventHandler(
            IContentLoader contentLoader,
            ProductExportService productExportService,
            ReferenceConverter referenceConverter,
            IRelationRepository relationRepository)
        {
            _contentLoader = contentLoader;
            _productExportService = productExportService;
            _referenceConverter = referenceConverter;
            _relationRepository = relationRepository;
        }

        public void Initialize()
        {
            Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid).Raised += OnCatalogKeyEventUpdated;
        }

        public void Uninitialize()
        {
            Event.Get(CatalogKeyEventBroadcaster.CatalogKeyEventGuid).Raised -= OnCatalogKeyEventUpdated;
        }

        private void OnCatalogKeyEventUpdated(object sender, EventNotificationEventArgs e)
        {
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
    }
}
