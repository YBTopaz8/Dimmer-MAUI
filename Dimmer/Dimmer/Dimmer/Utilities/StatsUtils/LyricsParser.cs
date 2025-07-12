using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.StatsUtils;

public static class LyricsParser
{
    /// <summary>
    /// Converts a timecode string (e.g., "01:23.45" or "[01:23.45]") into total milliseconds.
    /// This implementation is robust and handles brackets and various formats.
    /// </summary>
    /// <param name="timeCode">The timecode string to parse.</param>
    /// <returns>Total milliseconds, or -1 if parsing fails.</returns>
    public static int DecodeTimecodeToMs(string timeCode)
    {
        if (string.IsNullOrWhiteSpace(timeCode))
            return -1;

        try
        {
            // Clean the input string by removing brackets and whitespace.
            string cleanedTimeCode = timeCode.Trim().TrimStart('[').TrimEnd(']');

            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            int milliseconds = 0;

            string[] parts = cleanedTimeCode.Split(':');

            // Work backwards from the end. The last part is seconds.milliseconds.
            if (parts.Length > 0)
            {
                string lastPart = parts[^1];
                if (lastPart.Contains('.'))
                {
                    string[] secParts = lastPart.Split('.');
                    int.TryParse(secParts[0], out seconds);
                    // Pad with zeros for consistency (e.g., "45" becomes 450)
                    if (secParts.Length > 1 && int.TryParse(secParts[1].PadRight(3, '0').Substring(0, 3), out int ms))
                    {
                        milliseconds = ms;
                    }
                }
                else
                {
                    int.TryParse(lastPart, out seconds);
                }
            }
            if (parts.Length > 1)
            {
                int.TryParse(parts[^2], out minutes);
            }
            if (parts.Length > 2)
            {
                int.TryParse(parts[^3], out hours);
            }

            return (hours * 3600 + minutes * 60 + seconds) * 1000 + milliseconds;
        }
        catch (Exception ex)
        {
            // Log this error if you have a logger available
            System.Diagnostics.Debug.WriteLine($"Failed to decode timecode '{timeCode}': {ex.Message}");
            return -1;
        }
    }
}