using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KachingPlugIn.Sales
{
    public class SaleSummaryViewModel
    {
        [JsonProperty("base_price")]
        public decimal BasePrice { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("customer")]
        public SaleCustomerViewModel Customer { get; set; }

        [JsonProperty("is_return")]
        public bool IsReturn { get; set; }

        [JsonProperty("line_items")]
        public ICollection<SaleLineItemViewModel> LineItems { get; set; }

        [JsonProperty("margin")]
        public decimal Margin { get; set; }

        [JsonProperty("margin_total")]
        public decimal MarginTotal { get; set; }

        //[JsonProperty("purchase_type")]
        //public object PurchaseType { get; set; }

        [JsonProperty("return_reference")]
        public string ReturnReference { get; set; }

        [JsonProperty("sales_tax_amount")]
        public decimal SalesTaxAmount { get; set; }

        [JsonProperty("sub_total")]
        public decimal Subtotal { get; set; }

        [JsonProperty("taxes")]
        public ICollection<SaleTaxViewModel> Taxes { get; set; }

        [JsonProperty("total")]
        public decimal Total { get; set; }

        [JsonProperty("total_discounts")]
        public decimal TotalDiscount { get; set; }

        [JsonProperty("total_tax_amount")]
        public decimal TotalTax { get; set; }

        [JsonProperty("vat_amount")]
        public decimal VatAmount { get; set; }
    }
}
