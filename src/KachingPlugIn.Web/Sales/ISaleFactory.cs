using EPiServer.Commerce.Order;

namespace KachingPlugIn.Web.Sales
{
    public interface ISaleFactory
    {
        IPurchaseOrder CreatePurchaseOrder(
            SaleViewModel kachingSale);
    }
}