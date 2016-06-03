using System.Web.Mvc;
using Nop.Plugin.Tracking.AfterShip.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tracking.AfterShip.Controllers
{
    public class TrackingAfterShipController : BasePluginController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public TrackingAfterShipController(
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._localizationService = localizationService;
        }

        #endregion

        #region Actions

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var afterShipSettings = this._settingService.LoadSetting<AfterShipSettings>();
            var model = new ConfigurationModel
            {
                AfterShipUsername = afterShipSettings.AfterShipUsername,
                AllowCustomerNotification = afterShipSettings.AllowCustomerNotification,
                ApiKey = afterShipSettings.ApiKey
            };

            return View("~/Plugins/Tracking.AfterShip/Views/TrackingAfterShip/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            //load settings
            var afterShipSettings = _settingService.LoadSetting<AfterShipSettings>();
            afterShipSettings.AllowCustomerNotification = model.AllowCustomerNotification;
            afterShipSettings.AfterShipUsername = model.AfterShipUsername;
            afterShipSettings.ApiKey = model.ApiKey;

            _settingService.SaveSetting(afterShipSettings);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}