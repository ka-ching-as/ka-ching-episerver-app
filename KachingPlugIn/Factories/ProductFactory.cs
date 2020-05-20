using EPiServer.Commerce.Catalog.ContentTypes;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using EPiServer.Web.Routing;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Commerce.Catalog.Linking;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;
using EPiServer.Web;

namespace KachingPlugIn.Factories
{
    public class ProductFactory
    {
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

        public Product BuildKaChingProduct(ProductContent product, ICollection<string> tags, string skipVariantCode)
        {
            var kachingProduct = new Product();

            kachingProduct.Id = product.Code;
            kachingProduct.Name = _l10nStringFactory.LocalizedProductName(product);

            /* ---------------------------- */
            /* Barcode is just a string, but needs to fit the barcodes on the products in the store.
             * Don't use this one if your product has variants - see below for assignment on variant */
            /* ---------------------------- */
            // kachingProduct.Barcode = product.Barcode;

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
                Configuration.Instance().ExportSingleVariantAsProduct)
            {
                // If the product has only one variant and ExportSingleVariantAsProduct is configured to true,
                // then put all variant properties on the product instead.
                var variant = variants.First();

                kachingProduct.Id = variant.Code;
                //kachingProduct.Barcode = variant.Barcode;
                kachingProduct.Name = _l10nStringFactory.LocalizedVariantName(variant);
                kachingProduct.RetailPrice = MarketPriceForCode(variant.Code);

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
                    kachingVariant.Id = variant.Code;
                    //kachingVariant.Barcode = variant.Barcode;

                    var variantName = _l10nStringFactory.LocalizedVariantName(variant);
                    if (!variantName.Equals(kachingProduct.Name))
                    {
                        kachingVariant.Name = variantName;
                    }

                    kachingVariant.RetailPrice = MarketPriceForCode(variant.Code);

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