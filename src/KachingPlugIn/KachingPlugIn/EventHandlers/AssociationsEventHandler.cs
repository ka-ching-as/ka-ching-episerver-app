using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Catalog.Events;

namespace KachingPlugIn.KachingPlugIn.EventHandlers
{
    [ServiceConfiguration(typeof(CatalogEventListenerBase), Lifecycle = ServiceInstanceScope.Singleton)]
    public class AssociationsEventHandler : CatalogEventListenerBase
    {
        private readonly IObjectInstanceCache _cache;
        private readonly ProductExportService _productExportService;
        private readonly ReferenceConverter _referenceConverter;

        public AssociationsEventHandler(
            IObjectInstanceCache cache,
            ProductExportService productExportService,
            ReferenceConverter referenceConverter)
        {
            _cache = cache;
            _productExportService = productExportService;
            _referenceConverter = referenceConverter;
        }

        //public override void AssociationDeleted(object source, DeletedAssociationEventArgs args)
        //{
        //    ContentReference entryRef = _referenceConverter.GetContentLink(
        //        args.ParentEntryId,
        //        CatalogContentType.CatalogEntry,
        //        0);

        //    _productExportService.ExportProductRecommendations(new[] { entryRef });
        //}

        public override void AssociationUpdated(object source, AssociationEventArgs args)
        {
            // Get only one ContentReference per product, no matter how many changes are queued up for each.
            ICollection<ContentReference> entryRefs = args.Changes
                .GroupBy(ac => ac.ParentEntryId)
                .Select(g => g.First())
                .Select(ac =>
                    _referenceConverter.GetContentLink(
                        ac.ParentEntryId,
                        CatalogContentType.CatalogEntry,
                        0))
                .ToArray();

            if (entryRefs.Count == 0)
            {
                return;
            }

            // HACK: Episerver does not clear the deleted associations from cache until after this event has completed.
            // In order to load the list of associations after deletions/updates, force delete the association list from cache.
            foreach (ContentReference entryRef in entryRefs)
            {
                _cache.Remove("EP:ECF:Ass:" + entryRef.ID);
            }

            _productExportService.ExportProductRecommendations(entryRefs);
        }
    }
}
