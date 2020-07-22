using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using EPiServer.Commerce.Order;
using EPiServer.Logging;
using KachingPlugIn.Web.Owin;

namespace KachingPlugIn.Web.Sales
{
    [HostAuthentication(KachingApiDefaults.AuthenticationType)]
    [Authorize(Roles = "Kaching")]
    public class SalesController : ApiController
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(SalesController));

        private readonly IPurchaseOrderProvider _purchaseOrderProvider;
        private readonly ISaleFactory _saleFactory;

        public SalesController(
            IPurchaseOrderProvider purchaseOrderProvider,
            ISaleFactory saleFactory)
        {
            _purchaseOrderProvider = purchaseOrderProvider;
            _saleFactory = saleFactory;
        }

        [HttpPost]
        public IHttpActionResult Post(IDictionary<string, SaleViewModel> sales)
        {
            SaleViewModel sale = sales?.Values.FirstOrDefault();
            if (sale == null)
            {
                Logger.Error("The request is missing one or more sales objects. Exiting.");

                return BadRequest();
            }

            // Ignore returns and voided sales
            if (sale.Summary.IsReturn || sale.Voided)
            {
                return StatusCode(HttpStatusCode.NoContent);
            }

            try
            {
                IPurchaseOrder purchaseOrder = _saleFactory.CreatePurchaseOrder(sale);
                _purchaseOrderProvider.Save(purchaseOrder);
            }
            catch (InvalidOperationException ex)
            {
                Logger.Error("Error occurred while registering Ka-ching sale in Episerver.", ex);

                return BadRequest();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
