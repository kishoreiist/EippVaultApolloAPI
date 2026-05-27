using Syncfusion.XlsIO;
using System;
using System.Globalization;

public static class ExcelObjectValueHelper
{
    public static void SetCellValue(this IRange range, object? value)
    {
        if (range == null) return;

      
        var numberFormat = GetEffectiveNumberFormat(range);
        var isCurrency = IsCurrencyFormat(numberFormat);

        switch (value)
        {
            case null:
                range.Value = string.Empty;
                break;

            case int i:
                range.Number = i;
                break;

            case double d:
                range.Number = d;

                // reapplying currency format if cell/column is currency
                if (isCurrency && !string.IsNullOrWhiteSpace(numberFormat))
                {
                    range.NumberFormat = numberFormat;
                }
                break;

            case float f:
                range.Number = f;

                if (isCurrency && !string.IsNullOrWhiteSpace(numberFormat))
                {
                    range.NumberFormat = numberFormat;
                }
                break;

            case decimal dec:
                range.Number = Convert.ToDouble(dec);

                if (isCurrency && !string.IsNullOrWhiteSpace(numberFormat))
                {
                    range.NumberFormat = numberFormat;
                }
                break;

            case string s:
                // If string can be parsed as number → store as number
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    range.Number = parsed;

                    if (isCurrency && !string.IsNullOrWhiteSpace(numberFormat))
                    {
                        range.NumberFormat = numberFormat;
                    }
                }
                else
                {
                    range.Value = s;
                }
                break;

            default:
                range.Value = value.ToString();
                break;
        }
    }

    // ----------------- helpers -----------------

    private static string? GetEffectiveNumberFormat(IRange range)
    {
        // Cell format
        if (!string.IsNullOrWhiteSpace(range.NumberFormat) &&
            !range.NumberFormat.Equals("General", StringComparison.OrdinalIgnoreCase))
        {
            return range.NumberFormat;
        }

        // Column format
        var column = range.Worksheet.Columns[range.Column];
        if (column != null &&
            !string.IsNullOrWhiteSpace(column.NumberFormat) &&
            !column.NumberFormat.Equals("General", StringComparison.OrdinalIgnoreCase))
        {
            return column.NumberFormat;
        }

        return null;
    }

    private static bool IsCurrencyFormat(string? numberFormat)
    {
        if (string.IsNullOrWhiteSpace(numberFormat))
            return false;

        var format = numberFormat.ToLowerInvariant();

        // Covers ₹, $, €, £, accounting formats, locale-based currency
        return format.Contains("₹") ||
               format.Contains("$") ||
               format.Contains("€") ||
               format.Contains("£") ||
               format.Contains("currency") ||
               format.Contains("accounting") ||
               format.Contains("[$");
    }
}
