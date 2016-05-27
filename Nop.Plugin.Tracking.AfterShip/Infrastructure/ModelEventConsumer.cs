using System.Collections.Generic;
using System.Linq;
using AftershipAPI;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Events;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure
{
    public class ModelEventConsumer : IConsumer<EntityUpdated<Shipment>>, IConsumer<EntityInserted<Shipment>>
    {
        #region Constants

        private const string SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME = "IsSetNotification";

        #endregion

        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStoreContext _storeContext;
        private readonly AfterShipSettings _settings;

        #endregion

        #region Ctor

        public ModelEventConsumer(IGenericAttributeService genericAttributeService, AfterShipSettings settings, IStoreContext storeContext)
        {
            _genericAttributeService = genericAttributeService;
            _storeContext = storeContext;
            _settings = settings;
        }

        #endregion

        #region Methods

        public void HandleEvent(EntityUpdated<Shipment> eventMessage)
        {
            OnShipmentChange(eventMessage.Entity);
        }

        public void HandleEvent(EntityInserted<Shipment> eventMessage)
        {
            OnShipmentChange(eventMessage.Entity);
        }

        #endregion

        #region Utils

        private void OnShipmentChange(Shipment args)
        {
            if (string.IsNullOrWhiteSpace(args.TrackingNumber)) return;

            var genericAttrs = _genericAttributeService.GetAttributesForEntity(args.Id, "Shipment");
            // Events for tracking is not yet registered. We register and save this entry in the generic attributes
            if (genericAttrs.Any(g => g.Key.Equals(SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME))) return;

            var order = args.Order;
            var customer = args.Order.Customer;
            var customerFullName = string.Format("{0} {1}", order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName).Trim();

            var connection = new ConnectionAPI(_settings.ApiKey);
            var track = new AftershipAPI.Tracking(args.TrackingNumber)
            {
                customerName = customerFullName,
                orderID = string.Format("ID {0}", order.Id),
                orderIDPath = string.Format("{0}orderdetails/{1}", _storeContext.CurrentStore.Url, order.Id)
            };
            if (_settings.AllowCustomerNotification)
            {
                track.emails = new List<string> {customer.Email};
            }
            track = connection.createTracking(track);
            
            _genericAttributeService.InsertAttribute(new GenericAttribute
            {
                EntityId = args.Id,
                Key = SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME,
                KeyGroup = "Shipment",
                StoreId = 0,
                Value = "True"
            });
        }

        #endregion
    }
}