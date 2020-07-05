using EPiServer.ServiceLocation;

namespace KachingPlugIn.Sales
{
    /// <summary>
    /// Default order number generator for orders coming from Ka-ching to Episerver.
    /// The default implementation pads the Ka-ching sequence number with up to 7 zeros and prefixes with "PO".
    /// </summary>
    [ServiceConfiguration(typeof(IKachingOrderNumberGenerator), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultKachingOrderNumberGenerator : IKachingOrderNumberGenerator
    {
        public string GenerateOrderNumber(int sequenceNumber)
        {
            return "PO" + sequenceNumber.ToString("00000000");
        }

        public string GenerateReturnOrderNumber(int sequenceNumber)
        {
            return "RMA" + sequenceNumber.ToString("00000000");
        }
    }
}
