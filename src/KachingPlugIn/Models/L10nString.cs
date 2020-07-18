using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    [JsonConverter(typeof(L10nStringConverter))]
    public class L10nString
    {
        public static readonly L10nString EmptyLocalized = new L10nString(new Dictionary<string, string>(0));

        public string Unlocalized { get; }
        public IDictionary<string, string> Localized { get; }

        public L10nString(string value)
        {
            Unlocalized = value;
        }

        public L10nString(IDictionary<string, string> value)
        {
            Localized = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is L10nString l10NString)
            {
                if (l10NString.Unlocalized != null && Unlocalized != null && l10NString.Localized == null && Localized == null)
                {
                    return l10NString.Unlocalized == Unlocalized;
                }

                if (l10NString.Localized != null && Localized != null && l10NString.Unlocalized == null && Unlocalized == null)
                {
                    if (l10NString.Localized == Localized) return true;
                    if (l10NString.Localized == null || Localized == null) return false;
                    if (l10NString.Localized.Count != Localized.Count) return false;

                    var valueComparer = EqualityComparer<string>.Default;

                    foreach (var kvp in l10NString.Localized)
                    {
                        if (!Localized.TryGetValue(kvp.Key, out string value2)) return false;
                        if (!valueComparer.Equals(kvp.Value, value2)) return false;
                    }
                    return true;
                }

                if (l10NString.Localized == null && l10NString.Unlocalized == null && Localized == null && Unlocalized == null)
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
