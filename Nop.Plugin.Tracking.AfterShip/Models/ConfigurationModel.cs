using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Tracking.AfterShip.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Tracking.AfterShip.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Tracking.AfterShip.AfterShipUsername")]
        public string AfterShipUsername { get; set; }
        public bool AfterShipUsername_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Tracking.AfterShip.AllowCustomerNotification")]
        public bool AllowCustomerNotification { get; set; }
        public bool AllowCustomerNotification_OverrideForStore { get; set; }
    }
}