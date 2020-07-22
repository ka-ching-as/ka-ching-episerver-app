using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleShippingViewModel
    {
        [JsonProperty("address")]
        public SaleShippingAddressViewModel Address { get; set; }

        [JsonProperty("customer_info")]
        public SaleShippingCustomerViewModel CustomerInfo { get; set; }

        [JsonProperty("method_id")]
        public string MethodId { get; set; }
    }
}
