using System;
using System.Linq;
using EPiServer.Commerce.Order;
using EPiServer.Commerce.Order.Internal;
using EPiServer.Commerce.Storage;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using KachingPlugIn.Helpers;
using Mediachase.Commerce;
using Mediachase.Commerce.Customers;
using Mediachase.Commerce.Markets;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus;

namespace KachingPlugIn.Web.Sales
{
    [ServiceConfiguration(typeof(ISaleFactory), Lifecycle = ServiceInstanceScope.Singleton)]
    public class DefaultSaleFactory : ISaleFactory
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(DefaultSaleFactory));
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

            orderForm.Shipments.Clear();

            decimal total = 0;

            foreach (var groupedLineItems in kachingSale.Summary
                .LineItems
                .GroupBy(li => li.EcomId))
            {
                var shippingLineItem = groupedLineItems.FirstOrDefault(li => li.Behavior?.Shipping != null);
                var kachingShipping = shippingLineItem?.Behavior?.Shipping;

                total += shippingLineItem != null ? shippingLineItem.Total : 0;

                IShipment shipment = CreateShipment(
                    purchaseOrder,
                    orderForm,
                    customerContact,
                    kachingShipping,
                    kachingSale);
                orderForm.Shipments.Add(shipment);

                PopulateMetaFields(shipment, market, kachingShipping, kachingSale);
                SetCashier(shipment, kachingSale);

                foreach (var kachingLineItem in groupedLineItems)
                {
                    // Skip special behavior line items. These are:
                    // shipping - handled above
                    // giftcard or voucher purchase
                    // giftcard or voucher use (can be a line item if they are taxed at point of sale)
                    // expenses
                    // customer account deposit
                    // container deposit
                    //
                    // We could consider including container deposit behavior
                    // but for now we just skip that along with the rest.
                    if (kachingLineItem.Behavior != null)
                    {
                        continue;
                    }

                    ILineItem lineItem = CreateLineItem(purchaseOrder, shipment, kachingSale, kachingLineItem);
                    shipment.LineItems.Add(lineItem);

                    PopulateMetaFields(lineItem, market, kachingSale, kachingLineItem);
                    SetCashier(lineItem, kachingSale);

                    total += kachingLineItem.Total;
                }
            }

            IPayment payment = CreatePayment(purchaseOrder, customerContact, kachingSale, total);
            PopulateMetaFields(payment, market, kachingSale);
            SetCashier(payment, kachingSale);

            orderForm.Payments.Add(payment);

            return purchaseOrder;
        }

        protected virtual ILineItem CreateLineItem(
            IPurchaseOrder purchaseOrder,
            IShipment shipment,
            SaleViewModel kachingSale,
            SaleLineItemViewModel kachingLineItem)
        {
            ILineItem lineItem = _orderGroupFactory.CreateLineItem(
                kachingLineItem.VariantId.DesanitizeKey() ?? kachingLineItem.Id.DesanitizeKey(),
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
            decimal amount)
        {
            IPayment payment = _orderGroupFactory.CreatePayment(purchaseOrder);
            payment.Amount = amount;
            payment.CustomerName = kachingSale.Summary.Customer?.Name;
            payment.Status = PaymentStatus.Processed.ToString();
            payment.TransactionType = TransactionType.Sale.ToString();
            payment.PaymentType = PaymentType.Other;

            return payment;
        }

        protected virtual IShipment CreateShipment(
            IPurchaseOrder purchaseOrder,
            IOrderForm orderForm,
            CustomerContact customerContact,
            SaleShippingViewModel kachingShipping,
            SaleViewModel kachingSale)
        {
            IShipment shipment = _orderGroupFactory.CreateShipment(purchaseOrder);
            shipment.OrderShipmentStatus = kachingShipping != null
                ? OrderShipmentStatus.AwaitingInventory
                : OrderShipmentStatus.Shipped;

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
            SaleViewModel kachingSale)
        {
        }

        /// <summary>
        /// Populates default fields on the first shipment. Override to populate custom shipment fields.
        /// </summary>
        protected virtual void PopulateMetaFields(
            IShipment shipment,
            IMarket market,
            SaleShippingViewModel kachingShipping,
            SaleViewModel kachingSale)
        {
            // By default, use the shipping address supplied by Ka-ching.
            // Override this method if you need to use the customer's registered address.
            if (kachingShipping?.Address != null)
            {
                shipment.ShippingAddress = ConvertToAddress(shipment.ParentOrderGroup, kachingShipping);
            }
            else if (kachingSale.Summary?.Customer != null)
            {
                shipment.ShippingAddress = ConvertToAddress(shipment.ParentOrderGroup, kachingSale.Summary.Customer);
            }

            if (kachingShipping?.MethodId != null)
            {
                try
                {
                    shipment.ShippingMethodId = new Guid(kachingShipping.MethodId);
                }
                catch
                {
                    Logger.Error($"Invalid shipping method id on Ka-ching shipping {kachingShipping.MethodId}");
                }
            }
        }

        protected virtual IOrderAddress ConvertToAddress(
            IOrderGroup orderGroup,
            SaleShippingViewModel shipping)
        {
            if (shipping == null)
            {
                return null;
            }

            var orderAddress = _orderGroupFactory.CreateOrderAddress(orderGroup);
            orderAddress.Id = "Shipping";
            orderAddress.City = shipping.Address.City;
            orderAddress.CountryCode = shipping.Address.CountryCode;
            orderAddress.CountryName = shipping.Address.Country;
            orderAddress.DaytimePhoneNumber = shipping.CustomerInfo.Phone;
            orderAddress.Email = shipping.CustomerInfo.Email;
            orderAddress.FirstName = shipping.Address.Name;
            orderAddress.Line1 = shipping.Address.Street;
            orderAddress.PostalCode = shipping.Address.PostalCode;

            return orderAddress;
        }

        protected virtual IOrderAddress ConvertToAddress(
            IOrderGroup orderGroup,
            SaleCustomerViewModel customer)
        {
            if (customer == null)
            {
                return null;
            }

            var orderAddress = _orderGroupFactory.CreateOrderAddress(orderGroup);
            orderAddress.Id = "Shipping";
            orderAddress.City = customer.City;
            orderAddress.CountryCode = customer.CountryCode;
            orderAddress.CountryName = customer.Country;
            orderAddress.DaytimePhoneNumber = customer.Phone;
            orderAddress.Email = customer.Email;
            orderAddress.FirstName = customer.Name;
            orderAddress.Line1 = customer.Street;
            orderAddress.PostalCode = customer.PostalCode;

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
