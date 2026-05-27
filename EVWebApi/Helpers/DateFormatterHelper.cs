using System.Globalization;

namespace EVWebApi.Helpers
{
    public class DateFormatterHelper
    {

        public static DateOnly? ParsePeriod(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim().Replace(",", "-");
            if (DateTime.TryParseExact(
                    input,
                    "yyyy-MM",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
            {
                return new DateOnly(dt.Year, dt.Month, 1);
            }

            throw new Exception("Invalid period format. Expected yyyy-MM");

        }
    }
}
