using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public static class FilenameParser
{
    // Regex to capture "Artist - Title" format. It's non-greedy to handle multiple hyphens.
    // It also handles an optional track number at the beginning.
    private static readonly Regex ArtistTitleRegex = new(
        @"^(?<tracknum>\d{1,3}[\s\.-]*)?(?<artist>.+?) - (?<title>.+?)$",
        RegexOptions.Compiled | RegexOptions.RightToLeft); // RightToLeft helps find the LAST " - " which is often the correct one.

    /// <summary>
    /// Parses a filename to guess the Artist and Title when tags are missing.
    /// </summary>
    /// <param name="filePath">The full path to the audio file.</param>
    /// <returns>A tuple containing the guessed artist and title. Both can be null if parsing fails.</returns>
    public static (string? Artist, string? Title) Parse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return (null, null);
        }

        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Match match = ArtistTitleRegex.Match(fileName);
        if (match.Success)
        {
            string artist = match.Groups["artist"].Value.Trim();
            string title = match.Groups["title"].Value.Trim();

            // Basic validation: if either part is empty, the parse is likely wrong.
            if (!string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title))
            {
                return (artist, title);
            }
        }

        // Fallback: If no " - " separator is found, assume the whole filename is the title.
        return (null, fileName.Trim());
    }
}