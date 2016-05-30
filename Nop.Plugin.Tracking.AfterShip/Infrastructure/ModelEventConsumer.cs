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
            if (string.IsNullOrWhiteSpace(args.TrackingNumber)) return;
            var connection = new ConnectionAPI(_settings.ApiKey);
            var order = args.Order;
            var customer = args.Order.Customer;
            var customerFullName = string.Format("{0} {1}", order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName).Trim();

            //create the new tracker
            var track = new AftershipAPI.Tracking(args.TrackingNumber)
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
                    EntityId = args.Id,
                    Key = SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = "True"
                });
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = args.Id,
                    Key = SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = args.TrackingNumber
                });
            }
            catch (WebException ex)
            {
                _logger.Error(string.Format("Cannot resolve registration tracker with number - {0}", args.TrackingNumber), new Exception(ex.Message));
            }
        }

        private void OnShipmentChange(Shipment args)
        {
            if (string.IsNullOrWhiteSpace(args.TrackingNumber)) return;

            var genericAttrs = _genericAttributeService.GetAttributesForEntity(args.Id, "Shipment");
            // Events for tracking is not yet registered. We register and save this entry in the generic attributes
            if (genericAttrs.Any(g => g.Value.Equals(args.TrackingNumber))) return;

            var connection = new ConnectionAPI(_settings.ApiKey);
            var order = args.Order;
            var customer = args.Order.Customer;
            var customerFullName = string.Format("{0} {1}", order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName).Trim();

            //remove the old tracker
            var oldTrackAttr = genericAttrs.First(g => g.Key.Equals(SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME));
            if (oldTrackAttr != null && !oldTrackAttr.Value.Equals(args.TrackingNumber))
            {
                var couriers = connection.detectCouriers(oldTrackAttr.Value);
                foreach (var courier in couriers)
                {
                    var oldTracker = new AftershipAPI.Tracking(oldTrackAttr.Value);
                    oldTracker.slug = courier.slug;
                    try
                    {
                        var deleted = connection.deleteTracking(oldTracker);
                        foreach (var genericAttribute in genericAttrs)
                            _genericAttributeService.DeleteAttribute(genericAttribute);
                        if(deleted)
                            break;
                    }
                    catch (WebException ex)
                    {
                        _logger.Warning(string.Format("Cannot delete tracker with number - {0} and courier name - {1}", 
                            oldTrackAttr.Value, courier.name), new Exception(ex.Message));
                    }
                }
            }
            //create the new tracker
            var track = new AftershipAPI.Tracking(args.TrackingNumber)
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
                    EntityId = args.Id,
                    Key = SHIPMENT_NOTIFICATION_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = "True"
                });
                _genericAttributeService.InsertAttribute(new GenericAttribute
                {
                    EntityId = args.Id,
                    Key = SHIPMENT_TRACK_NUMBER_ATTRIBUTE_NAME,
                    KeyGroup = "Shipment",
                    StoreId = 0,
                    Value = args.TrackingNumber
                });
            }
            catch (WebException ex)
            {
                _logger.Error(string.Format("Cannot registration tracker with number - {0}", 
                    args.TrackingNumber), new Exception(ex.Message));
            }
        }

        private void OnShipmentDeleted(Shipment args)
        {
            try
            {
                var connection = new ConnectionAPI(_settings.ApiKey);
                var track = new AftershipAPI.Tracking(args.TrackingNumber);
                connection.deleteTracking(track);
            }
            catch (WebException ex)
            {
                _logger.Warning(string.Format("Cannot delete tracker with number - {0}", 
                    args.TrackingNumber), new Exception(ex.Message));
            }
        }
        
        #endregion
    }
}