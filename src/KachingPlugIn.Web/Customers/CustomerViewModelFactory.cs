using System.Collections.Generic;
using System.Linq;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;

namespace KachingPlugIn.Web.Customers
{
    [ServiceConfiguration(typeof(CustomerViewModelFactory))]
    public class CustomerViewModelFactory
    {
        public virtual IEnumerable<CustomerViewModel> Create(IEnumerable<CustomerContact> contacts)
        {
            if (contacts == null)
            {
                yield break;
            }

            foreach (CustomerContact contact in contacts)
            {
                CustomerAddress shippingAddress = contact.ContactAddresses.FirstOrDefault(
                    a => (a.AddressType & CustomerAddressTypeEnum.Shipping) == CustomerAddressTypeEnum.Shipping);

                var viewModel = new CustomerViewModel
                {
                    Identifier = contact.PrimaryKeyId.ToString(),
                    Name = contact.FullName,
                    Street = shippingAddress?.Line1 +
                             (!string.IsNullOrWhiteSpace(shippingAddress?.Line2) ? ", "+ shippingAddress.Line2 : null),
                    PostalCode = shippingAddress?.PostalCode,
                    City = shippingAddress?.City,
                    Country = shippingAddress?.CountryName,
                    CountryCode = shippingAddress?.CountryCode,
                    Email = shippingAddress?.Email ?? contact.Email,
                    Phone = shippingAddress?.DaytimePhoneNumber
                };

                yield return viewModel;
            }
        }
    }
} 
