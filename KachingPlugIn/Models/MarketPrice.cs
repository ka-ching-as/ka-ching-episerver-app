using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class MarketPriceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            MarketPrice l10n = (MarketPrice)value;
            if (l10n.MarketSpecific != null)
            {
                serializer.Serialize(writer, l10n.MarketSpecific);
            }
            else
            {
                writer.WriteValue(l10n.Single);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Only handling serialization for now
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(MarketPrice);
        }
    }


    [JsonConverter(typeof(MarketPriceConverter))]
    public class MarketPrice
    {
        public decimal Single { get; set; }
        public Dictionary<string, decimal> MarketSpecific { get; set; }

        public MarketPrice(decimal Value)
        {
            this.Single = Value;
        }

        public MarketPrice(Dictionary<string, decimal> Value)
        {
            this.MarketSpecific = Value;
        }
    }
}
