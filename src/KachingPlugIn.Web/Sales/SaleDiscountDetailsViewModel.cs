using System;
using KachingPlugIn.Models;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleDiscountDetailsViewModel
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public L10nString Name { get; set; }

        [JsonProperty("application")]
        public SaleDiscountApplicationViewModel Application { get; set; }
    }
}
