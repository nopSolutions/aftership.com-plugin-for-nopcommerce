using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Domain.Shipping;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Order;

namespace Nop.Plugin.Tracking.AfterShip.Filters
{
    public class AfterShipTrackerAdminFilterAttribute : ActionFilterAttribute
    {
        #region Methods

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var countryService = EngineContext.Current.Resolve<ICountryService>();
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var shipmentService = EngineContext.Current.Resolve<IShipmentService>();
            var shippingService = EngineContext.Current.Resolve<IShippingService>();
            var shippingSettings = settingService.LoadSetting<ShippingSettings>();
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var dateTimeHelper = EngineContext.Current.Resolve<IDateTimeHelper>();            

            var model = (filterContext.Controller as BaseController)?.ViewData.Model as ShipmentDetailsModel;

            if (model != null && !model.ShipmentStatusEvents.Any())
            {
                var shipment = shipmentService.GetShipmentById(model.Id);

                if (shipment.ShippedDateUtc.HasValue)
                    model.ShippedDate = dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                if (shipment.DeliveryDateUtc.HasValue)
                    model.DeliveryDate = dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);

                if (!string.IsNullOrEmpty(shipment.TrackingNumber))
                {
                    var afterShipSettings = settingService.LoadSetting<AfterShipSettings>();
                    var order = shipment.Order;
                    var srcm = shippingService.LoadShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);
                    
                    if (srcm != null &&
                        srcm.PluginDescriptor.Installed &&
                        shippingService.IsShippingRateComputationMethodActive(srcm))
                    {
                        var shipmentTracker = srcm.ShipmentTracker;
                        if (shipmentTracker == null)
                        {
                            shipmentTracker = new AfterShipTracker(afterShipSettings, shipmentService);

                            model.TrackingNumberUrl = shipmentTracker.GetUrl(shipment.TrackingNumber);
                            if (shippingSettings.DisplayShipmentEventsToStoreOwner)
                            {
                                var shipmentEvents = shipmentTracker.GetShipmentEvents(shipment.TrackingNumber);
                                if (shipmentEvents != null)
                                {
                                    foreach (var shipmentEvent in shipmentEvents)
                                    {
                                        var shipmentStatusEventModel = new ShipmentDetailsModel.ShipmentStatusEventModel();
                                        var shipmentEventCountry = countryService.GetCountryByTwoLetterIsoCode(shipmentEvent.CountryCode);
                                        shipmentStatusEventModel.Country = shipmentEventCountry != null
                                                                               ? localizationService.GetLocalized(shipmentEventCountry, x => x.Name)
                                                                               : shipmentEvent.CountryCode;
                                        shipmentStatusEventModel.Date = shipmentEvent.Date;
                                        shipmentStatusEventModel.EventName = shipmentEvent.EventName;
                                        shipmentStatusEventModel.Location = shipmentEvent.Location;
                                        model.ShipmentStatusEvents.Add(shipmentStatusEventModel);
                                    }
                                }

                                ((BaseAdminController)filterContext.Controller).ViewData.Model = model;
                            }
                        }
                    }
                }
            }
            base.OnResultExecuting(filterContext);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
        }

        #endregion
    }
}