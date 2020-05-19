using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.Web.Routing;
using KachingPlugIn.Configuration;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KachingPlugIn.Factories
{
    public class ProductFactory
    {
        private readonly IContentLoader _contentLoader;
        private readonly IMarketService _marketService;
        private readonly IUrlResolver _urlResolver;
        private readonly IPriceService _priceService;
        private readonly IRelationRepository _relationRepository;
        private readonly L10nStringFactory _l10nStringFactory;

        public ProductFactory(
            IContentLoader contentLoader,
            IMarketService marketService,
            IUrlResolver urlResolver,
            IPriceService priceService,
            IRelationRepository relationRepository,
            L10nStringFactory l10NStringFactory)
        {
            _contentLoader = contentLoader;
            _marketService = marketService;
            _urlResolver = urlResolver;
            _priceService = priceService;
            _relationRepository = relationRepository;
            _l10nStringFactory = l10NStringFactory;
        }

        public Product BuildKaChingProduct(
            ProductContent product,
            ICollection<string> tags,
            KachingConfiguration configuration,
            string skipVariantCode)
        {
            var kachingProduct = new Product();

            kachingProduct.Id = product.Code;
            kachingProduct.Name = _l10nStringFactory.LocalizedProductName(product);
            kachingProduct.Barcode = GetPropertyStringValue(product, configuration.SystemMappings.BarcodeMetaField);

            foreach (var mapping in configuration.AttributeMappings.Cast<AttributeMappingElement>())
            {
                object value = GetAttributeValue(product, mapping.MetaField);
                if (value == null)
                {
                    continue;
                }

                kachingProduct.Attributes[mapping.AttributeId] = value;
            }

            /* ---------------------------- */
            /* Example of dimension and dimension value construction from the Quicksilver site. */
            /* ---------------------------- */

            //var dimensions = new List<Dimension>();
            //var sizeDimension = new Dimension();
            //sizeDimension.Id = "Size";
            //sizeDimension.Name = new L10nString("Size");
            //sizeDimension.Values = new List<DimensionValue>();
            //foreach (var size in product.AvailableSizes)
            //{
            //    var sizeValue = new DimensionValue();
            //    sizeValue.Id = size.ToLower().Replace(' ', '_');
            //    sizeValue.Name = new L10nString(size);
            //    sizeDimension.Values.Add(sizeValue);
            //}
            //dimensions.Add(sizeDimension);

            //var colorDimension = new Dimension();
            //colorDimension.Id = "Color";
            //colorDimension.Name = new L10nString("Color");
            //colorDimension.Values = new List<DimensionValue>();
            //foreach (var color in product.AvailableColors)
            //{
            //    var colorValue = new DimensionValue();
            //    colorValue.Id = color.ToLower().Replace(' ', '_');
            //    colorValue.Name = new L10nString(color);
            //    colorDimension.Values.Add(colorValue);
            //}
            //dimensions.Add(colorDimension);

            //kachingProduct.Dimensions = dimensions;

            CommerceMedia productImage = product.CommerceMediaCollection.FirstOrDefault();
            if (productImage != null)
            {
                string absoluteUrl = _urlResolver.GetUrl(
                    productImage.AssetLink,
                    string.Empty,
                    new UrlResolverArguments { ForceCanonical = true });
                kachingProduct.ImageUrl = absoluteUrl;
            }

            IEnumerable<ContentReference> variantRefs = _relationRepository
                .GetChildren<ProductVariation>(product.ContentLink)
                .Select(r => r.Child);

            ICollection<VariationContent> variants = _contentLoader
                .GetItems(variantRefs, LanguageSelector.MasterLanguage())
                .OfType<VariationContent>()
                .ToArray();

            if (variants.Count == 1 &&
                configuration.ExportSingleVariantAsProduct)
            {
                // If the product has only one variant and ExportSingleVariantAsProduct is configured to true,
                // then put all variant properties on the product instead.
                var variant = variants.First();

                kachingProduct.Id = variant.Code;
                kachingProduct.Barcode = GetPropertyStringValue(variant, configuration.SystemMappings.BarcodeMetaField);
                kachingProduct.Name = _l10nStringFactory.LocalizedVariantName(variant);
                kachingProduct.RetailPrice = MarketPriceForCode(variant.Code);

                foreach (var mapping in configuration.AttributeMappings.Cast<AttributeMappingElement>())
                {
                    object value = GetAttributeValue(variant, mapping.MetaField);
                    if (value == null)
                    {
                        continue;
                    }

                    kachingProduct.Attributes[mapping.AttributeId] = value;
                }

                if (kachingProduct.ImageUrl == null)
                {
                    CommerceMedia variantImage = variant.CommerceMediaCollection.FirstOrDefault();
                    if (variantImage != null)
                    {
                        string absoluteUrl = _urlResolver.GetUrl(
                            variantImage.AssetLink,
                            string.Empty,
                            new UrlResolverArguments { ForceCanonical = true });
                        kachingProduct.ImageUrl = absoluteUrl;
                    }
                }
            }
            else if (variants.Count > 0)
            {
                var kachingVariants = new List<Variant>(variants.Count);

                foreach (var variant in variants)
                {
                    if (skipVariantCode != null &&
                        skipVariantCode == variant.Code)
                    {
                        continue;
                    }

                    var kachingVariant = new Variant();
                    kachingVariant.Id = variant.Code;
                    //kachingVariant.Barcode = GetPropertyStringValue(variant, configuration.FieldMappings.BarcodeField);

                    var variantName = _l10nStringFactory.LocalizedVariantName(variant);
                    if (!variantName.Equals(kachingProduct.Name))
                    {
                        kachingVariant.Name = variantName;
                    }

                    kachingVariant.RetailPrice = MarketPriceForCode(variant.Code);

                    foreach (var mapping in configuration.AttributeMappings.Cast<AttributeMappingElement>())
                    {
                        object value = GetAttributeValue(variant, mapping.MetaField);
                        if (value == null)
                        {
                            continue;
                        }

                        kachingVariant.Attributes[mapping.AttributeId] = value;
                    }

                    CommerceMedia variantImage = variant.CommerceMediaCollection.FirstOrDefault();
                    if (variantImage != null)
                    {
                        string absoluteUrl = _urlResolver.GetUrl(
                            variantImage.AssetLink,
                            string.Empty,
                            new UrlResolverArguments { ForceCanonical = true });
                        kachingVariant.ImageUrl = absoluteUrl;
                    }

                    if (kachingProduct.ImageUrl == null)
                    {
                        kachingProduct.ImageUrl = kachingVariant.ImageUrl;
                    }

                    kachingVariants.Add(kachingVariant);
                }

                kachingProduct.Variants = kachingVariants;
            }

            /* ---------------------------- */
            /* Assign tags to enable category navigation */
            /* ---------------------------- */

            kachingProduct.Tags = new Dictionary<string, bool>();
            foreach (var tag in tags)
            {
                kachingProduct.Tags[tag] = true;
            }

            /* ---------------------------- */
            /* Example of how to construct a description string from the Quicksilver site */
            /* ---------------------------- */

            //var html = product.Description.ToEditString();
            //var htmlDoc = new HtmlDocument();
            //htmlDoc.LoadHtml(html);
            //var text = htmlDoc.DocumentNode.InnerText;

            //string longText = null;
            //if (product.LongDescription != null)
            //{
            //    htmlDoc.LoadHtml(product.LongDescription.ToEditString());
            //    longText = htmlDoc.DocumentNode.InnerText;
            //}

            //var description = text;
            //if (longText != null)
            //{
            //    description = longText + "\n\n" + text;
            //}
            //kachingProduct.Description = new L10nString(description);

            return kachingProduct;
        }

        public ProductMetadata ProductMetadata()
        {
            var markets = _marketService.GetAllMarkets();
            var kachingMarkets = new Dictionary<string, bool>();
            foreach (var market in markets)
            {
                var marketKey = market.MarketId.Value.KachingCompatibleKey();
                kachingMarkets[marketKey] = true;
            }

            var result = new ProductMetadata();
            result.Channels = new Dictionary<string, bool>();
            result.Channels["pos"] = true;
            result.Markets = kachingMarkets;
            return result;
        }

        private object GetAttributeValue(CatalogContentBase content, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            PropertyData data = content.Property[propertyName];
            if (data == null || data.IsNull)
            {
                return null;
            }

            switch (data.Value)
            {
                case int numberData:
                    return numberData;
                case bool booleanData:
                    return booleanData ? "true" : "false";
                case string stringData:
                    return new AttributeTextValue(stringData);
                default:
                    return null;
            }
        }

        private string GetPropertyStringValue(ContentData content, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            PropertyData data = content.Property[propertyName];
            if (data == null || data.IsNull)
            {
                return null;
            }

            return data.Value is string stringValue
                ? stringValue
                : null;
        }

        private MarketPrice MarketPriceForCode(string code)
        {
            /* ---------------------------- */
            /* Find prices for all markets  */
            /* ---------------------------- */

            var markets = _marketService.GetAllMarkets();
            var prices = new Dictionary<string, decimal>();
            foreach (var market in markets)
            {
                var filter = new PriceFilter
                {
                    Currencies = new[] { market.DefaultCurrency }
                };

                var price = _priceService.GetPrices(
                    market.MarketId,
                    DateTime.Now,
                    new CatalogKey(code),
                    filter)
                    .FirstOrDefault();
                if (price != null && market.MarketId != null)
                {
                    var marketKey = market.MarketId.Value.KachingCompatibleKey();
                    prices[marketKey] = price.UnitPrice.Amount;
                }
            }

            return prices.Count == 0
                ? null
                : new MarketPrice(prices);
        }
    }
}