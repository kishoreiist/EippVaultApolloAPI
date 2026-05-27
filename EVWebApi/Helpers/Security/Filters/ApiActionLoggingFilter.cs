using Microsoft.AspNetCore.Mvc.Filters;

namespace EVWebApi.Helpers.Security.Filters
{
    public class ApiActionLoggingFilter: IActionFilter
    {
        private readonly ILogger<ApiActionLoggingFilter> _logger;

        public ApiActionLoggingFilter(ILogger<ApiActionLoggingFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogInformation(
                "Executing {Controller}.{Action}",
                context.Controller.GetType().Name,
                context.ActionDescriptor.DisplayName);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _logger.LogInformation(
                "Executed {Controller}.{Action} with status {StatusCode}",
                context.Controller.GetType().Name,
                context.ActionDescriptor.DisplayName,
                context.HttpContext.Response.StatusCode);
        }
    }
}
