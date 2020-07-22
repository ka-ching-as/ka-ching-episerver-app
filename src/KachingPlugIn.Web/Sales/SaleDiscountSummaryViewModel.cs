using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleDiscountSummaryViewModel
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("discount")]
        public SaleDiscountDetailsViewModel Discount { get; set; }
    }
}
