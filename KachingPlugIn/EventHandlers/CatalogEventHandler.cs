using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using KachingPlugIn.Helpers;
using KachingPlugIn.Services;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Engine.Events;
using System;
using System.Linq;
using KachingPlugIn.Configuration;

namespace KachingPlugIn.EventHandlers
{
    public class CatalogEventHandler
    {
        private enum ChangeType
        {
            Published,
            Moved,
            Deleting,
            Deleted
        }

        private readonly CatalogKeyEventBroadcaster _catalogKeyEventBroadcaster;
        private readonly ILogger _log = LogManager.GetLogger(typeof(CatalogEventHandler));
        private readonly IContentEvents _contentEvents;
        private readonly ReferenceConverter _referenceConverter;
        private readonly IContentLoader _contentLoader;
        private readonly CategoryExportService _categoryExportService;
        private readonly ProductExportService _productExportService;

        private NodeContent DeletingCategory = null;

        public CatalogEventHandler(
            CatalogKeyEventBroadcaster catalogKeyEventBroadcaster,
            IContentEvents contentEvents,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            CategoryExportService categoryExportService,
            ProductExportService productExportService)
        {
            _catalogKeyEventBroadcaster = catalogKeyEventBroadcaster;
            _contentEvents = contentEvents;
            _referenceConverter = referenceConverter;
            _contentLoader = contentLoader;
            _categoryExportService = categoryExportService;
            _productExportService = productExportService;
        }
        public void Initialize()
        {
            _catalogKeyEventBroadcaster.PriceUpdated += OnPriceUpdated;

            _contentEvents.PublishedContent += OnPublishedContent;
            _contentEvents.MovedContent += OnMovedContent;
            _contentEvents.DeletingContent += OnDeletingContent;
            _contentEvents.DeletedContent += OnDeletedContent;
        }

        public void Uninitialize()
        {
            _catalogKeyEventBroadcaster.PriceUpdated -= OnPriceUpdated;

            _contentEvents.PublishedContent -= OnPublishedContent;
            _contentEvents.MovedContent -= OnMovedContent;
            _contentEvents.DeletingContent -= OnDeletingContent;
            _contentEvents.DeletedContent -= OnDeletedContent;
        }

        private void OnDeletedContent(object sender, DeleteContentEventArgs e)
        {
            _log.Information("OnDeletedContent");
            if (DeletingCategory != null)
            {
                HandleCategoryChange(DeletingCategory, ChangeType.Deleted);
                DeletingCategory = null;
            }
        }

        private void OnDeletingContent(object sender, DeleteContentEventArgs e)
        {
            _log.Information("OnDeletingContent");
            try
            {
                var content = _contentLoader.Get<ContentData>(e.ContentLink);
                if (content is VariationContent)
                {
                    var variant = content as VariationContent;
                    _log.Information("Ka-ching event handler processing variant deleting: " + variant.Code);
                    HandleVariantChange(variant, true);
                }
                else if (content is ProductContent)
                {
                    var product = content as ProductContent;
                    _log.Information("Ka-ching event handler processing product deleting: " + product.Code);
                    HandleProductChange(product, true);
                    _productExportService.DeleteProductRecommendations(product);
                }
                else if (content is NodeContent)
                {
                    var node = content as NodeContent;
                    DeletingCategory = node;
                    _log.Information("Ka-ching event handler processing category deleting: " + node.Code);
                    HandleCategoryChange(node, ChangeType.Deleting);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void OnMovedContent(object sender, ContentEventArgs e)
        {
            _log.Information("OnMovedContent");
            try
            {
                var content = _contentLoader.Get<ContentData>(e.ContentLink);
                if (content is VariationContent)
                {
                    var variant = content as VariationContent;
                    _log.Information("Ka-ching event handler processing variant move: " + variant.Code);
                    HandleVariantChange(variant, false);
                }
                else if (content is ProductContent)
                {
                    var product = content as ProductContent;
                    _log.Information("Ka-ching event handler processing product move: " + product.Code);
                    HandleProductChange(product, false);
                }
                else if (content is NodeContent)
                {
                    var node = content as NodeContent;
                    _log.Information("Ka-ching event handler processing category move: " + node.Code);
                    HandleCategoryChange(node, ChangeType.Moved);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            _log.Information("OnPublishedContent");
            try
            {
                var content = _contentLoader.Get<ContentData>(e.ContentLink);
                if (content is VariationContent)
                {
                    var variant = content as VariationContent;
                    _log.Information("Ka-ching event handler processing variant publish: " + variant.Code);
                    HandleVariantChange(variant, false);
                }
                else if (content is ProductContent)
                {
                    var product = content as ProductContent;
                    _log.Information("Ka-ching event handler processing product publish: " + product.Code);
                    HandleProductChange(product, false);
                    _productExportService.ExportProductRecommendations(product);
                }
                else if (content is NodeContent)
                {
                    var node = content as NodeContent;
                    _log.Information("Ka-ching event handler processing category publish: " + node.Code);
                    HandleCategoryChange(node, ChangeType.Published);
                    _productExportService.ExportProductRecommendations(node);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void OnPriceUpdated(object sender, PriceUpdateEventArgs e)
        {
            _log.Information("OnPriceUpdated");
            try
            {
                var codes = e.CatalogKeys.Select(x => x.CatalogEntryCode);
                foreach (var code in codes)
                {
                    var link = _referenceConverter.GetContentLink(code);

                    var content = _contentLoader.Get<ContentData>(link);
                    if (content is VariationContent)
                    {
                        var variant = content as VariationContent;
                        _log.Information("Ka-ching event handler processing variant price update: " + variant.Code);
                        HandleVariantChange(variant, false);
                    }
                    else if (content is ProductContent)
                    {
                        var product = content as ProductContent;
                        _log.Information("Ka-ching event handler processing product price update: " + product.Code);
                        HandleProductChange(product, true);
                    }
                    else
                    {
                        _log.Warning("Price changed on unhandled type: " + content.GetType());
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }

        private void HandleProductChange(ProductContent product, bool isDelete, string deletedVariantCode = null)
        {
            _log.Information("HandleProductChange: " + product.Code, " isDelete: " + isDelete.ToString());

            // Make sure we have valid import endpoints configured before handling the change
            var configuration = KachingConfiguration.Instance;
            if (!configuration.ProductsImportUrl.IsValidProductsImportUrl())
            {
                _log.Error("Ka-ching product import url is not valid: " + configuration.ProductsImportUrl);
                return;
            }

            if (isDelete)
            {
                _productExportService.DeleteProduct(product, configuration.ProductsImportUrl);
                _productExportService.DeleteProductRecommendations(new[] { product.Code.KachingCompatibleKey() });
                _productExportService.DeleteProductAssets(new[] {product.Code.KachingCompatibleKey()});
            }
            else
            {
                _productExportService.ExportProduct(product, deletedVariantCode, configuration.ProductsImportUrl);
                _productExportService.ExportProductAssets(new[] {product});
                _productExportService.ExportProductRecommendations(product);
            }
        }

        private void HandleVariantChange(VariationContent variant, bool isDelete)
        {
            _log.Information("HandleVariantChange: " + variant.Code);

            var parents = variant.GetParentProducts();
            foreach (var parent in parents)
            {
                var product = _contentLoader.Get<ProductContent>(parent);

                // Since a variants are not independent datatypes in Ka-ching 
                // a variant change is always a product update and not a delete
                var deletedVariantCode = isDelete ? variant.Code : null;
                HandleProductChange(product, false, deletedVariantCode);
            }
        }

        private void HandleCategoryChange(NodeContent node, ChangeType changeType)
        {
            _log.Information("HandleCategoryChange - type: " + changeType + " code: " + node.Code);
            // Make sure we have valid import endpoints configured before handling the change
            var configuration = KachingConfiguration.Instance;
            if (!configuration.TagsImportUrl.IsValidTagsImportUrl() ||
                !configuration.FoldersImportUrl.IsValidFoldersImportUrl() ||
                !configuration.ProductsImportUrl.IsValidProductsImportUrl())
            {
                _log.Error("Ka-ching tag or folder import urls not valid: " + configuration.TagsImportUrl + " - " + configuration.FoldersImportUrl);
                return;
            }

            // Any category change in Episerver requires a full category export to Ka-ching 
            // to make sure the structure is represented correctly in Ka-ching.
            // We don't react to ChangeType.Deleting since that would find and export the category
            // that's being deleted.
            if (changeType != ChangeType.Deleting)
            {
                _categoryExportService.StartFullCategoryExport(configuration.TagsImportUrl, configuration.FoldersImportUrl);
            }

            // Product action depends on change type
            switch (changeType)
            {
                case ChangeType.Deleting:

                    // Deleting a category in the Episerver Commerce UI also deletes all children
                    // so we delete all child products here.
                    // This action assumes that products with the same Code are not duplicated
                    // anywhere else in the category tree.
                    // If this is the case you need to clone this repo and implement the correct action yourself.
                    _productExportService.DeleteChildProducts(node, configuration.ProductsImportUrl);
                    break;
                case ChangeType.Moved:
                    // Exporting all child products on a category move will ensure that the tags used 
                    // for folder placement in Ka-ching will match the new structure in Episerver.
                    // This action assumes that products with the same Code are not duplicated
                    // anywhere else in the category tree.
                    // If this is the case you need to clone this repo and implement the correct action yourself.
                    _productExportService.ExportChildProducts(node, configuration.ProductsImportUrl);
                    break;
                case ChangeType.Published:
                case ChangeType.Deleted:
                    // No product manipulation should be necessary here
                    break;
            }
        }
    }
}