using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class RecommendationGroup
    {
        [JsonProperty("product_id")]
        public string ProductId { get; set; }

        [JsonProperty("recommendations")]
        public ICollection<string> Recommendations { get; set; }
    }
}
