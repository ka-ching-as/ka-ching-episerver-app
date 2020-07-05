using System;
using KachingPlugIn.Models;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleDimensionViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public L10nString Name { get; set; }
        
        [JsonProperty("value")]
        public SaleDimensionValueViewModel Value { get; set; }
    }
}
