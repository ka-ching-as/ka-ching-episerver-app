using System.Collections.Generic;

namespace KachingPlugIn.Models
{
    public class Variant
    {
        public L10nString Name { get; set; }
        public MarketPrice RetailPrice { get; set; }
        public MarketPrice SalePrice { get; set; }
        public string Id { get; internal set; }
        public string ImageUrl { get; set; }

        public IDictionary<string, string> Attributes { get; set; }
        public IDictionary<string, string> DimensionValues { get; set; }
    }
}
