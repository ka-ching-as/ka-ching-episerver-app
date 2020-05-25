using System.Collections.Generic;

namespace KachingPlugIn.Models
{
    public class Product
    {
        public L10nString Name { get; set; }
        public L10nString Description { get; set; }
        public MarketPrice RetailPrice { get; set; }
        public MarketPrice SalePrice { get; set; }
        public string Id { get; internal set; }
        public string Barcode { get; set; }

        public string ImageUrl { get; set; }

        public ICollection<Variant> Variants { get; set; }
        public ICollection<Dimension> Dimensions { get; set; }

        public IDictionary<string, object> Attributes { get; } = new Dictionary<string, object>(0);
        public IDictionary<string, bool> Tags { get; set; }
    }
}
