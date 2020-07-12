using System;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleTimingViewModel
    {
        [JsonProperty("timestamp_string")]
        public DateTime Timestamp { get; set; }
    }
}
