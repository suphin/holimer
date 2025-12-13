using Ekomers.Data.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ekomers.Filters
{
    public class ErrorFilter : Attribute, IExceptionFilter
    {
        private readonly IUserService _userService;

        public ErrorFilter(IUserService userService)
        {
            _userService = userService;
        }

        public void OnException(ExceptionContext context)
        {
            context.ExceptionHandled = true;
            string userName = String.Empty,
                action = context.RouteData.Values["action"] as string ?? String.Empty,
                controller = context.RouteData.Values["controller"] as string ?? String.Empty;

            string hataMesaji = "Error => " + context.Exception.Message;

            //User.Identity!.Name

            //if (context.Controller is BaseController ctrl)
            //{
            //    userName = ctrl.CurrentUser.UserName;
            //}

            _userService.AddUserActivityLog(controller, action, hataMesaji, "OnException", userName);

            context.Result = new RedirectResult("/Home/Error");
        }
    }
}
