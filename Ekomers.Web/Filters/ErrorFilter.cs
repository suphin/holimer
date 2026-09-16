using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Ekomers.Filters
{
    public class ErrorFilter : Attribute, IAsyncExceptionFilter
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ErrorFilter> _logger;
        private readonly SystemErrorLogWriter _systemErrorLogWriter;

        public ErrorFilter(
            IUserService userService,
            IWebHostEnvironment environment,
            ILogger<ErrorFilter> logger,
            SystemErrorLogWriter systemErrorLogWriter)
        {
            _userService = userService;
            _environment = environment;
            _logger = logger;
            _systemErrorLogWriter = systemErrorLogWriter;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var action = context.RouteData.Values["action"] as string ?? string.Empty;
            var controller = context.RouteData.Values["controller"] as string ?? string.Empty;
            var requestId = context.HttpContext.TraceIdentifier;

            _logger.LogError(
                context.Exception,
                "İstek işlenirken hata oluştu. Controller: {Controller}, Action: {Action}, RequestId: {RequestId}",
                controller,
                action,
                requestId);

            await _systemErrorLogWriter.WriteAsync(
                context.Exception,
                context.HttpContext,
                requestId,
                controller,
                action);

            try
            {
                _userService.AddUserActivityLog(
                    controller,
                    action,
                    "Error => " + context.Exception.Message,
                    "OnException",
                    context.HttpContext.User.Identity?.Name ?? string.Empty);
            }
            catch (Exception logError)
            {
                _logger.LogWarning(logError, "Kullanıcı aktivite hata kaydı oluşturulamadı.");
            }

            var canSeeTechnicalDetails = _environment.IsDevelopment() || context.HttpContext.User.IsInRole("Admin");
            var model = ErrorViewModel.FromException(
                context.Exception,
                context.HttpContext.Request.Path,
                requestId,
                canSeeTechnicalDetails);

            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Home/Error.cshtml",
                StatusCode = StatusCodes.Status500InternalServerError,
                ViewData = new ViewDataDictionary<ErrorViewModel>(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary())
                {
                    Model = model
                }
            };
            context.ExceptionHandled = true;
        }
    }
}
