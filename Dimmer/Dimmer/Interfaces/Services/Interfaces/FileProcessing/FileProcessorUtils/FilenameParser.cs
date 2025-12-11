using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

public static class FilenameParser
{
    private static readonly Regex TrackNumberRegex = new(
        @"^\d{1,3}\s*[\.-]?\s*",
        RegexOptions.Compiled);

    public static (string? Artist, string? Title) Parse(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return (null, null);

        var fileName = Path.GetFileNameWithoutExtension(Uri.UnescapeDataString(filePath));

        // Remove track numbers
        fileName = TrackNumberRegex.Replace(fileName, "");

        // Fast search for first " - "
        int sep = fileName.IndexOf(" - ", StringComparison.Ordinal);
        if (sep > 0 && sep < fileName.Length - 3)
        {
            var artist = fileName[..sep].Trim();
            var title = fileName[(sep + 3)..].Trim();
            return (artist, title);
        }

        return (null, fileName.Trim());
    }
}
