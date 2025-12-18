using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Dimmer.Utilities;

public static class StaticUtils
{
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
    public static string CleanArtist(string filePath, string artistName, string title)
    {
        string a = artistName?.Trim() ?? "";

        if (a.Length == 0)
            return "Unknown Artist";

        // basic normalization
        a = a.Replace("�", "")
             .Replace("?", "")
             .Replace("–", "-")
             .Trim();

        // split simple separators first
        var parts = a.Split(new[] { ',', ';', '/', '&' },
                            StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim())
                     .Where(x => x.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .ToList();

        // features from title
        var featMatch = FeatArtistFromTitleRegex.Match(title);
        if (featMatch.Success)
        {
            foreach (var f in featMatch.Groups[1].Value
                .Split(new[] { ',', '&', '/', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()))
            {
                if (!parts.Contains(f, StringComparer.OrdinalIgnoreCase))
                    parts.Add(f);
            }
        }

        // soundtrack composer
        if (SoundtrackTitleRegex.IsMatch(title))
        {
            var matchBy = SoundtrackByRegex.Match(title);
            if (matchBy.Success)
            {
                string composer = matchBy.Groups[1].Value.Trim();
                if (!parts.Contains(composer, StringComparer.OrdinalIgnoreCase))
                    parts.Add(composer);
            }
        }

        // final
        if (parts.Count == 0)
            return "Unknown Artist";

        string final = string.Join(", ", parts);

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(final.ToLower());
    }

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _coverLocks = new();

    public static async Task WriteCoverSafeAsync(string path, byte[] bytes)
    {
        var sem = _coverLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
        }
        finally
        {
            sem.Release();
        }
    }

}
