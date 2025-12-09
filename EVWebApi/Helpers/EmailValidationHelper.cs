
using System.Text.RegularExpressions;
namespace EVWebApi.Helpers
{
    public class EmailValidationHelper
    {
        public static bool IsValidEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            return Regex.IsMatch(
                input,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase
            );
        }
    }
}

