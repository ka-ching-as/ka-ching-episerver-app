using System;
using KachingPlugIn.Models;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleDimensionValueViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public L10nString Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
