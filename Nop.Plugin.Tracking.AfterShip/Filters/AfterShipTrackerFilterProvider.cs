using System.Collections.Generic;
using System.Web.Mvc;

namespace Nop.Plugin.Tracking.AfterShip.Filters
{
    public class AfterShipTrackerFilterProvider : IFilterProvider
    {
        #region Methods

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            if (actionDescriptor.ControllerDescriptor.ControllerType == typeof(Web.Controllers.OrderController)
                && actionDescriptor.ActionName.Equals("ShipmentDetails"))
            {
                return new[]
                {
                    new Filter(new AfterShipTrackerWebFilterAttribute(), FilterScope.Action, null)
                };
            }

            if (actionDescriptor.ControllerDescriptor.ControllerType == typeof(Admin.Controllers.OrderController)
                && actionDescriptor.ActionName.Equals("ShipmentDetails"))
            {
                return new[]
                {
                    new Filter(new AfterShipTrackerAdminFilterAttribute(), FilterScope.Action, null)
                };
            }

            return new Filter[] { };
        }

        #endregion
    }
}