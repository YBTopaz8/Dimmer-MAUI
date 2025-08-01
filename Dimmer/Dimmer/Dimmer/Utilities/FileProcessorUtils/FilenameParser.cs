using System.Text.RegularExpressions;

namespace Dimmer.Utilities.FileProcessorUtils;
public static class FilenameParser
{
    // A list of common "noise" patterns to remove from titles, case-insensitive.
    private static readonly string[] NoisePatterns =
    {
        "(official music video)", "(official video)", "(audio)", "[hd]", "(hd)",
        "(official audio)", "[official audio]", "(lyrics)", "[lyrics]",
        "(visualizer)", "[visualizer]", "(official lyric video)",
        "official music video", "official video", "music video", "lyric video"
    };

    // Regex to find common Artist - Title patterns.
    // It looks for "Anything" then a " - " separator, then "Anything else".
    private static readonly Regex ArtistTitleRegex = new Regex(@"^(?<artist>.+?)\s+-\s+(?<title>.+)", RegexOptions.Compiled);

    // Regex to remove track numbers like "01.", "02 ", "1-03 - ", etc. at the start.
    private static readonly Regex TrackNumberRegex = new Regex(@"^(\d{1,3}[\s.-]*)", RegexOptions.Compiled);

    // Regex to find and remove YouTube IDs like (youtube, cbHxCcc1fNs) or [AbC123XyZ].
    private static readonly Regex YouTubeIdRegex = new Regex(@"[\(\[]((youtube|yt),?|id=)?[a-zA-Z0-9_-]{11}[\)\]]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (string Artist, string Title) Parse(string filename)
    {
        string artist = "Unknown Artist";
        string title = Path.GetFileNameWithoutExtension(filename);

        // --- Step 1: Clean up common noise ---
        title = YouTubeIdRegex.Replace(title, "");
        foreach (var pattern in NoisePatterns)
        {
            title = title.Replace(pattern, "", StringComparison.OrdinalIgnoreCase);
        }

        // --- Step 2: Try to extract Artist and Title using " - " separator ---
        var match = ArtistTitleRegex.Match(title);
        if (match.Success)
        {
            artist = match.Groups["artist"].Value.Trim();
            title = match.Groups["title"].Value.Trim();
        }

        // --- Step 3: Clean up remaining artifacts ---
        // Remove track numbers from the beginning of the title.
        title = TrackNumberRegex.Replace(title, "");

        // Remove any content in parentheses or brackets that might be left over.
        title = Regex.Replace(title, @"\s*[\(\[].*?[\)\]]\s*", "").Trim();

        // Final trim to clean up any leading/trailing whitespace.
        artist = artist.Trim();
        title = title.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            // If cleaning removed everything, fall back to the original filename.
            title = Path.GetFileNameWithoutExtension(filename);
        }

        return (artist, title);
    }
}