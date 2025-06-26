using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;



// Represents a song that has been matched and scored.
public class SearchResult
{
    public SongModelView Song { get; }
    public int Score { get; set; }

    public SearchResult(SongModelView song)
    {
        Song = song;
        Score = 0;
    }
}

// This class will parse the user's text into a structured query.
public class ParsedSearchQuery
{
    // A list of predicates that MUST be true (e.g., t:hello)
    public List<Func<SongModelView, bool>> PositiveFilters { get; } = new();

    // A list of predicates that MUST be false (e.g., ar:!adele)
    public List<Func<SongModelView, bool>> NegativeFilters { get; } = new();

    // A list of functions that add to a song's score
    public List<Func<SongModelView, int>> ScoringFunctions { get; } = new();
}

public static class SearchQueryParser
{
    // --- The new "everything" Regex ---
    // 1: Prefix (t, title, year, etc.)
    // 2: Negation (!)
    // 3: Operator (>, <, ^, $, ~)
    // 4: Quoted value
    // 5: Unquoted value
    private static readonly Regex _searchRegex = new Regex(
        @"\b(t|title|ar|artist|al|album|year|bpm|len|explicit|lyrics|favorite):(!)?(>|<|\^|\$|~)?(?:""([^""]*)""|(\S+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ParsedSearchQuery Parse(string searchText)
    {
        var query = new ParsedSearchQuery();
        var generalSearchTerms = new List<string>();

        // Find all "prefix:value" patterns and process them
        var remainingText = _searchRegex.Replace(searchText, match =>
        {
            // Extract all parts of the match
            string prefix = match.Groups[1].Value.ToLower();
            bool isNegated = match.Groups[2].Success;
            string op = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;
            string value = match.Groups[4].Success ? match.Groups[4].Value : match.Groups[5].Value;

            ProcessTerm(query, prefix, isNegated, op, value);

            return string.Empty; // Remove the matched part
        }).Trim();

        // Process any leftover "fuzzy" terms
        if (!string.IsNullOrWhiteSpace(remainingText))
        {
            generalSearchTerms.AddRange(remainingText.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            foreach (var term in generalSearchTerms)
            {
                ProcessTerm(query, "fuzzy", false, string.Empty, term);
            }
        }

        return query;
    }

    private static void ProcessTerm(ParsedSearchQuery query, string prefix, bool isNegated, string op, string value)
    {
        // Text Predicates (Title, Artist, Album)
        if ("t|title|ar|artist|al|album".Contains(prefix))
        {
            string propertyName = prefix.StartsWith("t") ? "Title" : (prefix.StartsWith("ar") ? "OtherArtistsName" : "AlbumName");

            // --- NEW: Handle OR syntax ---
            if (value.Contains('|'))
            {
                // Split the value into multiple options, e.g., "drake|wizkid" -> ["drake", "wizkid"]
                var options = value.Split('|', StringSplitOptions.RemoveEmptyEntries);

                // Create a predicate that checks if the property contains ANY of the options.
                Func<SongModelView, bool> orPredicate = song =>
                {
                    var songValue = GetStringProp(song, propertyName);
                    return options.Any(option => songValue.Contains(option, StringComparison.OrdinalIgnoreCase));
                };

                // Add it to the correct filter list (positive or negative).
                if (isNegated)
                    query.NegativeFilters.Add(orPredicate);
                else
                    query.PositiveFilters.Add(orPredicate);
            }
            else // --- This is the original logic for single values ---
            {
                Func<SongModelView, bool> textPredicate = op switch
                {
                    "^" => song => GetStringProp(song, propertyName).StartsWith(value, StringComparison.OrdinalIgnoreCase),
                    "$" => song => GetStringProp(song, propertyName).EndsWith(value, StringComparison.OrdinalIgnoreCase),
                    "~" => song => LevenshteinDistance(GetStringProp(song, propertyName), value) <= 2,
                    _ => song => GetStringProp(song, propertyName).Contains(value, StringComparison.OrdinalIgnoreCase)
                };
                if (isNegated)
                    query.NegativeFilters.Add(textPredicate);
                else
                    query.PositiveFilters.Add(textPredicate);
            }
        }
        // Numeric Predicates (Year, BPM)
        else if (prefix == "year" || prefix == "bpm") // 2. Numeric Range
        {
            if (value.Contains('-')) // Range like 1990-2000
            {
                var parts = value.Split('-');
                if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    Func<SongModelView, bool> rangePredicate = song => GetIntProp(song, prefix) >= start && GetIntProp(song, prefix) <= end;
                    if (isNegated)
                        query.NegativeFilters.Add(rangePredicate);
                    else
                        query.PositiveFilters.Add(rangePredicate);
                }
            }
            else if (int.TryParse(value, out int numValue))
            {
                Func<SongModelView, bool> numPredicate = op switch
                {
                    ">" => song => GetIntProp(song, prefix) > numValue,
                    "<" => song => GetIntProp(song, prefix) < numValue,
                    _ => song => GetIntProp(song, prefix) == numValue
                };
                if (isNegated)
                    query.NegativeFilters.Add(numPredicate);
                else
                    query.PositiveFilters.Add(numPredicate);
            }
        }
        // Duration/Length Predicate // 7. Duration Search
        else if (prefix == "len")
        {
            if (TryParseDuration(value, out var seconds))
            {
                Func<SongModelView, bool> lenPredicate = op switch
                {
                    ">" => song => song.DurationInSeconds > seconds,
                    "<" => song => song.DurationInSeconds < seconds,
                    _ => song => Math.Abs(song.DurationInSeconds - seconds) < 1
                };
                if (isNegated)
                    query.NegativeFilters.Add(lenPredicate);
                else
                    query.PositiveFilters.Add(lenPredicate);
            }
        }
        // Boolean Predicates // 4. Flag Search
        else if ("explicit|lyrics|favorite".Contains(prefix))
        {
            bool targetValue = "true|yes|1".Contains(value.ToLower());
            Func<SongModelView, bool> boolPredicate = song => GetBoolProp(song, prefix) == targetValue;
            if (isNegated)
                query.NegativeFilters.Add(song => !boolPredicate(song));
            else
                query.PositiveFilters.Add(boolPredicate);
        }
        // Field Presence/Absence // 5. Field Presence
        else if (value.ToLower() == "empty" || value.ToLower() == "null")
        {
            Func<SongModelView, bool> presencePredicate = prefix switch
            {
                "al" or "album" => song => string.IsNullOrEmpty(song.AlbumName),
                // Add more cases here, e.g., lyrics
                _ => song => false
            };
            if (isNegated)
                query.NegativeFilters.Add(song => !presencePredicate(song));
            else
                query.PositiveFilters.Add(presencePredicate);
        }
        // Fuzzy General Search Term
        else if (prefix == "fuzzy")
        {
            // All general terms must be found somewhere for the song to be included
            query.PositiveFilters.Add(song =>
                (song.Title?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (song.OtherArtistsName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (song.AlbumName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false)
            );

            // Add scoring for each match
            query.ScoringFunctions.Add(song => (song.Title?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ? 10 : 0);
            query.ScoringFunctions.Add(song => (song.OtherArtistsName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ? 5 : 0);
            query.ScoringFunctions.Add(song => (song.AlbumName?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ? 2 : 0);
        }
    }

    // --- Helper methods for parsing and reflection ---
    private static string GetStringProp(SongModelView song, string name) => name switch { "Title" => song.Title, "OtherArtistsName" => song.OtherArtistsName, "AlbumName" => song.AlbumName, _ => "" } ?? "";
    private static int GetIntProp(SongModelView song, string name) => name switch { "year" => (int)song.ReleaseYear, _ => 0 };
    private static bool GetBoolProp(SongModelView song, string name) => name switch { "lyrics" => song.HasLyrics, "favorite" => song.IsFavorite, _ => false };

    private static bool TryParseDuration(string input, out int totalSeconds)
    {
        totalSeconds = 0;
        if (input.EndsWith("s"))
            input = input[..^1];

        if (input.Contains(':'))
        {
            var parts = input.Split(':');
            if (int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int sec))
            {
                totalSeconds = min * 60 + sec;
                return true;
            }
        }
        else if (int.TryParse(input, out int sec))
        {
            totalSeconds = sec;
            return true;
        }
        return false;
    }
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++)
            ;
        for (int j = 0; j <= m; d[0, j] = j++)
            ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}