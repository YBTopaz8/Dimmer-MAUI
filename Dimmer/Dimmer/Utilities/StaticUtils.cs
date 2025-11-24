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

        // 1️⃣ Strip "by ..." uploader metadata entirely
        t = ByUploaderRegex.Replace(t, string.Empty);

        // 2️⃣ Remove artist prefix if given
        if (!string.IsNullOrWhiteSpace(artist) &&
            !artist.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            string escapedArtist = Regex.Escape(artist.Trim());
            t = Regex.Replace(t, $"^{escapedArtist}\\s*[-–—:]\\s*",
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        // 3️⃣ Remove “ft/feat/featuring ...” from title
        t = FeatRegex.Replace(t, string.Empty);

        // 4️⃣ Remove video/suffix clutter
        t = VideoSuffixRegex.Replace(t, string.Empty);
        t = YearOrCopyRegex.Replace(t, string.Empty);

        // 5️⃣ Trim & normalize whitespace
        t = Regex.Replace(t, @"\s{2,}", " ");
        t = Regex.Replace(t, @"^[-–—_ ]+|[-–—_ ]+$", string.Empty).Trim();

        // 6️⃣ Keep last segment only if it looks like "Artist - Title"
        if (MultiDashRegex.IsMatch(t))
        {
            var parts = t.Split('-', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim()).ToList();
            if (parts.Count > 1)
                t = parts.Last();
        }

        // 7️⃣ Proper casing
        if (!string.IsNullOrEmpty(t))
            t = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.ToLower());

        return t;
    }

    /// <summary>
    /// Cleans and normalizes artist metadata, merging 'ft/feat' references and deduplicating.
    /// </summary>
    public static string CleanArtist(string filePath, string artistName, string title)
    {
        string a = artistName?.Trim() ?? string.Empty;
        a = a.Replace("�", "").Replace("?", "").Replace("–", "-").Trim();

        // 1️⃣ Split & deduplicate
        var parts = a.Split(new[] { ',', ';', '/', '&' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(x => x.Trim())
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .ToList();

        // 2️⃣ Extract “ft/feat” from title if present
        var featMatch = FeatArtistFromTitleRegex.Match(title);
        if (featMatch.Success)
        {
            var feats = featMatch.Groups[1].Value
                .Split(new[] { ',', '&', '/', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var f in feats)
                if (!parts.Contains(f, StringComparer.OrdinalIgnoreCase))
                    parts.Add(f);
        }

        // 3️⃣ Add composer if title indicates soundtrack
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

        // 4️⃣ Final cleanup & normalize
        parts = parts.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        a = parts.Count > 0 ? string.Join(", ", parts) : "Unknown Artist";

        if (!string.IsNullOrEmpty(a))
            a = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(a.ToLower());

        return a;
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
