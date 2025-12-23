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
       @"\s*(?:\b(?:feat|ft|featuring|vs|versus|with)\b\.?|&|;| x)\s*",
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
    private static readonly Regex ByUploaderRegex =
        new(@"\s+by\s+[A-Za-z0-9$'_.\- ]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FeatRegex =
        new(@"\b(ft|feat|featuring)\b.*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex VideoSuffixRegex =
        new(@"[\(\[\{](official|video|clip|lyrics?|audio|hd|4k|music|remix|officiel|bonus|version|produced\s+by\s+[a-z0-9 ]+)[^\)\]\}]*[\)\]\}]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex YearOrCopyRegex =
        new(@"[_\- ]*(copy|19\d{2}|20\d{2}|[0-9]+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MultiDashRegex =
        new(@"\s-\s", RegexOptions.Compiled);

    private static readonly Regex FeatArtistFromTitleRegex =
        new(@"\b(?:ft|feat|featuring)\s+([\p{L}0-9$'_.\- ,&]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SoundtrackByRegex =
        new(@"\bby\s+([A-Za-z0-9$'_.\- ]+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SoundtrackTitleRegex =
        new(@"(soundtrack|theme|ost|score)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] Suffixes = { "II", "III", "IV", "Jr", "Sr", "Junior", "Senior" };

    private static List<string> SmartSplitArtistString(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();

        // The Regex: 
        // Split by: ; OR / OR & OR " x " OR " feat " 
        // OR Split by , (COMMA) ONLY IF it is NOT followed by a suffix (II, III, Jr)
        string suffixPattern = string.Join("|", Suffixes);
        string pattern = $@"\s*(?:;|/|&|\bx\b|\b(?:feat|ft|featuring|with|vs)\b\.?)\s*|,(?!\s*(?i:{suffixPattern})\b)";

        return Regex.Split(input, pattern, RegexOptions.IgnoreCase)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
    }

    /// <summary>
    /// Cleans a song title by removing uploader metadata, artist prefixes, suffix clutter, etc.
    /// </summary>
    public static string CleanTitle(string path, string title, string album = "", string artist = "")
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        string t = title.Trim();

        // remove uploader metadata
        t = ByUploaderRegex.Replace(t, "");

        // remove artist prefix
        if (!string.IsNullOrWhiteSpace(artist) &&
            !artist.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            string escaped = Regex.Escape(artist.Trim());
            t = Regex.Replace(t, $"^{escaped}\\s*[-–—:]\\s*",
                "", RegexOptions.IgnoreCase);
        }

        // remove featuring
        t = FeatRegex.Replace(t, "");

        // remove clutter
        t = VideoSuffixRegex.Replace(t, "");
        t = YearOrCopyRegex.Replace(t, "");

        // collapse spaces & trim dashes
        t = Regex.Replace(t, @"\s{2,}", " ").Trim();
        t = Regex.Replace(t, @"^[-–—_ ]+|[-–—_ ]+$", "");

        // If multiple dashes, keep last segment
        if (MultiDashRegex.IsMatch(t))
        {
            int idx = t.LastIndexOf('-');
            if (idx > 0 && idx < t.Length - 1)
                t = t[(idx + 1)..].Trim();
        }

        // ToTitleCase
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.ToLower());
    }

    /// <summary>
    /// Cleans and normalizes artist metadata, merging 'ft/feat' references and deduplicating.
    /// </summary>
    public static List<string> CleanArtist(string filePath, string artistName, string title)
    {
        if (string.IsNullOrWhiteSpace(artistName))
            return new List<string> { "Unknown Artist" };

        string a = artistName
    .Replace("\uFFFD", "") // Removes the '' character specifically
    .Replace("\0", "")     // Removes null characters
    .Replace("–", "-")     // Normalizes en-dash to hyphen
    .Trim();

        // 2. Split using the Smart Splitter
        var parts = SmartSplitArtistString(a);

        // 3. Features from title
        if (!string.IsNullOrEmpty(title))
        {
            var featMatch = FeatArtistFromTitleRegex.Match(title);
            if (featMatch.Success)
            {
                var featParts = SmartSplitArtistString(featMatch.Groups[1].Value);
                parts.AddRange(featParts);
            }
        }

        // 4. Soundtrack composer logic
        if (!string.IsNullOrEmpty(title) && SoundtrackTitleRegex.IsMatch(title))
        {
            var matchBy = SoundtrackByRegex.Match(title);
            if (matchBy.Success)
            {
                parts.Add(matchBy.Groups[1].Value.Trim());
            }
        }

        // Final Clean: Distinct, TitleCase, and filter empty
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => textInfo.ToTitleCase(p.Trim().ToLower()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    
    /// <summary>
     /// Intelligently extracts a list of unique, clean artist names from multiple tag fields.
     /// It correctly handles a wide variety of separators.
     /// </summary>
    public static List<string> ExtractArtists(params string?[] artistFields)
    {
        var uniqueCleanNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in artistFields)
        {
            if (string.IsNullOrWhiteSpace(field)) continue;

            // Use the smart splitter on each field
            var names = SmartSplitArtistString(field);
            foreach (var name in names)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    uniqueCleanNames.Add(name.Trim());
            }
        }

        return uniqueCleanNames.Count == 0
            ? new List<string> { "Unknown Artist" }
            : uniqueCleanNames.ToList();
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

    public static Func<string, long>? PlatformGetFileSizeHook { get; set; }
    public static long GetFileSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return 0;

        if (path.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            // Use the Android-specific logic
            return PlatformGetFileSizeHook?.Invoke(path) ?? 0;
        }

        // Use standard System.IO logic for Windows/standard paths
        try { return new FileInfo(path).Length; }
        catch { return 0; }
    }
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