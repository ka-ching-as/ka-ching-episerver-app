using System.Collections.Generic;

namespace KachingPlugIn.Models
{
    public class ProductMetadata
    {
        public Dictionary<string, bool> Channels { get; set; }
        public Dictionary<string, bool> Markets { get; set; }
    }
}