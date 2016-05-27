using System.Web.Mvc;
using Nop.Core;
using Nop.Plugin.Tracking.AfterShip.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Tracking.AfterShip.Controllers
{
    public class TrackingAfterShipController : BasePluginController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public TrackingAfterShipController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService,
            ILocalizationService localizationService)
        {
            _workContext = workContext;
            _storeService = storeService;
            _settingService = settingService;
            _localizationService = localizationService;
        }

        #endregion

        #region Actions

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var afterShipSettings = _settingService.LoadSetting<AfterShipSettings>(storeScope);
            var model = new ConfigurationModel
            {
                AfterShipUsername = afterShipSettings.AfterShipUsername,
                AllowCustomerNotification = afterShipSettings.AllowCustomerNotification,
                ApiKey = afterShipSettings.ApiKey,
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.AfterShipUsername_OverrideForStore = _settingService.SettingExists(afterShipSettings,
                    a => a.AfterShipUsername, storeScope);
                model.AllowCustomerNotification_OverrideForStore = _settingService.SettingExists(afterShipSettings,
                    a => a.AllowCustomerNotification, storeScope);
                model.ApiKey_OverrideForStore = _settingService.SettingExists(afterShipSettings, a => a.ApiKey,
                    storeScope);
            }

            return View("~/Plugins/Tracking.AfterShip/Views/TrackingAfterShip/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var afterShipSettings = _settingService.LoadSetting<AfterShipSettings>(storeScope);
            afterShipSettings.AllowCustomerNotification = model.AllowCustomerNotification;
            afterShipSettings.AfterShipUsername = model.AfterShipUsername;
            afterShipSettings.ApiKey = model.ApiKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.AllowCustomerNotification_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(afterShipSettings, x => x.AllowCustomerNotification, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(afterShipSettings, x => x.AllowCustomerNotification, storeScope);
            if (model.AfterShipUsername_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(afterShipSettings, x => x.AfterShipUsername, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(afterShipSettings, x => x.AfterShipUsername, storeScope);
            if (model.ApiKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(afterShipSettings, x => x.ApiKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(afterShipSettings, x => x.ApiKey, storeScope);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}