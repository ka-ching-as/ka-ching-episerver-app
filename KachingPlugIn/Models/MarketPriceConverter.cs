using System;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class MarketPriceConverter : JsonConverter<MarketPrice>
    {
        public override void WriteJson(
            JsonWriter writer,
            MarketPrice value,
            JsonSerializer serializer)
        {
            if (value.MarketSpecific != null)
            {
                serializer.Serialize(writer, value.MarketSpecific);
            }
            else
            {
                writer.WriteValue(value.Single);
            }
        }

        public override MarketPrice ReadJson(
            JsonReader reader,
            Type objectType,
            MarketPrice existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
