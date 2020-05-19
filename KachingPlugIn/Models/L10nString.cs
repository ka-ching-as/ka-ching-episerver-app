using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    [JsonConverter(typeof(L10nStringConverter))]
    public class L10nString
    {
        public string Unlocalized { get; }
        public Dictionary<string, string> Localized { get; }

        public L10nString(string value)
        {
            Unlocalized = value;
        }

        public L10nString(Dictionary<string, string> value)
        {
            Localized = value;
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
