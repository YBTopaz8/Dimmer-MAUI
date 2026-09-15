using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.DimmerSearch.TQL;


public static class TqlUtilities
{
    public static double ParseDuration(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.ToLowerInvariant().Trim();

        // Support for "3m", "90s", "1h"
        var match = Regex.Match(text, @"^(\d+)(s|m|h)$");
        if (match.Success)
        {
            double val = double.Parse(match.Groups[1].Value);
            return match.Groups[2].Value switch
            {
                "s" => val,
                "m" => val * 60,
                "h" => val * 3600,
                _ => val
            };
        }

        // Support for standard "3:30" format
        double totalSeconds = 0;
        var parts = text.Split(':');
        double multiplier = 1;
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                totalSeconds += value * multiplier;
                multiplier *= 60;
            }
        }
        return totalSeconds;
    }
}
