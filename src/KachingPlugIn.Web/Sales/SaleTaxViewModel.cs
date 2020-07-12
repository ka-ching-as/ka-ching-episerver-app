using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleTaxViewModel
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("rate")]
        public decimal Rate { get; set; }

        [JsonProperty("tax_name")]
        public string TaxName { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
