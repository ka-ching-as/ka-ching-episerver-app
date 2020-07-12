using EPiServer.Commerce.Order;

namespace KachingPlugIn.Sales
{
    public interface ISaleFactory
    {
        IPurchaseOrder CreatePurchaseOrder(
            SaleViewModel kachingSale);
    }
}