using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class ProductAsset
    {
        [JsonProperty("mime_type")]
        public string MimeType { get; set; }

        [JsonProperty("name")]
        public L10nString Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
