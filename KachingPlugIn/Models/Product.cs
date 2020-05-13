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

        public Dictionary<string, string> Attributes { get; set; }
        public Dictionary<string, bool> Tags { get; set; }
    }

    public class Variant
    {
        public L10nString Name { get; set; }
        public MarketPrice RetailPrice { get; set; }
        public MarketPrice SalePrice { get; set; }
        public string Id { get; internal set; }
        public string ImageUrl { get; set; }
        public Dictionary<string, string> DimensionValues { get; set; }

        public Dictionary<string, string> Attributes { get; set; }
    }

    public class Dimension
    {
        public L10nString Name { get; set; }
        public string Id { get; set; }
        public List<DimensionValue> Values { get; set; }
    }

    public class DimensionValue
    {
        public string Id { get; set; }
        public L10nString Name { get; set; }
        public string ImageUrl { get; set; }
        public string Color { get; set; }
    }


}
