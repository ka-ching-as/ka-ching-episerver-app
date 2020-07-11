using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Core;
using EPiServer.Logging;
using KachingPlugIn.Configuration;
using KachingPlugIn.Factories;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using Mediachase.Commerce.Catalog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EPiServer.Framework.Cache;

namespace KachingPlugIn.Services
{
    public class ProductExportService
    {
        private const int BatchSize = 1000;
        private readonly IAssociationRepository _associationRepository;
        private readonly GroupDefinitionRepository<AssociationGroupDefinition> _associationGroupRepository;
        private readonly ReferenceConverter _referenceConverter;
        private readonly KachingConfiguration _configuration;
        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly ProductFactory _productFactory;
        private readonly ILogger _log = LogManager.GetLogger(typeof(ProductExportService));

        public IExportState ExportState { get; set; }

        public ProductExportService(
            IAssociationRepository associationRepository,
            GroupDefinitionRepository<AssociationGroupDefinition> associationGroupRepository,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            IContentVersionRepository contentVersionRepository,
            ProductFactory productFactory)
        {
            _associationRepository = associationRepository;
            _associationGroupRepository = associationGroupRepository;
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
                    ExportAllProductRecommendations();

                    ResetState(false);
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
            _log.Information("Status code: " + statusCode.ToString());
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

            // When deleting category entries, tell Ka-ching to delete all outgoing product recommendations for the entries.
            // If any of those entries actually have no recommendations, Ka-ching will silently ignore those deletions.
            DeleteProductRecommendations(ids);
        }

        public void DeleteProductRecommendations(EntryContentBase entry)
        {
            DeleteProductRecommendations(new[] { entry.Code.KachingCompatibleKey() });
        }

        public void DeleteProductRecommendations(ICollection<string> entryCodes)
        {
            if (entryCodes == null ||
                entryCodes.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductRecommendationsImportUrl.IsValidProductRecommendationsImportUrl())
            {
                return;
            }

            // For each known association group, tell Ka-ching to delete all outgoing associations for the specified products.
            // We are doing it this way, because the import queue expects to be told which recommendation category
            // to delete associations from. Deleting a catalog entry from Episerver, affects all recommendation categories.
            foreach (var associationGroup in _associationGroupRepository.List())
            {
                // Do the deletion in batches.
                foreach (var batch in entryCodes.Batch(BatchSize))
                {
                    APIFacade.Delete(
                        batch,
                        _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" + associationGroup.Name.KachingCompatibleKey());
                }
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

        public void ExportAllProductRecommendations()
        {
            var catalog = _contentLoader
                .GetChildren<CatalogContent>(_referenceConverter.GetRootLink())
                .FirstOrDefault();
            if (catalog == null)
            {
                return;
            }

            ExportProductRecommendations(catalog);
        }

        public void ExportProductRecommendations(EntryContentBase entry)
        {
            ExportProductRecommendations(new[] { entry.ContentLink });
        }

        public void ExportProductRecommendations(NodeContentBase node)
        {
            if (node == null)
            {
                return;
            }

            if (!_configuration.ProductRecommendationsImportUrl.IsValidProductRecommendationsImportUrl())
            {
                return;
            }

            IEnumerable<ContentReference> descendentRefs = _contentLoader
                .GetDescendents(node.ContentLink);

            IEnumerable<ContentReference> entryRefs = _contentLoader
                .GetItems(descendentRefs, CultureInfo.InvariantCulture)
                .OfType<EntryContentBase>()
                .Select(c => c.ContentLink);

            ExportProductRecommendations(entryRefs);
        }

        public void ExportProductRecommendations(IEnumerable<ContentReference> entryLinks)
        {
            if (!_configuration.ProductRecommendationsImportUrl.IsValidProductRecommendationsImportUrl())
            {
                return;
            }

            var allAssociations = new List<Association>();
            var entryLinksToDelete = new HashSet<ContentReference>(ContentReferenceComparer.IgnoreVersion);

            foreach (ContentReference entryLink in entryLinks
                .Distinct(ContentReferenceComparer.IgnoreVersion))
            {
                var associations = (ICollection<Association>)_associationRepository.GetAssociations(entryLink);
                 if (associations.Count == 0)
                {
                    entryLinksToDelete.Add(entryLink);
                }
                else
                {
                    allAssociations.AddRange(associations);
                }
            }

            foreach (var associationsByGroup in allAssociations
                .GroupBy(a => a.Group.Name))
            {
                var recommendationGroups = associationsByGroup
                    .GroupBy(a => a.Source)
                    .Select(g => _productFactory.BuildKaChingRecommendationGroup(g.Key, g.ToArray()))
                    .Where(x => x != null);

                foreach (var group in recommendationGroups
                    .Batch(BatchSize))
                {
                    APIFacade.Post(
                        new { products = group },
                        _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" + associationsByGroup.Key.KachingCompatibleKey());
                }
            }

            if (entryLinksToDelete.Count == 0)
            {
                return;
            }

            foreach (var associationGroup in _associationGroupRepository.List())
            {
                foreach (var batch in _contentLoader
                    .GetItems(entryLinksToDelete, CultureInfo.InvariantCulture)
                    .OfType<EntryContentBase>()
                    .Select(c => c.Code.KachingCompatibleKey())
                    .Batch(BatchSize))
                {
                    APIFacade.DeleteObject(
                        batch,
                        _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" + associationGroup.Name.KachingCompatibleKey());
                }
            }
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
            var link = category.ParentLink;

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