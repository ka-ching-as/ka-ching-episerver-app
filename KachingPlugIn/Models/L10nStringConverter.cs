using System;
using System.Collections.Generic;
using Mediachase.Commerce.Catalog.Exceptions;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class L10nStringConverter : JsonConverter<L10nString>
    {
        public override void WriteJson(
            JsonWriter writer,
            L10nString value,
            JsonSerializer serializer)
        {
            if (value.Localized != null)
            {
                serializer.Serialize(writer, value.Localized);
            }
            else
            {
                writer.WriteValue(value.Unlocalized);
            }
        }

        public override L10nString ReadJson(
            JsonReader reader,
            Type objectType,
            L10nString existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return new L10nString((string) reader.Value);
                case JsonToken.StartObject:
                    var localized = serializer.Deserialize<IDictionary<string, string>>(reader);

                    return new L10nString(localized);
                default:
                    throw new InvalidObjectException("Unexpected format of L10nString.");
            }
        }
    }
}
