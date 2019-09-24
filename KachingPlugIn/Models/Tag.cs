using Newtonsoft.Json;

namespace KachingPlugIn.Models
{
    public class Tag
    {
        [JsonProperty(PropertyName = "tag")]
        public string TagValue { get; set;  }
        public L10nString Name { get; set; }

        public override string ToString()
        {
            return TagValue;
        }
    }
}
