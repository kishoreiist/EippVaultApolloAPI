using System.Text.Json;
using EVWebApi.Exceptions;

namespace EVWebApi.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)  // Handle all  custom domain exceptions here
            {
                _logger.LogWarning(ex, "Handled application exception");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex.StatusCode;

                var response = new
                {
                    statusCode = ex.StatusCode,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception ex) // Everything else goes to GlobalExceptionHandler
            {
                _logger.LogError(ex, "Unhandled exception");

                var (statusCode, response) = GlobalExceptionHandler.Handle(ex);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}
