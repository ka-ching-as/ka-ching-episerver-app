using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Commerce.Storage;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;

namespace KachingPlugIn.Sales
{
    [ServiceConfiguration(typeof(ISaleFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultSaleFactory : ISaleFactory
    {
        private readonly IMarketService _marketService;
        private readonly IOrderGroupFactory _orderGroupFactory;
        private readonly IKachingOrderNumberGenerator _orderNumberGenerator;
        private readonly IPurchaseOrderProvider _purchaseOrderProvider;

        public DefaultSaleFactory(
            IMarketService marketService,
            IOrderGroupFactory orderGroupFactory,
            IKachingOrderNumberGenerator orderNumberGenerator,
            IPurchaseOrderProvider purchaseOrderProvider)
        {
            _marketService = marketService;
            _orderGroupFactory = orderGroupFactory;
            _orderNumberGenerator = orderNumberGenerator;
            _purchaseOrderProvider = purchaseOrderProvider;
        }

        public virtual IPurchaseOrder CreatePurchaseOrder(
            SaleViewModel kachingSale)
        {
            IMarket market = _marketService.GetMarket(kachingSale.Source.MarketId);
            if (market == null || !market.IsEnabled)
            {
                throw new InvalidOperationException(
                    $"The MarketId ('{kachingSale.Source.MarketId}') is not recognized in Episerver Commerce.");
            }

            CustomerContact customerContact = null;
            if (kachingSale.Summary.Customer?.Identifier != null)
            {
                customerContact = CustomerContext.Current.GetContactById(
                    Guid.Parse(kachingSale.Summary.Customer.Identifier));
                if (customerContact?.PrimaryKeyId == null)
                {
                    throw new InvalidOperationException(
                        $"The customer ('{kachingSale.Summary.Customer.Identifier}') is not recognized in Episerver Commerce.");
                }
            }

            IPurchaseOrder purchaseOrder = _purchaseOrderProvider.Create(
                customerContact?.PrimaryKeyId.Value ?? Guid.Empty,
                "Default");
            if (purchaseOrder == null)
            {
                throw new InvalidOperationException(
                    "The purchase order could not be initialized.");
            }

            PopulateMetaFields(purchaseOrder, market, kachingSale);
            SetCashier(purchaseOrder, kachingSale);

            IOrderForm orderForm = purchaseOrder.GetFirstForm();
            PopulateMetaFields(purchaseOrder, orderForm, market, kachingSale);
            SetCashier(orderForm, kachingSale);

            IShipment shipment = CreateShipment(purchaseOrder, customerContact, kachingSale);
            PopulateMetaFields(shipment, market, kachingSale);
            SetCashier(shipment, kachingSale);

            foreach (var kachingLineItem in kachingSale.Summary.LineItems)
            {
                ILineItem lineItem = CreateLineItem(purchaseOrder, shipment, kachingSale, kachingLineItem);

                PopulateMetaFields(lineItem, market, kachingSale, kachingLineItem);
                SetCashier(lineItem, kachingSale);

                shipment.LineItems.Add(lineItem);
            }

            IPayment cashPayment = null;
            foreach (var kachingPayment in kachingSale.Payments)
            {
                switch (kachingPayment.PaymentType)
                {
                    // If the cash payment has cashback and/or rounding, adjust the cash payment amount.
                    // Cashback occurs when, for example, a customer pays for 120 DKK with 150 DKK in cash,
                    // and gets 30 DKK back from the merchant. Episerver does not need this information separately,
                    // so here we adjusts the cash payment entity with all corrections.
                    case "cash.cashback" when cashPayment != null:
                    case "cash.rounding" when cashPayment != null:
                        cashPayment.Amount += kachingPayment.Amount;
                        continue;
                }

                IPayment payment = CreatePayment(purchaseOrder, customerContact, kachingSale, kachingPayment);
                if (kachingPayment.PaymentType == "cash")
                {
                    cashPayment = payment;
                }
                
                PopulateMetaFields(payment, market, kachingSale, kachingPayment);
                SetCashier(payment, kachingSale);

                orderForm.Payments.Add(payment);
            }

            return purchaseOrder;
        }

        protected virtual ILineItem CreateLineItem(
            IPurchaseOrder purchaseOrder,
            IShipment shipment,
            SaleViewModel kachingSale,
            SaleLineItemViewModel kachingLineItem)
        {
            ILineItem lineItem = _orderGroupFactory.CreateLineItem(
                kachingLineItem.VariantId ?? kachingLineItem.Id,
                purchaseOrder);

            decimal quantity = kachingLineItem.UnitCount ?? kachingLineItem.Quantity;
            lineItem.Quantity = quantity;
            lineItem.PlacedPrice = kachingLineItem.RetailPrice / quantity;

            // Get the specific discount amount of this line item (discount that were applied directly to the line item).
            decimal lineItemDiscount =
                kachingLineItem.Discounts?
                    .Where(d => !(d.Discount?.Application.Basket ?? false))
                    .Sum(d => d.Amount) ?? 0;

            // Get the amount of the order level discount that this line item contributes
            // (the order level discount are spread out on all line items).
            decimal orderLevelDiscount =
                kachingLineItem.Discounts?
                    .Where(d => d.Discount?.Application.Basket ?? false)
                    .Sum(d => d.Amount) ?? 0;

            lineItem.SetEntryDiscountValue(lineItemDiscount);
            lineItem.SetOrderDiscountValue(orderLevelDiscount);

            return lineItem;
        }

        protected virtual IPayment CreatePayment(
            IPurchaseOrder purchaseOrder,
            CustomerContact customerContact,
            SaleViewModel kachingSale,
            SalePaymentViewModel kachingPayment)
        {
            IPayment payment = _orderGroupFactory.CreatePayment(purchaseOrder);
            payment.Amount = kachingPayment.Amount;
            payment.CustomerName = kachingSale.Summary.Customer?.Name;
            payment.ProviderTransactionID = kachingPayment.Identifier.ToString();
            payment.Status = (kachingPayment.Success ? PaymentStatus.Processed : PaymentStatus.Failed).ToString();
            payment.TransactionType = TransactionType.Sale.ToString();

            switch (kachingPayment.PaymentType)
            {
                case "cash":
                case "mobilepay":
                case "mobilepay.integration":
                    payment.PaymentType = PaymentType.Other;
                    break;
                case "card.external":
                case "card.izettle":
                case "card.payex":
                case "card.verizone":
                    payment.PaymentType = PaymentType.CreditCard;
                    break;
                case "giftcard":
                    payment.PaymentType = PaymentType.GiftCard;
                    break;
                case "customer_account.integration":
                case "invoice.erp":
                case "invoice.external":
                    payment.PaymentType = PaymentType.Invoice;
                    break;
            }

            return payment;
        }

        protected virtual IShipment CreateShipment(
            IPurchaseOrder purchaseOrder,
            CustomerContact customerContact,
            SaleViewModel kachingSale)
        {
            SaleCustomerViewModel kachingCustomer = kachingSale.Summary.Customer;
            IShipment shipment = purchaseOrder.GetFirstShipment();

            // By default, use the shipping address supplied by Ka-ching.
            // Override this method if you need to use the customer's registered address.
            if (kachingCustomer == null)
            {
                return shipment;
            }

            int lastNameIndex = kachingCustomer.Name.LastIndexOf(' ');
            string firstName = kachingCustomer.Name.Substring(0, lastNameIndex-1);
            string lastName = kachingCustomer.Name.Substring(lastNameIndex, kachingCustomer.Name.Length - lastNameIndex);

            IOrderAddress shippingAddress = _orderGroupFactory.CreateOrderAddress(purchaseOrder);
            shippingAddress.FirstName = firstName;
            shippingAddress.LastName = lastName;
            shippingAddress.Line1 = kachingCustomer.Street;
            shippingAddress.PostalCode = kachingCustomer.PostalCode;
            shippingAddress.City = kachingCustomer.City;
            shippingAddress.CountryName = kachingCustomer.Country;
            shippingAddress.CountryCode = kachingCustomer.CountryCode;
            shippingAddress.DaytimePhoneNumber = kachingCustomer.Phone;
            shippingAddress.Email = kachingCustomer.Email;

            return shipment;
        }

        /// <summary>
        /// Populates default fields on the purchase order. Override to populate custom purchase order fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            IPurchaseOrder purchaseOrder,
            IMarket market,
            SaleViewModel kachingSale)
        {
            purchaseOrder.Currency = kachingSale.CurrencyCode;
            purchaseOrder.MarketId = market.MarketId;
            purchaseOrder.MarketName = market.MarketName;
            purchaseOrder.PricesIncludeTax = market.PricesIncludeTax;

            purchaseOrder.OrderNumber = _orderNumberGenerator.GenerateOrderNumber(kachingSale.SequenceNumber);
            purchaseOrder.Properties["CustomerName"] = kachingSale.Summary.Customer?.Name;
        }

        /// <summary>
        /// Populates default fields on the first line item. Override to populate custom line item fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            ILineItem lineItem,
            IMarket market,
            SaleViewModel kachingSale,
            SaleLineItemViewModel kachingLineItem)
        {
        }

        /// <summary>
        /// Populates default fields on the first order form. Override to populate custom order form fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            IPurchaseOrder purchaseOrder,
            IOrderForm orderForm,
            IMarket market,
            SaleViewModel kachingSale)
        {
            orderForm.Name = purchaseOrder.Name;
        }

        /// <summary>
        /// Populates default fields on the payment. Override to populate custom payment fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            IPayment payment,
            IMarket market,
            SaleViewModel kachingSale,
            SalePaymentViewModel kachingPayment)
        {
        }

        /// <summary>
        /// Populates default fields on the first shipment. Override to populate custom shipment fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            IShipment shipment,
            IMarket market,
            SaleViewModel kachingSale)
        {
        }

        /// <summary>
        /// Converts a CustomerAddress entity to an OrderAddress entity.
        /// </summary>
        protected virtual IOrderAddress ConvertToAddress(
            CustomerContact contact,
            CustomerAddress address,
            IOrderGroup orderGroup)
        {
            if (address == null)
            {
                return null;
            }

            var orderAddress = _orderGroupFactory.CreateOrderAddress(orderGroup);
            orderAddress.City = address.City;
            orderAddress.CountryCode = address.CountryCode;
            orderAddress.CountryName = address.CountryName;
            orderAddress.DaytimePhoneNumber = address.DaytimePhoneNumber;
            orderAddress.Email = !string.IsNullOrWhiteSpace(address.Email) ? address.Email : contact.Email;
            orderAddress.EveningPhoneNumber = address.EveningPhoneNumber;
            orderAddress.FirstName = address.FirstName;
            orderAddress.LastName = address.LastName;
            orderAddress.Line1 = address.Line1;
            orderAddress.Line2 = address.Line2;
            orderAddress.Organization = address.Organization;
            orderAddress.PostalCode = address.PostalCode;
            orderAddress.RegionName = address.RegionName;
            orderAddress.RegionCode = address.RegionCode;

            return orderAddress;
        }

        /// <summary>
        /// Sets the CreatorId and ModifierId properties to the initials of the seller for good records.
        /// Also sets the Created and Modified date properties to the exact date and time of the sale,
        /// regardless of the time it was received in Episerver.
        /// </summary>
        protected void SetCashier(
            IExtendedProperties entity,
            SaleViewModel sale)
        {
            SetCashier(entity, sale.Timing.Timestamp, sale.Source.CashierName);
        }

        /// <summary>
        /// Sets the CreatorId and ModifierId properties to the initials of the seller for good records.
        /// Also sets the Created and Modified date properties to the exact date and time of the sale,
        /// regardless of the time it was received in Episerver.
        /// </summary>
        protected void SetCashier(
            IExtendedProperties entity,
            DateTime dateTime,
            string cashierName)
        {
            // All the order system objects we care about have both IExtendedProperties and MetaObject in common.
            // So we can cast the entity by interface to class, to get access to the four properties below.
            if (!(entity is MetaObject metaObject))
            {
                return;
            }

            metaObject.Created = dateTime;
            metaObject.CreatorId = cashierName;
            metaObject.Modified = dateTime;
            metaObject.ModifierId = cashierName;
        }
    }
}
