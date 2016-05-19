using System;
using System.Linq;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Models.Order;

namespace Nop.Plugin.Tracking.AfterShip.Filters
{
    public class AfterShipTrackerWebFilterAttribute : ActionFilterAttribute
    {
        #region Methods

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var countryService = EngineContext.Current.Resolve<ICountryService>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var shipmentService = EngineContext.Current.Resolve<IShipmentService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();
            var storeService = EngineContext.Current.Resolve<IStoreService>();
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var shippingSettings = settingService.LoadSetting<ShippingSettings>();

            var model = filterContext.Controller.ViewData.Model as ShipmentDetailsModel;

            if (model != null && !model.ShipmentStatusEvents.Any())
            {
                var shipment = shipmentService.GetShipmentById(model.Id);

                if (!String.IsNullOrEmpty(shipment.TrackingNumber))
                {
                    var storeScope = this.GetActiveStoreScopeConfiguration(storeService, workContext);
                    var afterShipSettings = settingService.LoadSetting<AfterShipSettings>(storeScope);
                    var order = shipment.Order;
                    var srcm = shippingService.LoadShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);

                    if (srcm != null &&
                        srcm.PluginDescriptor.Installed &&
                        srcm.IsShippingRateComputationMethodActive(shippingSettings))
                    {
                        var shipmentTracker = srcm.ShipmentTracker;
                        if (shipmentTracker == null)
                        {
                            shipmentTracker = new AfterShipTracker(afterShipSettings);

                            model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
                            if (shippingSettings.DisplayShipmentEventsToCustomers)
                            {
                                var shipmentEvents = shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                                if (shipmentEvents != null)
                                {
                                    foreach (var shipmentEvent in shipmentEvents)
                                    {
                                        var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel();
                                        var shipmentEventCountry = countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                        shipmentStatusEventModel.Country = shipmentEventCountry != null
                                                                               ? shipmentEventCountry.GetLocalized(x => x.Name)
                                                                               : shipmentEvent.CountryCode;
                                        shipmentStatusEventModel.Date = shipmentEvent.Date;
                                        shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                        shipmentStatusEventModel.Location = shipmentEvent.Location;
                                        model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                                    }
                                }
                                filterContext.Controller.ViewData.Model = model;
                            }
                        }
                    }
                }
            }
            base.OnResultExecuting(filterContext);
        }

        #endregion

        #region Utilites

        private int GetActiveStoreScopeConfiguration(IStoreService storeService, IWorkContext workContext)
        {
            //ensure that we have 2 (or more) stores
            if (storeService.GetAllStores().Count < 2)
                return 0;

            var storeId = workContext.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration);
            var store = storeService.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }

        #endregion
    }
}