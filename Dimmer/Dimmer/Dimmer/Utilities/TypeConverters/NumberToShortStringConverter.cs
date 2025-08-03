using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;
public class NumberToShortStringConverter : IValueConverter
{
    /// <summary>
    /// Converts a numeric value (like play count) into a compact, human-readable string (e.g., 23.9k, 1.4M).
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // --- Step 1: Handle invalid input gracefully ---
        if (value == null)
            return "0";

        // Try to convert the incoming value to a double.
        if (!double.TryParse(value.ToString(), out double number))
        {
            return "0";
        }

        // --- Step 2: The Core Formatting Logic ---
        if (number < 1000)
        {
            // If the number is less than 1000, just show it as is.
            return number.ToString("N0"); // "N0" formats with commas and no decimal places (e.g., 999)
        }
        if (number < 1_000_000)
        {
            // For thousands, divide by 1000 and show one decimal place with a "k".
            // e.g., 23904 -> 23.9k
            return $"{(number / 1000.0):0.0}k";
        }
        if (number < 1_000_000_000)
        {
            // For millions, divide by 1,000,000 and show one decimal place with an "M".
            // e.g., 1450000 -> 1.5M (Note: rounded from 1.45)
            return $"{(number / 1_000_000.0):0.0}M";
        }

        // For billions, divide by 1,000,000,000 and show one decimal place with a "B".
        return $"{(number / 1_000_000_000.0):0.0}B";
    }

    /// <summary>
    /// This method is for converting back from the UI to the ViewModel.
    /// We don't need this for a one-way display, so we just return the original value.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("Converting from short string back to a number is not supported.");
    }
}