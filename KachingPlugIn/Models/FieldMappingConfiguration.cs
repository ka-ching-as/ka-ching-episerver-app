using System.Collections.Generic;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace KachingPlugIn.Models
{
    public class FieldMappingConfiguration : IDynamicData
    {
        public Identity Id { get; set; }

        public string BarcodeField { get; set; }
        public IDictionary<string, string> AttributeFields { get; set; } = new Dictionary<string, string>(0);
    }
}
