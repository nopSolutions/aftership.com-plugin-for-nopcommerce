using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AftershipAPI;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Logging;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure
{
    public class ModelEventConsumer : IConsumer<EntityUpdated<Shipment>>, IConsumer<EntityInserted<Shipment>>, IConsumer<EntityDeleted<Shipment>>
    {
        #region Constants

        private const string SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME = "IsSetNotification";
        private const string SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME = "TrackNumber";

        #endregion

        #region Fields

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly AfterShipSettings _settings;

        #endregion

        #region Ctor

        public ModelEventConsumer(IGenericAttributeService genericAttributeService, IStoreContext storeContext, ILogger logger, AfterShipSettings settings)
        {
            _genericAttributeService = genericAttributeService;
            _storeContext = storeContext;
            _logger = logger;
            _settings = settings;
        }

        #endregion

        #region Methods

        public void HandleEvent(EntityInserted<Shipment> eventMessage)
        {
            OnShipmentInsert(eventMessage.Entity);
        }

        public void HandleEvent(EntityUpdated<Shipment> eventMessage)
        {
            OnShipmentChange(eventMessage.Entity);
        }

        public void HandleEvent(EntityDeleted<Shipment> eventMessage)
        {
            OnShipmentDeleted(eventMessage.Entity);
        }

        #endregion

        #region Utils

        private void OnShipmentInsert(Shipment args)
        {
            if (!string.IsNullOrEmpty(args.TrackingNumber))
                CreateTracking(args);
        }

        private void OnShipmentChange(Shipment args)
        {
            if (string.IsNullOrWhiteSpace(args.TrackingNumber)) return;

            var genericAttrs = _genericAttributeService.GetAttributesForEntity(args.Id, "Shipment");
            // Events for tracking is not yet registered. We register and save this entry in the generic attributes
            if (genericAttrs.Any(g => g.Value.Equals(args.TrackingNumber))) return;

            //remove the old tracker
            var oldTrackAttr = genericAttrs.First(g => g.Key.Equals(SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME));
            if (oldTrackAttr != null && !oldTrackAttr.Value.Equals(args.TrackingNumber))
            {
                if (RemoveTracking(args.TrackingNumber))
                {
                    foreach (var genericAttribute in genericAttrs)
                    {
                        _genericAttributeService.DeleteAttribute(genericAttribute);
                    }
                }
            }

            //create the new tracking in Aftarship
            CreateTracking(args);
        }

        private void OnShipmentDeleted(Shipment args)
        {
            if (!string.IsNullOrEmpty(args.TrackingNumber))
                RemoveTracking(args.TrackingNumber);
        }

        private void CreateTracking(Shipment shipment)
        {
            var connection = new ConnectionAPI(_settings.ApiKey);
            var order = shipment.Order;
            var customer = shipment.Order.Customer;
            var customerFullName = string.Format("{0} {1}", order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName).Trim();

            //create the new tracking
            var track = new AftershipAPI.Tracking(shipment.TrackingNumber)
            {
                customerName = customerFullName,
                orderID = string.Format("ID {0}", order.Id),
                orderIDPath = string.Format("{0}orderdetails/{1}", _storeContext.CurrentStore.Url, order.Id)
            };
            if (_settings.AllowCustomerNotification)
            {
                track.emails = new List<string> { customer.Email };
            }
            try
            {
                connection.createTracking(track);
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = shipment.Id,
                    Key = SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = "True"
                });
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = shipment.Id,
                    Key = SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = shipment.TrackingNumber
                });
            }
            catch (WebException ex)
            {
                _logger.Error(string.Format("Cannot registration tracking with number - {0}",
                    shipment.TrackingNumber), new Exception(ex.Message));
            }
        }

        private bool RemoveTracking(string trackingNumber)
        {
            bool result = false;
            try
            {
                var connection = new ConnectionAPI(_settings.ApiKey);
                var track = new AftershipAPI.Tracking(trackingNumber);
                var couriers = connection.detectCouriers(trackingNumber);
                foreach (var courier in couriers)
                {
                    track.slug = courier.slug;
                    result = connection.deleteTracking(track);
                    if(result)
                        break;
                }
            }
            catch (WebException ex)
            {
                _logger.Warning(string.Format("Cannot delete tracking with number - {0}",
                    trackingNumber), new Exception(ex.Message));
            }
            return result;
        }
        
        #endregion
    }
}