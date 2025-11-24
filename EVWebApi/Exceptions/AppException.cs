
namespace EVWebApi.Exceptions
{
    // Base class
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode = 400)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    // Custom Exceptions 

    public class NotFoundException : AppException
    {
        public NotFoundException(string message)
            : base(message, 404) { }
    }

    public class ValidationException : AppException
    {
        public ValidationException(string message)
            : base(message, 400) { }
    }

    public class AuthenticationException : AppException
    {
        public AuthenticationException(string message)
            : base(message, 401) { }
    }

    public class AuthorizationException : AppException
    {
        public AuthorizationException(string message)
            : base(message, 403) { }
    }

    public class ConflictException : AppException
    {
        public ConflictException(string message)
            : base(message, 409) { }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message)
            : base(message, 400) { }
    }

    public class ServerException : AppException
    {
        public ServerException(string message)
            : base(message, 500) { }
    }

}
