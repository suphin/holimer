using Ekomers.Data.Services.IServices;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using NuGet.Protocol;

namespace Ekomers.Filters
{
    public class ActionFilter : Attribute, IActionFilter
    {
        private readonly IUserService _userService;

        public ActionFilter(IUserService userService)
        {
            _userService = userService;
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            string userName = String.Empty,
                action = context.RouteData.Values["action"] as string ?? String.Empty,
                controller = context.RouteData.Values["controller"] as string ?? String.Empty;

            string prms = String.Empty;
            foreach(var key in context.ActionArguments)
            {
                prms += key.Key + " => " + key.Value.ToJson();
            }

            if(context.Controller is BaseController ctrl)
            {
                userName = ctrl.CurrentUser.UserName;
            }

            _userService.AddUserActivityLog(controller, action, prms, "OnActionExecuting", userName);
        }
    }
}
