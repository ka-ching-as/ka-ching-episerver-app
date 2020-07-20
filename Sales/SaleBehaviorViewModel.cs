using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleBehaviorViewModel
    {
        [JsonProperty("shipping")]
        public SaleShippingViewModel Shipping { get; set; }
    }
}
