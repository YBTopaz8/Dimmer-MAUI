using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public static class FilenameParser
{
    // Regex to capture "Artist - Title" format. It's non-greedy to handle multiple hyphens.
    // It also handles an optional track number at the beginning.
    private static readonly Regex ArtistTitleRegex = new(
    @"^(?<tracknum>\d{1,3}[\s\.-]*)?(?<artist>[^-]+?)\s*-\s*(?<title>.+)$",
    RegexOptions.Compiled);
    // RightToLeft helps find the LAST " - " which is often the correct one.

    /// <summary>
    /// Parses a filename to guess the Artist and Title when tags are missing.
    /// </summary>
    /// <param name="filePath">The full path to the audio file.</param>
    /// <returns>A tuple containing the guessed artist and title. Both can be null if parsing fails.</returns>
     public static (string? Artist, string? Title) Parse(string filePath)
     { 
        if (string.IsNullOrWhiteSpace(filePath))
            return (null, null);

        string fileName = Path.GetFileNameWithoutExtension(filePath);

        var match = ArtistTitleRegex.Match(fileName);
        if (!match.Success)
        return (null, fileName.Trim());

        var artist = match.Groups["artist"].Value.Trim();
        var title = match.Groups["title"].Value.Trim();

        // Post-cleaning: split on common multi-artist separators
        artist = Regex.Replace(artist, @"\s*(feat\.?|ft\.?|vs\.?|&|,|;| x )\s*", " & ", RegexOptions.IgnoreCase);

        if (string.IsNullOrWhiteSpace(title))
            return (null, fileName.Trim());

        return (artist, title);
     }
}