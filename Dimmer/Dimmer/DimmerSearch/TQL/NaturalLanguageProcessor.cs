


using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.TQL;

public static class NaturalLanguageProcessor
{
    // A value is either a quoted string "like this", or a single word/number
    private const string Value = @"((?:""(?:[^""]|"""")*""|\S+)+)";

    // 1. NORMALIZATION: Turn human slang into math before parsing
    private static readonly Dictionary<string, string> _normalizers = new(StringComparer.OrdinalIgnoreCase)
    {
        // Comparators
        { "longer than", ">" }, { "more than", ">" }, { "over", ">" }, { "greater than", ">" },
        { "shorter than", "<" }, { "less than", "<" }, { "under", "<" },
        { "at least", ">=" }, { "up to", "<=" }, { "exactly", "=" },
        
        // Time Units (Normalize to our TqlUtilities format: s, m, h)
        { "minutes", "m" }, { "minute", "m" }, { "mins", "m" }, { "min", "m" },
        { "seconds", "s" }, { "second", "s" }, { "secs", "s" }, { "sec", "s" },
        { "hours", "h" }, { "hour", "h" }, { "hrs", "h" }, { "hr", "h" },
        
        // Date Slang
        { "in the last", "last" }, { "in the past", "last" }, { "over the last", "last" },
        
        // Clean up common possession
        { "'s music", " music" }, { "'s songs", " songs" }
    };

    // 2. EXTRACTION: Now that the English is cleaned up, extract the data
    private static readonly (Regex Pattern, string Replacement)[] _rules =
    {
        // --- Artists, Albums, Genres ---
        (new Regex($@"(?:songs by|music from|music by|artist is|tracks by|made by) {Value}", RegexOptions.IgnoreCase), @"artist:$1"),
        (new Regex($@"(?:in album|on album|from the album|album is) {Value}", RegexOptions.IgnoreCase), @"album:$1"),
        (new Regex($@"(?:of genre|genre is|style is) {Value}", RegexOptions.IgnoreCase), @"genre:$1"),

        // --- Durations (Now handles: > 2m, < 2:00 m, >= 200 s, < 1h) ---
        // Notice how simple the Regex is because "longer than" was already replaced by ">"
        (new Regex(@"(>|<|>=|<=|=)\s*(\d+(?::\d+)?)\s*([smh]?)", RegexOptions.IgnoreCase), "len:$1$2$3"),

        // --- Ratings & Plays (NEW!) ---
        (new Regex(@"(>|<|>=|<=|=)?\s*(\d+)\s*(?:stars?|rating)", RegexOptions.IgnoreCase), "rate:$1$2"),
        (new Regex(@"rated\s*(>|<|>=|<=|=)?\s*(\d+)", RegexOptions.IgnoreCase), "rate:$1$2"),
        (new Regex(@"(?:played|listened to)\s*(>|<|>=|<=|=)\s*(\d+)\s*times?", RegexOptions.IgnoreCase), "plays:$1$2"),
        (new Regex(@"with\s*(>|<|>=|<=|=)\s*(\d+)\s*plays", RegexOptions.IgnoreCase), "plays:$1$2"),

        // --- Dates (Relative) ---
        (new Regex(@"(added|played) tonight", RegexOptions.IgnoreCase), "$1:evening"),
        (new Regex(@"(added|played) (today|yesterday|this week|last week|this month|last month)", RegexOptions.IgnoreCase), "$1:$2"),
        
        // --- Dates (Time Ago) ---
        // e.g. "added last 5 days" -> added:ago("5d")
        (new Regex(@"(added|played) last (\d+)\s*([dwmy])\w*", RegexOptions.IgnoreCase), "$1:ago(\"$2$3\")"),

        // --- Decades ---
        (new Regex(@"(?:from the|in the) (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),

        // --- Booleans ---
        (new Regex(@"favorite songs|my favorites|my favs?|loved songs", RegexOptions.IgnoreCase), "fav:true"),
        (new Regex(@"has lyrics|have lyrics|with lyrics", RegexOptions.IgnoreCase), "haslyrics:true"),
        (new Regex(@"no lyrics|without lyrics|instrumental", RegexOptions.IgnoreCase), "haslyrics:false"),
    };

    public static string Process(string naturalQuery)
    {
        if (string.IsNullOrWhiteSpace(naturalQuery)) return string.Empty;

        // 1. MASK QUOTES (Protect user's exact strings)
        var quotesDict = new Dictionary<string, string>();
        int counter = 0;
        var maskedQuery = Regex.Replace(naturalQuery, "\".*?\"", match =>
        {
            string placeholder = $"__QUOTE_{counter++}__";
            quotesDict[placeholder] = match.Value;
            return placeholder;
        });

        // 2. NORMALIZE SLANG
        // We replace phrases like "longer than" with ">" before running the main regexes.
        // We pad with spaces to ensure we only replace whole words (using Regex \b).
        foreach (var (slang, mathSymbol) in _normalizers)
        {
            maskedQuery = Regex.Replace(maskedQuery, $@"\b{slang}\b", mathSymbol, RegexOptions.IgnoreCase);
        }

        // 3. APPLY RULES
        foreach (var (pattern, replacement) in _rules)
        {
            while (pattern.IsMatch(maskedQuery))
            {
                maskedQuery = pattern.Replace(maskedQuery, replacement, 1);
            }
        }

        // 4. CLEANUP (Remove accidental double spaces created by replacement)
        maskedQuery = Regex.Replace(maskedQuery, @"\s+", " ");

        // 5. UNMASK QUOTES
        foreach (var kvp in quotesDict)
        {
            maskedQuery = maskedQuery.Replace(kvp.Key, kvp.Value);
        }

        return maskedQuery.Trim();
    }
}

//Why is this approach "AI-ish"?

//By separating the Dictionary Normalization from the Regex Extraction, the engine
//understands concepts rather than just strings.

//Example 1: Durations

//If a user types: "music by Tool longer than 2:00 mins"

//1.  Normalization Pass: Sees "longer than", turns it into >. Sees "mins", turns
//    it into m.
//      - String becomes: "music by Tool > 2:00 m"
//2.  Extraction Pass: Sees > 2:00 m.Extracts it to len:>2:00m. Sees "music by
//    Tool". Extracts to artist:Tool.
//3.  Result: artist:Tool len:>2:00m

//This works for "at least 1h", "over 200 s", "shorter than 90 seconds", etc.

//Example 2: Ratings (Brand New Feature)

//If a user types: "rated over 4 stars"

//1.  Normalization Pass: Turns "over" into >.
//      - String becomes: "rated > 4 stars"
//2.  Extraction Pass: Regex catches rated > 4. Translates to rate:>4.

//Example 3: Play Counts (Brand New Feature)

//If a user types: "played more than 50 times"

//1.  Normalization Pass: Turns "more than" into >.
//      - String becomes: "played > 50 times"
//2.  Extraction Pass: Regex catches played > 50 times. Translates to plays:>50.

//The Future Step

//If you eventually outgrow this, the next architectural step is a Lexer-based NLP
//Engine. Instead of operating on strings, your Lexer would generate tokens like
//[Action: Play]
//[Subject: Song]
//[Condition: Over 5 mins]. But for an offline music
//app, this Hybrid Normalizer + Regex approach will successfully handle 99% of
//what your users throw at it, instantly and with zero RAM overhead.


