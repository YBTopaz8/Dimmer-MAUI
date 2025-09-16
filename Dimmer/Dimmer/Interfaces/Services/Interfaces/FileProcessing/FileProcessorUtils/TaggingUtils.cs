using System.Text.RegularExpressions;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

/// <summary>
/// A robust utility class for parsing and cleaning metadata from audio files.
/// It's designed to handle the messy, inconsistent tagging found in the wild.
/// </summary>
public static class TaggingUtils
{
    // Regex to split artist strings. It handles common separators like feat, ft, vs, &, ,, ;, and wraps them in word boundaries (\b)
    // to avoid splitting words like "feat." inside an artist's name (e.g., "The Feat. Masters").
    // It's case-insensitive.
    private static readonly Regex ArtistSeparatorRegex = new(
        @"\s*(\b(feat|ft|featuring|vs|versus|with)\b\.?)|\s*(&|,|;| x )\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex to find "version" information in a track title, like (Remix), [Live], - Radio Edit, etc.
    private static readonly Regex TitleVersionRegex = new(
        @"[\(\{\[](?<version>.*?remix|.*?edit|.*?mix|.*?version|live|instrumental|acoustic|unplugged|remastered|explicit|clean)[\)\}\]]|" +
        @"\s-\s(?<version>.*?remix|.*?edit|.*?mix|.*?version|live|instrumental|acoustic|unplugged|remastered)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex to specifically find and remove featured artist info from a title, as it doesn't belong there.
    private static readonly Regex TitleFeatRegex = new(
        @"[\(\{\[]\s*\b(feat|ft|featuring)\b\.?.*[\)\}\]]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);


    /// <summary>
    /// Intelligently extracts a list of unique artist names from primary and album artist tag fields.
    /// Gives precedence to the AlbumArtist field if available, as it's often more canonical.
    /// Handles a wide variety of separators (feat, ft, vs, &, ,, ;).
    /// </summary>
    /// <param name="primaryArtistField">The track's primary artist tag (e.g., "Artist A feat. Artist B").</param>
    /// <param name="albumArtistField">The track's album artist tag (e.g., "Artist A").</param>
    /// <returns>A list of cleaned artist names.</returns>
    public static List<string> ExtractArtists(string? primaryArtistField, string? albumArtistField)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Prioritize AlbumArtist if it exists, as it's often the "cleaner" primary artist list.
        string mainStringToParse = !string.IsNullOrWhiteSpace(albumArtistField)
            ? albumArtistField
            : primaryArtistField ?? string.Empty;

        // Also parse the primary artist field if it's different, to catch featured artists.
        string secondaryStringToParse = (!string.IsNullOrWhiteSpace(primaryArtistField) &&
                                         primaryArtistField != albumArtistField)
            ? primaryArtistField
            : string.Empty;

        ParseAndAddArtists(mainStringToParse, names);
        ParseAndAddArtists(secondaryStringToParse, names);

        if (names.Count == 0)
        {
            names.Add("Unknown Artist");
        }

        return names.ToList();
    }

    private static void ParseAndAddArtists(string? input, HashSet<string> names)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Split the string using our robust regex
        string[] potentialArtists = ArtistSeparatorRegex.Split(input);

        foreach (var artistName in potentialArtists)
        {
            if (string.IsNullOrWhiteSpace(artistName))
                continue;

            // Clean up the resulting name
            string trimmedName = artistName.Trim();

            // Further clean-up: remove surrounding quotes or stray characters if necessary
            // This can be expanded with more rules.
            trimmedName = trimmedName.Trim('"', '\'');

            if (!string.IsNullOrWhiteSpace(trimmedName) && !IsSeparator(trimmedName))
            {
                names.Add(trimmedName);
            }
        }
    }

    // Helper to prevent adding the separators themselves as artists
    private static bool IsSeparator(string input)
    {
        var lower = input.ToLowerInvariant();
        return lower is "feat" or "ft" or "&" or "vs" or "featuring" or "with" or "x";
    }

    /// <summary>
    /// Parses a raw track title to separate the main title from version/remix information.
    /// Also removes featured artist info from the title string itself.
    /// </summary>
    /// <param name="rawTitle">The raw title from the file's tag.</param>
    /// <returns>A tuple containing the cleaned main title and any extracted version info.</returns>
    public static (string MainTitle, string? VersionInfo) ParseTrackTitle(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return (string.Empty, null);
        }

        // 1. First, remove any "feat. Artist" sections from the title, as this is artist metadata.
        string titleWithoutFeat = TitleFeatRegex.Replace(rawTitle, string.Empty);

        // 2. Now, find and extract version/remix information.
        var versionMatches = TitleVersionRegex.Matches(titleWithoutFeat);
        string? versionInfo = null;

        if (versionMatches.Count > 0)
        {
            versionInfo = string.Join(" ", versionMatches.Select(m => m.Groups["version"].Value.Trim())).Trim();
            // Remove the matched version strings from the title
            titleWithoutFeat = TitleVersionRegex.Replace(titleWithoutFeat, string.Empty);
        }

        // 3. Final cleanup of the main title string.
        string mainTitle = titleWithoutFeat.Trim();
        // Remove trailing characters like "-" or orphaned parentheses
        mainTitle = mainTitle.TrimEnd(' ', '-', '(', '[');

        return (mainTitle, versionInfo);
    }

    // Your original IsValidFile and GenerateId are good, I've kept them here.
    // Let's enhance GetAllAudioFilesFromPaths to be more efficient.

    public static string GenerateId(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length < 4)
            prefix = "SONG"; // A more descriptive default
        return $"{prefix.Substring(0, 4).ToUpperInvariant()}_{Guid.NewGuid()}";
    }

    public static bool IsValidFile(string filePath, IReadOnlySet<string> supportedExtensions)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;

        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension) || !supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return false;

        // Using FileInfo can be slightly slow if called in a tight loop for thousands of files.
        // A direct check via FileSystemInfo can sometimes be faster, but this is fine.
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length > 1024; // 1KB is a reasonable minimum for a real audio file
    }

    /// <summary>
    /// Efficiently finds all audio files from a list of paths (which can be files or directories).
    /// Uses streaming enumeration to minimize memory usage for large directories.
    /// </summary>
    public static List<string> GetAllAudioFilesFromPaths(IEnumerable<string> pathsToScan, IReadOnlySet<string> supportedExtensions)
    {
        var uniqueFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in pathsToScan.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(path))
                {
                    if (supportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                    {
                        uniqueFiles.Add(path);
                    }
                }
                else if (Directory.Exists(path))
                {
                    var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                                         .Where(f => supportedExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase));
                    foreach (var file in files)
                    {
                        uniqueFiles.Add(file);
                    }
                }
                else
                {
                    Debug.WriteLine($"Invalid path or unsupported file type: '{path}'");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Access denied for path '{path}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing path '{path}': {ex.Message}");
            }
        }
        return uniqueFiles.ToList();
    }
}