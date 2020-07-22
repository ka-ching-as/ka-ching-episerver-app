using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleBehaviorViewModel
    {
        [JsonProperty("shipping")]
        public SaleShippingViewModel Shipping { get; set; }
    }
}
