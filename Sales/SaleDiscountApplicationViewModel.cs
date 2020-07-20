﻿using System;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleDiscountApplicationViewModel
    {
        [JsonProperty("basket")]
        public bool Basket { get; set; }

        [JsonProperty("line_item")]
        public Guid LineItemId { get; set; }
    }
}
