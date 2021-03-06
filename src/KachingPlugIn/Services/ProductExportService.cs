﻿using EPiServer;
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
        private readonly ProductFactory _productFactory;
        private readonly IPublishedStateAssessor _publishedStateAssessor;
        private readonly ILogger _log = LogManager.GetLogger(typeof(ProductExportService));

        public IExportState ExportState { get; set; }

        public ProductExportService(
            IAssociationRepository associationRepository,
            GroupDefinitionRepository<AssociationGroupDefinition> associationGroupRepository,
            ReferenceConverter referenceConverter,
            IContentLoader contentLoader,
            ProductFactory productFactory,
            IPublishedStateAssessor publishedStateAssessor)
        {
            _associationRepository = associationRepository;
            _associationGroupRepository = associationGroupRepository;
            _referenceConverter = referenceConverter;
            _configuration = KachingConfiguration.Instance;
            _contentLoader = contentLoader;
            _productFactory = productFactory;
            _publishedStateAssessor = publishedStateAssessor;
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

        public void DeleteProducts(ICollection<EntryContentBase> catalogEntries)
        {
            if (catalogEntries == null ||
                catalogEntries.Count == 0)
            {
                return;
            }

            IEnumerable<string> catalogCodes = catalogEntries.Select(c => c.Code.SanitizeKey());

            // Call the external endpoint asynchronously and return immediately.
            Task.Factory.StartNew(() =>
                APIFacade.DeleteAsync(
                        catalogCodes,
                        _configuration.ProductsImportUrl)
                    .ConfigureAwait(false));
        }

        public void DeleteSingleVariantProduct(VariationContent variant)
        {
            _log.Information("DeleteSingleVariantProduct: " + variant.Code);

            if (!_configuration.ProductsImportUrl.IsValidProductsImportUrl())
            {
                _log.Information("Skipped single variant product delete because url is not valid: " +
                                 _configuration.ProductsImportUrl);
                return;
            }

            // Bail if not published
            if (!_publishedStateAssessor.IsPublished(variant))
            {
                _log.Information("Skipped single variant product delete because it's not yet published");
                return;
            }

            var ids = new List<string>();
            ids.Add(variant.Code.SanitizeKey());

            // Call the external endpoint asynchronously and return immediately.
            Task.Factory.StartNew(() =>
                APIFacade.DeleteAsync(
                        ids,
                        _configuration.ProductsImportUrl)
                    .ConfigureAwait(false));
        }

        public void DeleteChildProducts(NodeContent category)
        {
            _log.Information("DeleteChildProducts: " + category.Code);

            var tags = ParentTagsForCategory(category);
            var categories = new List<NodeContent>();
            categories.Add(category);
            // TODO - getting product ids here is enough.
            var products = BuildKachingProducts(categories, tags);
            var ids = products.Select(p => p.Id.SanitizeKey()).ToArray();

            // Call the external endpoint asynchronously and return immediately.
            Task.Factory.StartNew(() =>
                APIFacade.DeleteAsync(
                        ids,
                        _configuration.ProductsImportUrl)
                    .ConfigureAwait(false));
        }

        public void DeleteProductAssets(ICollection<EntryContentBase> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductAssetsImportUrl.IsValidProductAssetsImportUrl())
            {
                return;
            }

            // Call the external endpoint asynchronously and return immediately.
            Task.Factory.StartNew(() =>
                APIFacade.DeleteAsync(
                        entries
                            .OfType<ProductContent>()
                            .Select(e => e.Code.SanitizeKey()),
                        _configuration.ProductAssetsImportUrl)
                    .ConfigureAwait(false));
        }

        public void ExportProductAssets(ICollection<ProductContent> entries)
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

            foreach (var batch in entries
                .Batch(BatchSize))
            {
                var assets = new Dictionary<string, ICollection<ProductAsset>>(BatchSize);
                foreach (var entry in batch)
                {
                    if (entry.CommerceMediaCollection?.Count == 0)
                    {
                        continue;
                    }

                    assets.Add(
                        entry.Code.SanitizeKey(),
                        _productFactory.BuildKaChingProductAssets(entry).ToArray());
                }
                APIFacade.Post(
                    new { assets },
                    _configuration.ProductAssetsImportUrl);
            }
        }

        public void DeleteProductRecommendations(ICollection<EntryContentBase> entries)
        {
            if (entries == null || entries.Count == 0)
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
                foreach (var batch in entries
                    .OfType<ProductContent>()
                    .Batch(BatchSize))
                {
                    // Call the external endpoint asynchronously and return immediately.
                    Task.Factory.StartNew(() =>
                        APIFacade.DeleteAsync(
                                batch.Select(s => s.Code.SanitizeKey()),
                                _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" +
                                associationGroup.Name.SanitizeKey())
                            .ConfigureAwait(false));
                }
            }
        }

        public void ExportProduct(ProductContent product, string deletedVariantCode)
        {
            var configuration = KachingConfiguration.Instance;
            _log.Information("ExportProduct: " + product.Code);

            // Bail if not published
            if (!_publishedStateAssessor.IsPublished(product))
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
            PostKachingProducts(products, _configuration.ProductsImportUrl);
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

            ExportProductRecommendations(catalog, ExportState);
        }

        public void ExportProductRecommendations(NodeContentBase node, IExportState exportState)
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

            ExportProductRecommendations(entryRefs, exportState);
        }

        public void ExportProductRecommendations(IEnumerable<ContentReference> entryLinks, IExportState exportState)
        {
            if (!_configuration.ProductRecommendationsImportUrl.IsValidProductRecommendationsImportUrl())
            {
                return;
            }

            IEnumerable<EntryContentBase> entries = _contentLoader
                .GetItems(entryLinks, CultureInfo.InvariantCulture)
                .OfType<EntryContentBase>();

            ExportProductRecommendations(entries, exportState);
        }

        public void ExportProductRecommendations(IEnumerable<EntryContentBase> entries, IExportState exportState)
        {
            if (!_configuration.ProductRecommendationsImportUrl.IsValidProductRecommendationsImportUrl())
            {
                return;
            }

            var allAssociations = new HashSet<Association>();
            var entriesToDelete = new HashSet<EntryContentBase>(ContentComparer.Default);

            foreach (EntryContentBase entry in entries
                .Distinct(ContentComparer.Default)
                .OfType<EntryContentBase>())
            {
                var associations = (ICollection<Association>)_associationRepository.GetAssociations(entry.ContentLink);
                if (associations.Count == 0)
                {
                    entriesToDelete.Add(entry);
                }
                else
                {
                    foreach (Association association in associations)
                    {
                        allAssociations.Add(association);
                    }
                }
            }

            foreach (var associationsByGroup in allAssociations
                .GroupBy(a => a.Group.Name))
            {
                if (exportState != null)
                {
                    exportState.Action = "Exported";
                    exportState.ModelName = $"product associations ({associationsByGroup.Key})";
                    exportState.Total = associationsByGroup.Count();
                    exportState.Uploaded = 0;
                }

                var recommendationGroups = associationsByGroup
                    .GroupBy(a => a.Source)
                    .Select(g => _productFactory.BuildKaChingRecommendationGroup(g.Key, g.ToArray()))
                    .Where(x => x != null);

                foreach (var group in recommendationGroups
                    .Batch(BatchSize))
                {
                    APIFacade.Post(
                        new { products = group },
                        _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" + associationsByGroup.Key.SanitizeKey());

                    if (exportState != null)
                    {
                        exportState.Uploaded += group.Count;
                    }
                }
            }

            if (entriesToDelete.Count == 0)
            {
                return;
            }

            foreach (var associationGroup in _associationGroupRepository.List())
            {
                if (exportState != null)
                {
                    exportState.Action = "Deleted";
                    exportState.ModelName = $"product associations ({associationGroup.Name})";
                    exportState.Total = entriesToDelete.Count;
                    exportState.Uploaded = 0;
                }

                foreach (var batch in entriesToDelete
                    .Select(c => c.Code.SanitizeKey())
                    .Batch(BatchSize))
                {
                    // Call the external endpoint asynchronously and return immediately.
                    Task.Factory.StartNew(() =>
                        APIFacade.DeleteObjectAsync(
                                batch,
                                _configuration.ProductRecommendationsImportUrl + "&recommendation_id=" + associationGroup.Name.SanitizeKey())
                            .ConfigureAwait(false));

                    if (exportState != null)
                    {
                        exportState.Uploaded += batch.Count;
                    }
                }
            }
        }

        public void ExportChildProducts(NodeContent category)
        {
            _log.Information("ExportChildProducts: " + category.Code);

            // Since Ka-ching uses tags for category hierachy we need to go
            // up the tree of nodes to find the correct tags for the product
            var tags = ParentTagsForCategory(category);
            var categories = new List<NodeContent>();
            categories.Add(category);
            var products = BuildKachingProducts(categories, tags);

            PostKachingProducts(products, _configuration.ProductsImportUrl);
        }

        private void ExportAllProducts(string url)
        {
            var root = _contentLoader.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            var first = root.FirstOrDefault();
            var children = _contentLoader.GetChildren<NodeContent>(first.ContentLink);
            var products = BuildKachingProducts(children, new List<string>());

            if (ExportState != null)
            {
                ExportState.Action = "Exported";
                ExportState.ModelName = "products";
                ExportState.Total = products.Count;
            }

            PostKachingProducts(products, url);
        }

        private void ExportAllProductAssets()
        {
            var assets = new Dictionary<string, IEnumerable<ProductAsset>>();
            var entriesWithoutAssets = new List<string>();

            var root = _contentLoader.GetChildren<CatalogContent>(_referenceConverter.GetRootLink());
            BuildKachingProductAssets(root.FirstOrDefault(), assets, entriesWithoutAssets);

            if (assets.Count == 0 && entriesWithoutAssets.Count == 0)
            {
                return;
            }

            if (!_configuration.ProductAssetsImportUrl.IsValidProductAssetsImportUrl())
            {
                return;
            }

            ResetState(false);
            ExportState.Busy = true;
            ExportState.Action = "Exported";
            ExportState.ModelName = "product assets";
            ExportState.Total = assets.Count + entriesWithoutAssets.Count;

            // Push assets groups for products that have assets.
            foreach (var batch in assets.Batch(BatchSize))
            {
                APIFacade.Post(
                    new {assets = batch},
                    _configuration.ProductAssetsImportUrl);

                ExportState.Uploaded += batch.Count;
            }

            // Delete asset groups for products that have no assets (even if they do not exist in Ka-ching).
            // This is to enforce no assets for products that have no assets, even if an individual deletion was missed earlier.
            ExportState.Action = "Deleted";

            foreach (var batch in entriesWithoutAssets.Batch(BatchSize))
            {
                // Call the external endpoint asynchronously and return immediately.
                Task.Factory.StartNew(() =>
                    APIFacade.DeleteAsync(
                            batch,
                            _configuration.ProductAssetsImportUrl)
                        .ConfigureAwait(false));


                ExportState.Uploaded += batch.Count;
            }
        }

        private IList<Product> BuildKachingProducts(IEnumerable<NodeContent> nodes, IList<string> tags)
        {
            var configuration = KachingConfiguration.Instance;
            var kachingProducts = new List<Product>();

            foreach (var node in nodes)
            {
                var nextTags = new List<string>();
                nextTags.Add(node.Code.SanitizeKey());
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
                        if (!_publishedStateAssessor.IsPublished(product))
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
                                childEntry.Code.SanitizeKey());
                        }
                        else
                        {
                            productAssets.Add(
                                childEntry.Code.SanitizeKey(),
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
                result.Add(category.Code.SanitizeKey());
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
                result.Add(ancestor.Code.SanitizeKey());
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
            ExportState.Action = string.Empty;
            ExportState.ModelName = string.Empty;
        }
    }
}