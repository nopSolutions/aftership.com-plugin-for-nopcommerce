﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AftershipAPI;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Tracking.AfterShip
{
    public class AfterShipTracker : IShipmentTracker
    {
        #region Fields

        private readonly AfterShipSettings _settings;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public AfterShipTracker(AfterShipSettings settings)
        {
            this._countryService = EngineContext.Current.Resolve<ICountryService>();
            this._localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            this._logger = EngineContext.Current.Resolve<ILogger>();
            this._workContext = EngineContext.Current.Resolve<IWorkContext>();
            this._settings = settings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets if the current tracker can track the tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>True if the tracker can track, otherwise false.</returns>
        public bool IsMatch(string trackingNumber)
        {
            return !string.IsNullOrWhiteSpace(trackingNumber);
        }

        /// <summary>
        /// Gets a url for a page to show tracking info (third party tracking page).
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>A url to a tracking page.</returns>
        public string GetUrl(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(_settings.AfterShipUsername) || _settings.AfterShipUsername.Equals("MyAfterShipUsername"))
                return string.Format("https://track.aftership.com/{0}", trackingNumber);

            return string.Format("https://{0}.aftership.com/{1}", _settings.AfterShipUsername, trackingNumber);
        }

        /// <summary>
        /// Gets all events for a tracking number.
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track.</param>
        /// <returns>List of Shipment Events.</returns>
        public IList<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.Equals("MyAfterShipAPIKey"))
                return new List<ShipmentStatusEvent>();

            if (string.IsNullOrEmpty(trackingNumber))
                return new List<ShipmentStatusEvent>();

            var connection = new ConnectionAPI(_settings.ApiKey);
            var tracker = new AftershipAPI.Tracking(trackingNumber);
            IList<Courier> couriers = null;
            var shipmentStatusList = new List<ShipmentStatusEvent>();
            try
            {
                //use try-catch to ensure exception won't be thrown is web service is not available
                couriers = connection.detectCouriers(trackingNumber, null, null, null, null);
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting couriers information on Aftership", ex);
            }

            if (couriers == null) return shipmentStatusList;

            foreach (var courier in couriers)
            {
                try
                {
                    tracker.slug = courier.slug;
                    tracker = connection.getTrackingByNumber(tracker);
                    if (!tracker.checkpoints.Any()) continue;

                    foreach (var checkpoint in tracker.checkpoints)
                    {
                        var checkpointCountryIso3Code = checkpoint.countryISO3.ToString();
                        var country = _countryService.GetCountryByThreeLetterIsoCode(checkpointCountryIso3Code);
                        var shipmentStatus = new ShipmentStatusEvent
                        {
                            CountryCode = country.TwoLetterIsoCode,
                            Date = Convert.ToDateTime(checkpoint.checkpointTime),
                            EventName = String.Format("{0} ({1})", checkpoint.message, GetStatus(checkpoint)),
                            Location = checkpoint.city
                        };
                        //other properties (not used yet)
                        //checkpoint.checkpointTime;
                        //checkpoint.countryName;
                        //checkpoint.state;
                        //checkpoint.zip;

                        shipmentStatusList.Add(shipmentStatus);
                    }
                    break;
                }
                catch (WebException)
                {
                    _logger.Error(string.Format("Error getting tracking information on Aftership events - {0}",
                        trackingNumber));
                }
            }

            return shipmentStatusList;
        }

        #endregion

        #region Utilites

        private string GetStatus(Checkpoint checkpoint)
        {
            switch (checkpoint.tag)
            {
                case "Pending":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.Pending",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "InfoReceived":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.InfoReceived",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "InTransit":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.InTransit",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "OutForDelivery":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.OutForDelivery",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "AttemptFail":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.AttemptFail",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "Delivered":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.Delivered",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                case "Expired":
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.Expired",
                            _workContext.WorkingLanguage.Id).ResourceValue;
                default:
                    return
                        _localizationService.GetLocaleStringResourceByName("Plugins.Tracking.AfterShip.Status.Exception",
                            _workContext.WorkingLanguage.Id).ResourceValue;
            }
        }

        #endregion
    }
}