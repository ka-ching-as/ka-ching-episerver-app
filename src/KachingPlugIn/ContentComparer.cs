using System.Collections.Generic;
using EPiServer.Core;

namespace KachingPlugIn
{
    public class ContentComparer : IEqualityComparer<IContent>
    {
        public static readonly ContentComparer Default = new ContentComparer();

        public bool Equals(IContent x, IContent y)
        {
            if (x == y)
            {
                return true;
            }

            return x != null &&
                   y != null &&
                   x.ContentLink.Equals(y.ContentLink);
        }

        public int GetHashCode(IContent obj)
        {
            return obj.ContentLink.GetHashCode();
        }
    }
}
