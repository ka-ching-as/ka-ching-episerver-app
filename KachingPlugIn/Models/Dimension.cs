using System.Collections.Generic;

namespace KachingPlugIn.Models
{
    public class Dimension
    {
        public L10nString Name { get; set; }
        public string Id { get; set; }
        public List<DimensionValue> Values { get; set; }
    }
}
