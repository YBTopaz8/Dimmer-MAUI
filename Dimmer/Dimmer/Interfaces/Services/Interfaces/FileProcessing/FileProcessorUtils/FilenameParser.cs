using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public static class FilenameParser
{
    // Regex to strip leading track numbers like "01.", "1 - ", "1. ", etc.
    private static readonly Regex TrackNumberRegex = new(
        @"^\d{1,3}\s*[\.-]?\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses a filename to guess the Artist and Title. More robustly handles multiple hyphens.
    /// </summary>
    public static (string? Artist, string? Title) Parse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (null, null);

        string fileName = Path.GetFileNameWithoutExtension(filePath);

        // 1. Strip any leading track number first.
        string cleanFileName = TrackNumberRegex.Replace(fileName, "");

        // 2. Split by " - ". This is a very common and reliable pattern.
        var parts = cleanFileName.Split(" - ", StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 2)
        {
            // Assume the first part is the artist and the rest is the title.
            // This correctly handles titles like "Song Title - Live at Wembley".
            var artist = parts[0].Trim();
            var title = string.Join(" - ", parts.Skip(1)).Trim();
            return (artist, title);
        }

        // Fallback: If no " - " separator, we assume the whole filename is the title.
        return (null, cleanFileName.Trim());
    }
}