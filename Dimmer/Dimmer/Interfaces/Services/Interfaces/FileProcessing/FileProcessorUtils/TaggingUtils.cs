using System.Collections.Concurrent;
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
       @"\s*(?:\b(?:feat|ft|featuring|vs|versus|with)\b\.?|&|,|;| x)\s*",
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
    /// Intelligently extracts a list of unique, clean artist names from multiple tag fields.
    /// It correctly handles a wide variety of separators.
    /// </summary>
    public static List<string> ExtractArtists(params string?[] artistFields)
    {
        var uniqueCleanNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        string combined = string.Join(";", artistFields.Where(s => !string.IsNullOrWhiteSpace(s)));

        if (string.IsNullOrWhiteSpace(combined))
            return new List<string> { "Unknown Artist" };


        var potentialArtists = ArtistSeparatorRegex.Split(combined);

        foreach (var artist in potentialArtists)
        {
            if (string.IsNullOrWhiteSpace(artist))
                continue;


            var subArtists = artist.Split(';', StringSplitOptions.RemoveEmptyEntries);

            foreach (var sub in subArtists)
            {

                string clean = sub.Trim().Trim('"', '\'').Trim();


                if (!string.IsNullOrWhiteSpace(clean))
                    uniqueCleanNames.Add(clean);
            }
        }

        if (uniqueCleanNames.Count == 0)
            return new List<string> { "Unknown Artist" };

        return uniqueCleanNames.ToList();
    }

    

    // Helper to prevent adding the separators themselves as artists
    private static bool IsSeparator(string input)
    {
        var lower = input.ToLowerInvariant();
        return lower is "feat" or "ft" or "&" or "vs" or "featuring" or "with";
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
    public static Func<string, IReadOnlySet<string>, bool>? PlatformSpecificFileValidator { get; set; }

    public static bool IsValidFile(string filePath, IReadOnlySet<string> supportedExtensions)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (filePath.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            if (PlatformSpecificFileValidator != null)
            {
                return PlatformSpecificFileValidator.Invoke(filePath, supportedExtensions);
            }
            return false;
        }

        if (!File.Exists(filePath))
            return false;



        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension) || !supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return false;

        // Using FileInfo can be slightly slow if called in a tight loop for thousands of files.
        // A direct check via FileSystemInfo can sometimes be faster, but this is fine.
        FileInfo fileInfo = new FileInfo(filePath);
        return fileInfo.Length > 1024; // 1KB is a reasonable minimum for a real audio file
    }
    public static Func<string, bool>? PlatformFileExistsHook { get; set; }

    public static bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        // 1. Handle Android Content URI
        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return PlatformFileExistsHook?.Invoke(path) ?? false;
        }

        // 2. Standard System.IO Logic
        return File.Exists(path);
    }

    // 1. Define a delegate that takes a Path + Extensions and returns a list of files
    // The Android project will assign this function later.
    public static Func<string, IReadOnlySet<string>, List<string>>? PlatformSpecificScanner { get; set; }
    public static Func<string, Stream> PlatformGetStreamHook { get; set; }
    public static Task<List<string>> GetAllAudioFilesFromPathsAsync(
    IEnumerable<string> pathsToScan,
    IReadOnlySet<string> supportedExtensions)
    {
      return Task.Run(() => 
    {  var filesBag = new ConcurrentBag<string>();

        Parallel.ForEach(
            pathsToScan.Where(p => !string.IsNullOrWhiteSpace(p))
                       .Distinct(StringComparer.OrdinalIgnoreCase),
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            path =>
            {
                try
                {
                    if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
                    {
                        if (PlatformSpecificScanner != null)
                        {
                            foreach (var f in PlatformSpecificScanner(path, supportedExtensions))
                                filesBag.Add(f);
                        }
                        return;
                    }

                    if (File.Exists(path))
                    {
                        var ext = Path.GetExtension(path);
                        if (!string.IsNullOrEmpty(ext) && supportedExtensions.Contains(ext))
                            filesBag.Add(path);
                        return;
                    }

                    if (Directory.Exists(path))
                    {
                        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                        {
                            var ext = Path.GetExtension(file);
                            if (!string.IsNullOrEmpty(ext) && supportedExtensions.Contains(ext))
                                filesBag.Add(file);
                        }
                    }
                }
                catch { /* ignored */ }
            });

        return filesBag.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        });
    }

}