using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class L10nStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            L10nString l10n = (L10nString)value;
            if (l10n.Localized != null)
            {
                serializer.Serialize(writer, l10n.Localized);
            }
            else
            {
                writer.WriteValue(l10n.Unlocalized);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Only handling serialization for now
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(L10nString);
        }
    }


    [JsonConverter(typeof(L10nStringConverter))]
    public class L10nString
    {
        public string Unlocalized { get; set; }
        public Dictionary<string, string> Localized { get; set; }

        public L10nString(string Value)
        {
            this.Unlocalized = Value;
        }

        public L10nString(Dictionary<string, string> Value)
        {
            this.Localized = Value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is L10nString)
            {
                var l10nString = obj as L10nString;
                if (l10nString.Unlocalized != null && Unlocalized != null && l10nString.Localized == null && Localized == null)
                {
                    return l10nString.Unlocalized == Unlocalized;
                }
                if (l10nString.Localized != null && Localized != null && l10nString.Unlocalized == null && Unlocalized == null)
                {
                    if (l10nString.Localized == Localized) return true;
                    if ((l10nString.Localized == null) || (Localized == null)) return false;
                    if (l10nString.Localized.Count != Localized.Count) return false;

                    var valueComparer = EqualityComparer<string>.Default;

                    foreach (var kvp in l10nString.Localized)
                    {
                        string value2;
                        if (!Localized.TryGetValue(kvp.Key, out value2)) return false;
                        if (!valueComparer.Equals(kvp.Value, value2)) return false;
                    }
                    return true;
                }
                if (l10nString.Localized == null && l10nString.Unlocalized == null && Localized == null && Unlocalized == null)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            // Method taken from https://stackoverflow.com/questions/5059994/custom-type-gethashcode
            unchecked
            {
                int result = 37;
                result *= 397;
                if (Unlocalized != null)
                {
                    result += Unlocalized.GetHashCode();
                }

                result *= 397;
                if (Localized != null)
                {
                    result += Localized.GetHashCode();
                }

                return result;
            }
        }
    }
}
