using System.Collections.Generic;
using EPiServer.Commerce.SpecializedProperties;
using EPiServer.Core;

namespace KachingPlugIn.KachingPlugIn
{
    public class CommerceMediaComparer : IEqualityComparer<CommerceMedia>
    {
        public static readonly CommerceMediaComparer Default = new CommerceMediaComparer();

        public bool Equals(CommerceMedia x, CommerceMedia y)
        {
            return ContentReferenceComparer.IgnoreVersion.Equals(x?.AssetLink, y?.AssetLink);
        }

        public int GetHashCode(CommerceMedia obj)
        {
            return ContentReferenceComparer.IgnoreVersion.GetHashCode();
        }
    }
}
