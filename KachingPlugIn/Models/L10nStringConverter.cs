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
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    return new L10nString(reader.ReadAsString());
                case JsonToken.StartObject:
                    var localized = new Dictionary<string, string>();

                    do
                    {
                        // Read the property name and property value.
                        reader.Read();
                        string key = (string)reader.Value;
                        string value = reader.ReadAsString();

                        localized.Add(key, value);
                    } while (reader.TokenType == JsonToken.PropertyName);

                    // Explicitly read the EndObject token.
                    reader.Read();

                    return new L10nString(localized);
                default:
                    throw new InvalidObjectException("Unexpected format of L10nString.");
            }
        }
    }
}
