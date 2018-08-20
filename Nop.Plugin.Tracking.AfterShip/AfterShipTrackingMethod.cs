using Nop.Core;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;

namespace Nop.Plugin.Tracking.AfterShip
{
    public class AfterShipTrackingMethod : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public AfterShipTrackingMethod(ISettingService settingService, 
            IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey", "API Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey.Hint", "Specify AfterShip API Key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername", "AfterShip Username");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername.Hint", "Specify AfterShip Username.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification", "Allow Customer Notification");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification.Hint", "Check to allow customer email\\sms notification from AfterShip service.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Pending", "New shipments added that are pending to track, or new shipments without tracking information available yet.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InfoReceived", "Carrier has received request from shipper and is about to pick up the shipment.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InTransit", "Carrier has accepted or picked up shipment from shipper. The shipment is on the way.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.OutForDelivery", "Carrier is about to deliver the shipment , or it is ready to pickup.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.AttemptFail", "Carrier attempted to deliver but failed, and usually leaves a notice and will try to delivery again.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Delivered", "The shipment was delivered successfully.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Expired", "Shipment has no tracking information for 7 days since added, or has no further updates for 30 days since last update.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Exception", "Custom hold, undelivered, returned shipment to sender or any shipping exceptions.");

            //settings
            var settings = new AfterShipSettings
            {
                AllowCustomerNotification = true,
                AfterShipUsername = "MyAfterShipUsername",
                ApiKey = "MyAfterShipAPIKey"
            };

            this._settingService.SaveSetting(settings);
            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            this._settingService.DeleteSetting<AfterShipSettings>();

            //locales
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.ApiKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AfterShipUsername.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.AllowCustomerNotification.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Pending");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InfoReceived");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.InTransit");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.OutForDelivery");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.AttemptFail");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Delivered");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Expired");
            _localizationService.DeletePluginLocaleResource("Plugins.Tracking.AfterShip.Status.Exception");

            base.Uninstall();
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/TrackingAfterShip/Configure";
        }

        #endregion
    }
}