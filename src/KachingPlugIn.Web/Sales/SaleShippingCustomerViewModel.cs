using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleShippingCustomerViewModel
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }
}
