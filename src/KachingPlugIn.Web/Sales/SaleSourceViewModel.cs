using System;
using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleSourceViewModel
    {
        [JsonProperty("cashier_id")]
        public Guid CashierId { get; set; }

        [JsonProperty("cashier_name")]
        public string CashierName { get; set; }

        [JsonProperty("market_id")]
        public string MarketId { get; set; }

        [JsonProperty("market_name")]
        public string MarketName { get; set; }

        [JsonProperty("register_id")]
        public Guid RegisterId { get; set; }

        [JsonProperty("register_name")]
        public string RegisterName { get; set; }

        [JsonProperty("shop_id")]
        public string ShopId { get; set; }

        [JsonProperty("shop_name")]
        public string ShopName { get; set; }
    }
}
