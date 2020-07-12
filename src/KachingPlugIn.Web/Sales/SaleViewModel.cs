using System.Collections.Generic;
using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleViewModel
    {
        [JsonProperty("base_currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("identifier")]
        public string Identifier { get; set; }

        [JsonProperty("sequence_number")]
        public int SequenceNumber { get; set; }

        [JsonProperty("payments")]
        public ICollection<SalePaymentViewModel> Payments { get; set; }

        [JsonProperty("summary")]
        public SaleSummaryViewModel Summary { get; set; }

        [JsonProperty("source")]
        public SaleSourceViewModel Source { get; set; }

        [JsonProperty("timing")]
        public SaleTimingViewModel Timing { get; set; }

        [JsonProperty("voided")]
        public bool Voided { get; set; }
    }
}
