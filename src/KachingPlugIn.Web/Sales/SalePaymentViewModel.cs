using System;
using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SalePaymentViewModel
    {
        [JsonProperty("identifier")]
        public Guid Identifier { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("foreign_currency")]
        public string ForeignCurrency { get; set; }

        [JsonProperty("foreign_currency_amount")]
        public decimal ForeignCurrencyAmount { get; set; }

        [JsonProperty("payment_type")]
        public string PaymentType { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
