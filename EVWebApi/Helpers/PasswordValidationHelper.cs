using System.Text.RegularExpressions;

namespace EVWebApi.Helpers
{
    public class PasswordValidationHelper
    {
        public static (bool IsValid, string Error) Validate(
            string password,
            string? userName,
            string? firstName,
            string? lastName,
            string? email)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty.");

            if (password.Length < 8)
                return (false, "Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, "[A-Z]"))
                return (false, "Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, "[a-z]"))
                return (false, "Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, "[0-9]"))
                return (false, "Password must contain at least one number.");

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return (false, "Password must contain at least one special character.");

            // Case-insensitive checks
            var pwdLower = password.ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(userName) &&
                pwdLower.Contains(userName.ToLowerInvariant()))
                return (false, "Password should not contain the username.");

            if (!string.IsNullOrWhiteSpace(firstName) &&
                pwdLower.Contains(firstName.ToLowerInvariant()))
                return (false, "Password should not contain the first name.");

            if (!string.IsNullOrWhiteSpace(lastName) &&
                pwdLower.Contains(lastName.ToLowerInvariant()))
                return (false, "Password should not contain the last name.");

            if (!string.IsNullOrWhiteSpace(email))
            {
                var emailPart = email.Split('@')[0].ToLowerInvariant();
                if (pwdLower.Contains(emailPart))
                    return (false, "Password should not contain the email.");
            }

            return (true, string.Empty);
        }
    }
}
