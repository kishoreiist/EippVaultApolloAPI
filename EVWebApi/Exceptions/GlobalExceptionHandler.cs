using System.Net;

namespace EVWebApi.Exceptions
{
    public static class GlobalExceptionHandler
    {
        public static (int StatusCode, object Response) Handle(Exception ex)
        {
            return ex switch
            {
                //built-in .NET exception
                KeyNotFoundException => (
                    404,
                    new
                    {
                        statusCode = 404,
                        message = "Resource not found",
                        timestamp = DateTime.UtcNow
                    }
                ),

                ArgumentException => (
                    400,
                    new
                    {
                        statusCode = 400,
                        message = ex.Message,
                        timestamp = DateTime.UtcNow
                    }
                ),

                // FINAL fallback: unknown exception → Internal Server Error
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    new
                    {
                        statusCode = 500,
                        message = "Internal server error",
                        detail = ex.Message, 
                        timestamp = DateTime.UtcNow
                    }
                )
            };
        }
    }
}
