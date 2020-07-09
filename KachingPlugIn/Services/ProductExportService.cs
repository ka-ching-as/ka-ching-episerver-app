using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using KachingPlugIn.Factories;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthorizeNet.Api.Contracts.V1;
using KachingPlugIn.Configuration;
using KachingPlugIn.KachingPlugIn.Models;

namespace KachingPlugIn.Services
{
    public class ProductExportService
    {
        private const int BatchSize = 1000;
        private readonly ReferenceConverter _referenceConverter;
        private readonly KachingConfiguration _configuration;
        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly ProductFactory _productFactory;
        private readonly ILogger _log = LogManager.GetLogger(typeof(ProductExportService));

        public IExportState ExportState { get; set; }

        public ProductExportService(
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository,
            ProductFactory productFactory)
        {
            _referenceConverter = referenceConverter;
            _configuration = KachingConfiguration.Instance;
            _contentLoader = contentLoader;
            _contentVersionRepository = contentVersionRepository;
            _productFactory = productFactory;
        }

        public void StartFullProductExport(string url)
        {
            if (ExportState != null)
            {
                if (ExportState.Busy)
                {
                    return;
                }
                ExportState.Busy = true;
            }

            Task.Run(() =>
            {
                try
                {
                    ExportAllProducts(url);
                    ExportAllProductAssets();
                }
                catch (WebException e)
                {
                    ResetState(true);
                    _log.Error("Response status code: " + e.Status);
                    _log.Error("Export aborted with error: " + e.Message);
                }
                catch (Exception e)
                {
                    ResetState(true);
                    _log.Error("Export aborted with error: " + e.Message);
                }
            });
        }

        public void DeleteProduct(ProductContent product, string url)
        {
            _log.Information("DeleteProduct: " + product.Code);

            // Bail if not published
            var isPublished = _contentVersionRepository.ListPublished(product.ContentLink).Count() > 0;
            if (!isPublished)
            {
                _log.Information("Skipped product delete because it's not yet published");
                return;
            }

            var ids = new List<string>();
            ids.Add(product.Code.KachingCompatibleKey());
            var statusCode = APIFacade.Delete(ids, url);
            _log.Information("Status code: " + statusCode);

            DeleteProductAssets(ids);
        }

        public void DeleteChildProducts(NodeContent category, string url)
        {
            _log.Information("DeleteChildProducts: " + category.Code);

            var tags = ParentTagsForCategory(category);
            var categories = new List<NodeContent>();
            categories.Add(category);
            // TODO - getting product ids here is enough.
            var products = BuildKachingProducts(categories, tags);
            var ids = products.Select(p => p.Id).ToArray();
            APIFacade.Delete(ids, url);

            DeleteProductAssets(ids);
        }

        public void DeleteProductAssets(ICollection<string> entryCodes)
        {
            if (entryCodes == null ||
                entryCodes.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductAssetsImportUrl.IsValidProductAssetsImportUrl())
            {
                return;
            }

            APIFacade.Delete(entryCodes, _configuration.ProductAssetsImportUrl);
        }

        public void ExportProductAssets(ICollection<EntryContentBase> entries)
        {
            if (entries == null ||
                entries.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductAssetsImportUrl.IsValidProductAssetsImportUrl())
            {
                return;
            }

            foreach (var batch in entries.Batch(BatchSize))
            {
                var assets = new Dictionary<string, ICollection<ProductAsset>>(BatchSize);

                foreach (var entry in batch)
                {
                    assets.Add(
                        entry.Code.KachingCompatibleKey(),
                        _productFactory.BuildKaChingProductAssets(entry).ToArray());
                }

                APIFacade.Post(
                    new { assets },
                    _configuration.ProductAssetsImportUrl);
            }
        }

        public void ExportProduct(ProductContent product, string deletedVariantCode, string url)
        {
            var configuration = KachingConfiguration.Instance;
            _log.Information("ExportProduct: " + product.Code);

            // Bail if not published
            var isPublished = _contentVersionRepository.ListPublished(product.ContentLink).Count() > 0;
            if (!isPublished)
            {
                _log.Information("Skipped product export because it's not yet published");
                return;
            }

            // Since Ka-ching uses tags for category hierachy we need to go
            // up the tree of nodes to find the correct tags for the product
            var tags = TagsForProduct(product);
            var kachingProduct = _productFactory.BuildKaChingProduct(product, tags, configuration, deletedVariantCode);
            var products = new List<Product>();
            products.Add(kachingProduct);
            PostKachingProducts(products, url);
        }

        public void ExportChildProducts(NodeContent category, string url)
        {
            _log.Information("ExportChildProducts: " + category.Code);

            // Since Ka-ching uses tags for category hierachy we need to go
            // up the tree of nodes to find the correct tags for the product
            var tags = ParentTagsForCategory(category);
            var categories = new List<NodeContent>();
            categories.Add(category);
            var products = BuildKachingProducts(categories, tags);
            PostKachingProducts(products, url);
        }

        private void ExportAllProducts(string url)
        {
            var root = _contentLoader.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            var first = root.FirstOrDefault();
            var children = _contentLoader.GetChildren<NodeContent>(first.ContentLink);
            var products = BuildKachingProducts(children, new List<string>());

            if (ExportState != null)
            {
                ExportState.Total = products.Count;
            }

            PostKachingProducts(products, url);

            ResetState(false);
        }

        private void ExportAllProductAssets()
        {
            var assets = new Dictionary<string, IEnumerable<ProductAsset>>();
            var entriesWithoutAssets = new List<string>();

            var root = _contentLoader.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            BuildKachingProductAssets(root.FirstOrDefault(), assets, entriesWithoutAssets);

            if (assets.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductAssetsImportUrl.IsValidProductAssetsImportUrl())
            {
                return;
            }

            if (ExportState != null)
            {
                ExportState.Total = assets.Count;
            }

            // Push assets groups for products that have assets.
            foreach (var batch in assets.Batch(BatchSize))
            {
                APIFacade.Post(
                    new { assets = batch },
                    _configuration.ProductAssetsImportUrl);
            }

            // Delete asset groups for products that have no assets (even if they do not exist in Ka-ching).
            // This is to enforce no assets for products that have no assets, even if an individual deletion was missed earlier.
            foreach (var batch in entriesWithoutAssets.Batch(BatchSize))
            {
                APIFacade.Delete(batch, _configuration.ProductAssetsImportUrl);
            }

            ResetState(false);
        }

        private IList<Product> BuildKachingProducts(IEnumerable<NodeContent> nodes, IList<string> tags)
        {
            var configuration = KachingConfiguration.Instance;
            var kachingProducts = new List<Product>();

            foreach (var node in nodes)
            {
                var nextTags = new List<string>();
                nextTags.Add(node.Code.KachingCompatibleKey());
                nextTags.AddRange(tags);

                var childrenNodes = _contentLoader.GetChildren<NodeContent>(node.ContentLink);
                if (childrenNodes.Count() > 0)
                {
                    var kachingProductsFromChildren = this.BuildKachingProducts(childrenNodes, nextTags);
                    kachingProducts.AddRange(kachingProductsFromChildren);
                }
                else
                {
                    var products = _contentLoader.GetChildren<ProductContent>(node.ContentLink);
                    foreach (var product in products)
                    {
                        // continue if not published
                        var isPublished = _contentVersionRepository.ListPublished(product.ContentLink).Count() > 0;
                        if (!isPublished)
                        {
                            _log.Information("Skipped product because it's not yet published");
                            continue;
                        }

                        kachingProducts.Add(_productFactory.BuildKaChingProduct(product, nextTags, configuration, null));
                    }
                }
            }

            return kachingProducts;
        }

        private void BuildKachingProductAssets(
            NodeContentBase node,
            IDictionary<string, IEnumerable<ProductAsset>> productAssets,
            ICollection<string> entriesWithoutAssets)
        {
            var children = _contentLoader.GetChildren<CatalogContentBase>(node.ContentLink);

            foreach (var child in children)
            {
                switch (child)
                {
                    case NodeContent childNode:
                        BuildKachingProductAssets(
                            childNode,
                            productAssets,
                            entriesWithoutAssets);
                        break;
                    case ProductContent childEntry:
                        ICollection<ProductAsset> assets = _productFactory.BuildKaChingProductAssets(childEntry);
                        if (assets == null)
                        {
                            entriesWithoutAssets.Add(
                                childEntry.Code.KachingCompatibleKey());
                        }
                        else
                        {
                            productAssets.Add(
                                childEntry.Code.KachingCompatibleKey(),
                                assets);
                        }
                        break;
                }
            }
        }

        private IList<string> TagsForProduct(ProductContent product)
        {
            var result = new List<string>();
            var link = product.ParentLink;

            if (_contentLoader.TryGet(link, out NodeContent category))
            {
                result.Add(category.Code.KachingCompatibleKey());
                result.AddRange(ParentTagsForCategory(category));
            }
            else
            {
                _log.Warning("Parent link is not linking to a category as expected");
            }

            return result;
        }

        private IList<string> ParentTagsForCategory(NodeContent category)
        {
            var result = new List<string>();

            IEnumerable<NodeContent> ancestors = _contentLoader
                .GetAncestors(category.ContentLink)
                .OfType<NodeContent>();
            foreach (var ancestor in ancestors)
            {
                result.Add(ancestor.Code.KachingCompatibleKey());
            }

            return result;
        }

        private void PostKachingProducts(IList<Product> products, string url)
        {
            _log.Information("Number of products: " + products.Count);

            var workload = products;
            while (workload.Count > 0)
            {
                var actualBatchSize = Math.Min(BatchSize, workload.Count);
                var batch = workload.Take(actualBatchSize);
                workload = workload.Skip(actualBatchSize).ToList();
                PostBatchOfKachingProducts(batch.ToList(), url);

                if (ExportState != null)
                {
                    ExportState.Uploaded += actualBatchSize;

                    _log.Information("Products posted: " + ExportState.Uploaded.ToString());

                    if (ExportState.Uploaded < ExportState.Total)
                    {
                        System.Threading.Thread.Sleep(1000); // Pause for a bit to avoid ratelimit
                    }
                }
            }
        }

        private void PostBatchOfKachingProducts(IList<Product> batch, string url)
        {
            var statusCode = APIFacade.Post(JsonWrapper(batch), url);
            _log.Information("Status code: " + statusCode.ToString());
        }

        private object JsonWrapper(IList<Product> products)
        {
            return new
            {
                metadata = _productFactory.ProductMetadata(),
                products
            };
        }

        private void ResetState(bool error)
        {
            if (ExportState == null)
            {
                return;
            }

            ExportState.Busy = false;
            ExportState.Total = 0;
            ExportState.Uploaded = 0;
            ExportState.Polls = 0;
            ExportState.Error = error;
        }
    }
}