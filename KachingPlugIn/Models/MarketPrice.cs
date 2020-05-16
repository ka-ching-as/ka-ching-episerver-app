using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    [JsonConverter(typeof(MarketPriceConverter))]
    public class MarketPrice
    {
        public decimal Single { get; }
        public Dictionary<string, decimal> MarketSpecific { get; }

        public MarketPrice(decimal value)
        {
            Single = value;
        }

        public MarketPrice(Dictionary<string, decimal> value)
        {
            MarketSpecific = value;
        }
    }
}
