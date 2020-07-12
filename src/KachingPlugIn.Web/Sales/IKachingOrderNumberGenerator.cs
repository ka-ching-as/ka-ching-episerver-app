namespace KachingPlugIn.Web.Sales
{
    public interface IKachingOrderNumberGenerator
    {
        string GenerateOrderNumber(int sequenceNumber);
        string GenerateReturnOrderNumber(int sequenceNumber);
    }
}
