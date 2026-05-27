
using EVWebApi.Models;

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
    public class AccountDeletedException : AppException
    {
        public AccountDeletedException(string message)
            : base(message, 403) { }
    }
    public class AccountNotActivatedException : AppException
    {
        public AccountNotActivatedException(string message)
            : base(message, 403) { }
    }
    public class AccountDisabledException : AppException
    {
        public AccountDisabledException(string message)
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
    public class LockedException : AppException
    {
        public LockedException(string message)
            : base(message, 423) { }
    }
    public class IpBlacklistedException:AppException
    {
        public IpBlacklistedException(string message)
            : base(message, 403) { }
    }
    public class PasswordExpiredException : AppException
    {
        public bool ForcedPasswordReset { get; } = true;
        public PasswordExpiredException(string message)
            : base(message, 403) { }
    }
    public class PasswordReuseException : AppException
    {
        public PasswordReuseException(string message)
            : base(message, 400) { }
    }
    public class DocumentLockedException : AppException
    {

        public int LockedBy { get; }
        public DateTime LockedAt { get; }
        public DateTime LockExpiry { get; }

        public DocumentLockedException(DocumentLock lockInfo)
            : base("Document is currently locked by another user")
        {
            LockedBy = lockInfo.LockedBy;
            LockedAt = lockInfo.LockedAt;
            LockExpiry = lockInfo.LockExpiry;
        }
    }
}
