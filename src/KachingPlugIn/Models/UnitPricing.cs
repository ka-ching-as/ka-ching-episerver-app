using KachingPlugIn.Models;

namespace KachingPlugIn.Models
{
    public class UnitPricing
    {
        public string Unit { get; set; }
        public MarketPrice CostPricePerUnit { get; set; }
        public MarketPrice RetailPricePerUnit { get; set; }
        public int Multiplicity { get; set; } = 1;
    }
}
