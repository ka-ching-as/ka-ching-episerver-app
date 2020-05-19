using System;
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
            // Only handling serialization for now
            throw new NotImplementedException();
        }
    }
}
