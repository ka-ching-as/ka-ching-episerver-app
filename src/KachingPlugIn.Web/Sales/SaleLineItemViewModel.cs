using System;
using System.Collections.Generic;
using KachingPlugIn.Models;
using Newtonsoft.Json;

namespace KachingPlugIn.Web.Sales
{
    public class SaleLineItemViewModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("base_price")]
        public decimal BasePrice { get; set; }

        [JsonProperty("behavior")]
        public SaleBehaviorViewModel Behavior { get; set; }

        [JsonProperty("cost_price")]
        public decimal CostPrice { get; set; }

        [JsonProperty("dimensions")]
        public ICollection<SaleDimensionViewModel> Dimensions { get; set; }

        [JsonProperty("discounts")]
        public ICollection<SaleDiscountSummaryViewModel> Discounts { get; set; }

        [JsonProperty("ecom_id")]
        public string EcomId { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }
        
        [JsonProperty("line_item_id")]
        public Guid LineItemId { get; set; }

        [JsonProperty("margin")]
        public decimal Margin { get; set; }

        [JsonProperty("margin_total")]
        public decimal MarginTotal { get; set; }

        [JsonProperty("name")]
        public L10nString Name { get; set; }

        [JsonProperty("product_group")]
        public string ProductGroup { get; set; }

        [JsonProperty("quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("retail_price")]
        public decimal RetailPrice { get; set; }

        [JsonProperty("sales_tax_amount")]
        public decimal SalesTaxAmount { get; set; }

        [JsonProperty("sub_total")]
        public decimal Subtotal { get; set; }
        
        [JsonProperty("stock_location_id")]
        public string StockLocationId { get; set; }

        [JsonProperty("tags")]
        public ICollection<string> Tags { get; set; }

        [JsonProperty("taxes")]
        public ICollection<SaleTaxViewModel> Taxes { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("total_tax_amount")]
        public decimal TotalTax { get; set; }

        [JsonProperty("variant_id")]
        public string VariantId { get; set; }

        [JsonProperty("vat_amount")]
        public decimal VatAmount { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("unit_count")]
        public decimal? UnitCount { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }
}
