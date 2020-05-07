using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.Logging;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using EPiServer.Web.Routing;
using Mediachase.Commerce;
using Mediachase.Commerce.Catalog;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Pricing;
using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;

namespace KachingPlugIn.Factories
{
    public class ProductFactory
    {
        private readonly IContentLoader _contentLoader;
        private readonly IMarketService _marketService;
        private readonly IUrlResolver _urlResolver;
        private readonly IPriceService _priceService;
        private readonly L10nStringFactory _l10nStringFactory;
        private readonly ILogger _log = LogManager.GetLogger(typeof(ProductFactory));

        public ProductFactory(
            IContentLoader contentLoader,
            IMarketService marketService,
            IUrlResolver urlResolver,
            IPriceService priceService,
            L10nStringFactory l10NStringFactory)
        {
            _contentLoader = contentLoader;
            _marketService = marketService;
            _urlResolver = urlResolver;
            _priceService = priceService;
            _l10nStringFactory = l10NStringFactory;
        }

        public Product BuildKaChingProduct(ProductContent product, IList<string> tags, string skipVariantCode)
        {
            var kachingProduct = new Product();

            /* ---------------------------- */
            /* Assign id */
            /* ---------------------------- */

            kachingProduct.Id = product.Code;

            /* ---------------------------- */
            /* Find name for all localizations */
            /* ---------------------------- */

            kachingProduct.Name = _l10nStringFactory.LocalizedProductName(product);

            /* ---------------------------- */
            /* Find prices for product */
            /* RetailPrice in Ka-ching is including VAT style tax and excluding sales tax style tax */
            /* ---------------------------- */

            kachingProduct.RetailPrice = MarketPriceForCode(product.Code);

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

            var kachingVariants = new List<Variant>();

            var variantLinks = product.GetVariants();
            var variants = variantLinks.Select(x => _contentLoader.Get<VariationContent>(x));
            foreach (var variation in variants)
            {
                if (skipVariantCode != null && skipVariantCode == variation.Code)
                {
                    continue;
                }

                var kachingVariant = new Variant();
                kachingVariant.Id = variation.Code;

                /* ---------------------------- */
                /* Barcode is just a string, but needs to fit the barcodes on the products in the store. */
                /* ---------------------------- */
                // kachingVariant.Barcode = variant.Barcode;

                /* ---------------------------- */
                /* Assign localized variant name if it's different than product name */
                /* ---------------------------- */

                var variantName = _l10nStringFactory.LocalizedVariantName(variation);
                if (!variantName.Equals(kachingProduct.Name))
                {
                    kachingVariant.Name = variantName;
                }

                /* ---------------------------- */
                /* Example of dimension and dimension value assignment from the Quicksilver site. */
                /* ---------------------------- */

                //kachingVariant.DimensionValues = new Dictionary<string, string>();
                //kachingVariant.DimensionValues[colorDimension.Id] = variant.Color.ToLower().Replace(' ', '_');
                //kachingVariant.DimensionValues[sizeDimension.Id] = variant.Size.ToLower().Replace(' ', '_');

                /* ---------------------------- */
                /* Find prices for variant */
                /* RetailPrice in Ka-ching is including VAT style tax and excluding sales tax style tax */
                /* ---------------------------- */

                kachingVariant.RetailPrice = MarketPriceForCode(variation.Code);

                /* ---------------------------- */
                /* Find an image */
                /* ---------------------------- */

                var media = variation.CommerceMediaCollection.FirstOrDefault();
                if (media != null)
                {
                    var content = _contentLoader.Get<IContentMedia>(media.AssetLink);
                    var relativeUrl = _urlResolver.GetUrl(content);
                    kachingVariant.ImageUrl = new UrlBuilder(relativeUrl).ToString();
                }

                // Make sure umbrella product is assigned an image url
                // If references are likely to change without triggering an update in Ka-ching
                // talk to us about enabling image import where Ka-ching downloads 
                // and stores the image to avoid dangling references.
                if (kachingProduct.ImageUrl == null)
                {
                    kachingProduct.ImageUrl = kachingVariant.ImageUrl;
                }

                kachingVariants.Add(kachingVariant);
            }

            if (kachingVariants.Count > 0)
            {
                kachingProduct.Variants = kachingVariants.ToArray();
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

        private MarketPrice MarketPriceForCode(string code)
        {
            /* ---------------------------- */
            /* Find prices for all markets */
            /* ---------------------------- */

            var markets = _marketService.GetAllMarkets();
            var prices = new Dictionary<string, decimal>();
            foreach (var market in markets)
            {
                var filter = new PriceFilter()
                {
                    Currencies = new Currency[] { market.DefaultCurrency }
                };

                var price = _priceService.GetPrices(
                    market.MarketId,
                    DateTime.Now,
                    new CatalogKey(code),
                    filter)
                    .FirstOrDefault();
                if (price != null && market.MarketName != null)
                {
                    var marketKey = market.MarketId.Value.KachingCompatibleKey();
                    prices[marketKey] = price.UnitPrice.Amount;
                }
            }

            if (prices.Count == 0)
            {
                return null;
            }
            return new MarketPrice(prices);
        }
    }
}