using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.Web;
using EPiServer.Web.Routing;
using KachingPlugIn.Configuration;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KachingPlugIn.Factories
{
    public class ProductFactory
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ProductFactory));
        private readonly IContentLoader _contentLoader;
        private readonly IMarketService _marketService;
        private readonly IUrlResolver _urlResolver;
        private readonly IPriceService _priceService;
        private readonly IRelationRepository _relationRepository;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;
        private readonly L10nStringFactory _l10nStringFactory;

        public ProductFactory(
            IContentLoader contentLoader,
            IMarketService marketService,
            IUrlResolver urlResolver,
            IPriceService priceService,
            IRelationRepository relationRepository,
            ISiteDefinitionResolver siteDefinitionResolver,
            L10nStringFactory l10NStringFactory)
        {
            _contentLoader = contentLoader;
            _marketService = marketService;
            _urlResolver = urlResolver;
            _priceService = priceService;
            _relationRepository = relationRepository;
            _siteDefinitionResolver = siteDefinitionResolver;
            _l10nStringFactory = l10NStringFactory;
        }

        public RecommendationGroup BuildKaChingRecommendationGroup(
            ContentReference entryLink,
            ICollection<Association> associations)
        {
            IDictionary<ContentReference, EntryContentBase> entriesByContentLink = _contentLoader
                .GetItems(
                    associations.Select(x => x.Target).Concat(new[] { entryLink }),
                    CultureInfo.InvariantCulture)
                .OfType<EntryContentBase>()
                .ToDictionary(x => x.ContentLink);

            if (!entriesByContentLink.TryGetValue(entryLink, out EntryContentBase entry))
            {
                return null;
            }

            ICollection<string> childCodes = new List<string>(associations.Count);
            foreach (var targetRef in associations.Select(a => a.Target))
            {
                if (!entriesByContentLink.TryGetValue(targetRef, out EntryContentBase childEntry))
                {
                    continue;
                }

                childCodes.Add(childEntry.Code.KachingCompatibleKey());
            }

            return new RecommendationGroup
            {
                ProductId = entry.Code.KachingCompatibleKey(),
                Recommendations = childCodes
            };
        }

        public IEnumerable<RecommendationGroup> BuildKaChingRecommendationGroups(
            IDictionary<string, ICollection<Association>> associationsByEntry)
        {
            foreach (var kvp in associationsByEntry)
            {
                IDictionary<ContentReference, EntryContentBase> entriesByContentLink = _contentLoader
                    .GetItems(
                        kvp.Value.Select(x => x.Target),
                        CultureInfo.InvariantCulture)
                    .OfType<EntryContentBase>()
                    .ToDictionary(x => x.ContentLink);

                ICollection<string> childCodes = new List<string>(kvp.Value.Count);

                foreach (var targetRef in kvp.Value.Select(a => a.Target))
                {
                    if (!entriesByContentLink.TryGetValue(targetRef, out EntryContentBase childEntry))
                    {
                        continue;
                    }

                    childCodes.Add(childEntry.Code);
                }

                yield return new RecommendationGroup
                {
                    ProductId = kvp.Key,
                    Recommendations = childCodes
                };
            }
        }

        public Product BuildKaChingProduct(
            ProductContent product,
            ICollection<string> tags,
            KachingConfiguration configuration,
            string skipVariantCode)
        {
            var kachingProduct = new Product();

            kachingProduct.Id = product.Code.KachingCompatibleKey();
            kachingProduct.Barcode = GetPropertyStringValue(product, configuration.SystemMappings.BarcodeMetaField);
            kachingProduct.Name = _l10nStringFactory.GetLocalizedString(product, nameof(product.DisplayName));
            kachingProduct.Description = _l10nStringFactory.GetLocalizedString(product, configuration.SystemMappings.DescriptionMetaField);

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
                Uri absoluteUrl = GetAbsoluteUrl(productImage.AssetLink);
                kachingProduct.ImageUrl = absoluteUrl.AbsoluteUri;
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

                kachingProduct.Id = variant.Code.KachingCompatibleKey();
                kachingProduct.Barcode = GetPropertyStringValue(variant, configuration.SystemMappings.BarcodeMetaField);
                kachingProduct.Name = _l10nStringFactory.GetLocalizedString(variant, nameof(variant.DisplayName));

                MarketPrice retailPrice = MarketPriceForCode(variant.Code);
                AddProductPricing(kachingProduct, product, retailPrice, configuration.SystemMappings.PriceUnitMetaField);

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
                        Uri absoluteUrl = GetAbsoluteUrl(variantImage.AssetLink);
                        kachingProduct.ImageUrl = absoluteUrl.AbsoluteUri;
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
                    kachingVariant.Id = variant.Code.KachingCompatibleKey();
                    kachingVariant.Barcode = GetPropertyStringValue(variant, configuration.SystemMappings.BarcodeMetaField);

                    var variantName = _l10nStringFactory.GetLocalizedString(variant, nameof(variant.DisplayName));
                    if (!variantName.Equals(kachingProduct.Name))
                    {
                        kachingVariant.Name = variantName;
                    }

                    if (kachingProduct.UnitPricing == null)
                    {
                        kachingVariant.RetailPrice = MarketPriceForCode(variant.Code);
                    }

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
                        Uri absoluteUrl = GetAbsoluteUrl(variantImage.AssetLink);
                        kachingVariant.ImageUrl = absoluteUrl.AbsoluteUri;
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

            return kachingProduct;
        }

        public ICollection<ProductAsset> BuildKaChingProductAssets(EntryContentBase entryContent)
        {
            if (entryContent.CommerceMediaCollection == null ||
                entryContent.CommerceMediaCollection.Count == 0)
            {
                return null;
            }

            var assets = new List<ProductAsset>(entryContent.CommerceMediaCollection.Count);

            // Load all media assets in one go, for the particular catalog entry.
            IDictionary<ContentReference, MediaData> mediaByContentLink = _contentLoader
                .GetItems(
                    entryContent.CommerceMediaCollection
                        .Distinct(CommerceMediaComparer.Default)
                        .Select(x => x.AssetLink),
                    CultureInfo.InvariantCulture)
                .OfType<MediaData>()
                .ToDictionary(x => x.ContentLink);

            foreach (CommerceMedia commerceMedia in entryContent.CommerceMediaCollection)
            {
                // Look up the referenced asset from the pre-loaded media assets.
                if (!mediaByContentLink.TryGetValue(commerceMedia.AssetLink, out MediaData mediaData))
                {
                    continue;
                }

                Uri absoluteUrl = GetAbsoluteUrl(commerceMedia.AssetLink);

                string mimeType;
                switch (mediaData.MimeType)
                {
                    case "application/pdf":
                        mimeType = "document/pdf";
                        break;
                    case "image/jpeg":
                    case "image/png":
                        mimeType = mediaData.MimeType;
                        break;
                    default:
                        continue;
                }

                var asset = new ProductAsset
                {
                    MimeType = mimeType,
                    Name = new L10nString(mediaData.Name),
                    Url = absoluteUrl.ToString()
                };

                assets.Add(asset);
            }

            return assets;
        }

        public ProductMetadata ProductMetadata()
        {
            var markets = _marketService.GetAllMarkets();
            var kachingMarkets = new Dictionary<string, bool>();

            foreach (var market in markets)
            {
                if (!market.IsEnabled)
                {
                    continue;
                }

                var marketKey = market.MarketId.Value.KachingCompatibleKey();
                kachingMarkets[marketKey] = true;
            }

            var result = new ProductMetadata
            {
                Markets = kachingMarkets,
                Channels = new Dictionary<string, bool>(1)
                {
                    ["pos"] = true
                }
            };

            return result;
        }

        private void AddProductPricing(
            Product kachingProduct,
            CatalogContentBase catalogContent,
            MarketPrice retailPrice,
            string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                kachingProduct.RetailPrice = retailPrice;
                return;
            }

            string priceUnit = GetPropertyStringValue(catalogContent, propertyName);

            string kachingPriceUnit;
            switch (priceUnit?.ToLowerInvariant())
            {
                case "g":
                    kachingPriceUnit = "mass/g";
                    break;
                case "kg":
                    kachingPriceUnit = "mass/kg";
                    break;
                case "m":
                    kachingPriceUnit = "length/m";
                    break;
                case "cm":
                    kachingPriceUnit = "length/cm";
                    break;
                case "mm":
                    kachingPriceUnit = "length/mm";
                    break;
                case "m2":
                    kachingPriceUnit = "area/m2";
                    break;
                case "cm2":
                    kachingPriceUnit = "area/cm2";
                    break;
                case "mm2":
                    kachingPriceUnit = "area/mm2";
                    break;
                case "l":
                    kachingPriceUnit = "volume/l";
                    break;
                case "dl":
                    kachingPriceUnit = "volume/dl";
                    break;
                case "cl":
                    kachingPriceUnit = "volume/cl";
                    break;
                case "ml":
                    kachingPriceUnit = "volume/ml";
                    break;
                default:
                    kachingProduct.RetailPrice = retailPrice;
                    return;
            }

            kachingProduct.UnitPricing = new UnitPricing
            {
                Unit = kachingPriceUnit,
                RetailPricePerUnit = retailPrice
            };
        }

        private Uri GetAbsoluteUrl(ContentReference contentRef)
        {
            string url = _urlResolver.GetUrl(
                contentRef,
                string.Empty,
                new UrlResolverArguments { ForceCanonical = true });

            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri absoluteUrl) &&
                !absoluteUrl.IsAbsoluteUri)
            {
                var siteDefinition = _siteDefinitionResolver.GetByContent(
                    contentRef,
                    true,
                    true);
                absoluteUrl = new Uri(siteDefinition.SiteUrl, absoluteUrl);
            }

            return absoluteUrl;
        }

        private object GetAttributeValue(IContentData content, string propertyName)
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

            switch (data.Type)
            {
                case PropertyDataType.Number:
                    return (int)data.Value;
                case PropertyDataType.Boolean:
                    return (bool)data.Value ? "true" : "false";
                case PropertyDataType.String:
                case PropertyDataType.LongString:
                    // TODO: Support culture-specific strings.
                    return new AttributeTextValue((string)data.Value);
                default:
                    Logger.Warning(
                        "Mapped property ('{0}') has unsupported property type ({1}). Skipping.",
                        propertyName,
                        data.Type);
                    return null;
            }
        }

        private string GetPropertyStringValue(IContentData content, string propertyName)
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
            /* Find prices for all enabled markets  */
            /* ---------------------------- */

            var markets = _marketService.GetAllMarkets();
            var prices = new Dictionary<string, decimal>();

            ICollection<IPriceValue> allPriceValues = _priceService
                .GetPrices(
                    MarketId.Empty,
                    DateTime.UtcNow,
                    new CatalogKey(code),
                    new PriceFilter())
                .ToArray();

            foreach (var market in markets)
            {
                if (!market.IsEnabled)
                {
                    continue;
                }

                var priceValue = allPriceValues.FirstOrDefault(pv =>
                    pv.MarketId == market.MarketId &&
                    pv.UnitPrice.Currency == market.DefaultCurrency);
                if (priceValue == null)
                {
                    continue;
                }

                var marketKey = market.MarketId.Value.KachingCompatibleKey();
                prices[marketKey] = priceValue.UnitPrice.Amount;
            }

            return prices.Count == 0
                ? null
                : new MarketPrice(prices);
        }
    }
}