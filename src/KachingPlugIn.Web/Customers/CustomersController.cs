using System.Collections.Generic;
using System.Web.Http;
using KachingPlugIn.Web.Owin;
using Mediachase.Commerce.Customers;

namespace KachingPlugIn.Web.Customers
{
    [HostAuthentication(KachingApiDefaults.AuthenticationType)]
    [Authorize(Roles = "Kaching")]
    public class CustomerLookupController : ApiController
    {
        private readonly CustomerViewModelFactory _customerViewModelFactory;

        public CustomerLookupController(CustomerViewModelFactory customerViewModelFactory)
        {
            _customerViewModelFactory = customerViewModelFactory;
        }

        [HttpGet]
        public IHttpActionResult GetAllCustomers(string q)
        {
            IEnumerable<CustomerContact> contacts = CustomerContext.Current.GetContactsByPattern(q);

            IEnumerable<CustomerViewModel> viewModels = _customerViewModelFactory.Create(contacts);

            return Json(viewModels);
        }
    }
}
