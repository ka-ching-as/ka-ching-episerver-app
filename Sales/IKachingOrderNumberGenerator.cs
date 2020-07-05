namespace KachingPlugIn.Sales
{
    public interface IKachingOrderNumberGenerator
    {
        string GenerateOrderNumber(int sequenceNumber);
        string GenerateReturnOrderNumber(int sequenceNumber);
    }
}
