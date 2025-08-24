using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.TQL;

public static class NaturalLanguageProcessor
{
    // A single term can be a quoted string or a single word
    private const string Term = @"(""(?:[^""]|"""")*""|\S+)";

    // A value can be one or more terms
    private const string Value = @"((?:""(?:[^""]|"""")*""|\S+)+)";

    // Define rules from more specific to more general
    private static readonly (Regex Pattern, string Replacement)[] _rules =
    {
    // "songs by <artist>" or "music from <artist>" - Handles quotes and complex names
    (new Regex($@"songs by {Value}|music from {Value}|artist is {Value}", RegexOptions.IgnoreCase), @"artist:$1"),

    // "album is <album name>"
    (new Regex($@"album is {Value}", RegexOptions.IgnoreCase), @"album:$1"),
    (new Regex($@"in album {Value}", RegexOptions.IgnoreCase), @"album:$1"),

    // Date-based queries
    (new Regex(@"added (today|yesterday|this week|last week)", RegexOptions.IgnoreCase), @"added:$1"),
    (new Regex(@"played (today|yesterday|this week|last week)", RegexOptions.IgnoreCase), @"played:$1"),

    // "favorite songs" or "my favorites"
    (new Regex(@"favorite songs|my favorites|my fav|my favs|loved songs", RegexOptions.IgnoreCase), "fav:true"),
    (new Regex(@"has lyrics|have lyrics|with lyrics", RegexOptions.IgnoreCase), "haslyrics:true"),
        
    // "from the 90s" or "in the 80s" - handles 2 or 4 digit decades
    (new Regex(@"from the (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),
    (new Regex(@"in the (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),

    // "longer than 5 minutes" or "shorter than 3:30"
    (new Regex(@"longer than (\d+:\d+|\d+)\s*m(in(ute)?s?)?", RegexOptions.IgnoreCase), "len:>$1"),
    (new Regex(@"shorter than (\d+:\d+|\d+)\s*m(in(ute)?s?)?", RegexOptions.IgnoreCase), "len:<$1"),

    // "show me <genre> music" (e.g., "rock music", "electronic music")
    (new Regex($@"{Value} music", RegexOptions.IgnoreCase), "genre:$1"),

    // Simple commands
    (new Regex(@"show me everything|all songs", RegexOptions.IgnoreCase), "any:"),
    (new Regex(@"in the (last|past) (\d+) (day)s?", RegexOptions.IgnoreCase), "added:>{now.AddDays(-$2)}"),
(new Regex(@"in the (last|past) (\d+) (week)s?", RegexOptions.IgnoreCase), "added:>{now.AddDays(-$2 * 7)}"),
(new Regex(@"in the (last|past) (\d+) (month)s?", RegexOptions.IgnoreCase), "added:>{now.AddMonths(-$2)}"),
(new Regex(@"in the (last|past) (\d+) (year)s?", RegexOptions.IgnoreCase), "added:>{now.AddYears(-$2)}"),
};

    public static string Process(string naturalQuery)
    {
        // Pre-process to handle possessives like "the beatles' music" -> "music by the beatles"
        var processedQuery = Regex.Replace(naturalQuery, @"(\w+)'s music", "music by $1", RegexOptions.IgnoreCase);

        foreach (var (pattern, replacement) in _rules)
        {
            // Use a loop to allow multiple matches for the same rule
            while (pattern.IsMatch(processedQuery))
            {
                processedQuery = pattern.Replace(processedQuery, replacement, 1);
            }
        }
        processedQuery = EvaluateDynamicDates(processedQuery);
        return processedQuery.Trim();
    }

    private static string EvaluateDynamicDates(string query)
    {
        
        string processedQuery = query;

        
        
        processedQuery = Regex.Replace(processedQuery, @">\{now\.AddDays\(([-]?\d+)\)\}", m =>
        {
            int days = int.Parse(m.Groups[1].Value);
            var date = DateTime.UtcNow.AddDays(days);
            return $">{date:yyyy-MM-dd}"; 
        });

        
        processedQuery = Regex.Replace(processedQuery, @">\{now\.AddMonths\(([-]?\d+)\)\}", m =>
        {
            int months = int.Parse(m.Groups[1].Value);
            var date = DateTime.UtcNow.AddMonths(months);
            return $">{date:yyyy-MM-dd}";
        });

        
        processedQuery = Regex.Replace(processedQuery, @">\{now\.AddYears\(([-]?\d+)\)\}", m =>
        {
            int years = int.Parse(m.Groups[1].Value);
            var date = DateTime.UtcNow.AddYears(years);
            return $">{date:yyyy-MM-dd}";
        });

        
        return processedQuery;
    }
}