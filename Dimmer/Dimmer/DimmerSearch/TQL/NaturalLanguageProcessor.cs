namespace Dimmer.DimmerSearch.TQL;

public static class NaturalLanguageProcessor
{
    private const string Value = @"((?:""(?:[^""]|"""")*""|\S+)+)";

    private static readonly (Regex Pattern, string Replacement)[] _rules =
    {
        (new Regex($@"songs by {Value}|music from {Value}|artist is {Value}", RegexOptions.IgnoreCase), @"artist:$1"),
        (new Regex($@"album is {Value}|in album {Value}", RegexOptions.IgnoreCase), @"album:$1"),
        (new Regex(@"(added|played) tonight", RegexOptions.IgnoreCase), "$1:evening"),
        (new Regex(@"added (today|yesterday|this week|last week)", RegexOptions.IgnoreCase), @"added:$1"),
        (new Regex(@"played (today|yesterday|this week|last week)", RegexOptions.IgnoreCase), @"played:$1"),
        (new Regex(@"favorite songs|my favorites|my fav|my favs|loved songs", RegexOptions.IgnoreCase), "fav:true"),
        (new Regex(@"has lyrics|have lyrics|with lyrics", RegexOptions.IgnoreCase), "haslyrics:true"),
        (new Regex(@"has no lyrics|without lyrics", RegexOptions.IgnoreCase), "haslyrics:false"),
        (new Regex(@"from the (\d{2,4})s|in the (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),
        (new Regex(@"at least (\d+:\d+|\d+)\s*m(in(ute)?s?)?", RegexOptions.IgnoreCase), "len:>=$1"),
        (new Regex(@"longer than (\d+:\d+|\d+)\s*m(in(ute)?s?)?",    RegexOptions.IgnoreCase), "len:>$1"),
        (new Regex(@"shorter than (\d+:\d+|\d+)\s*m(in(ute)?s?)?", RegexOptions.IgnoreCase), "len:<$1"),
        (new Regex(@"up to (\d+:\d+|\d+)\s*m(in(ute)?s?)?", RegexOptions.IgnoreCase), "len:<=$1"),
        (new Regex($@"of genre {Value}", RegexOptions.IgnoreCase), "genre:$1"),
        (new Regex(@"in the (last|past) (\d+)\s*(day)s?", RegexOptions.IgnoreCase), "added:ago(\"$2d\")"),
        (new Regex(@"in the (last|past) (\d+)\s*(week)s?", RegexOptions.IgnoreCase), "added:ago(\"$2w\")"),
        (new Regex(@"in the (last|past) (\d+)\s*(month)s?", RegexOptions.IgnoreCase), "added:ago(\"$2m\")"),
        (new Regex(@"in the (last|past) (\d+)\s*(year)s?", RegexOptions.IgnoreCase), "added:ago(\"$2y\")"),
    };

    public static string Process(string naturalQuery)
    {
        if (string.IsNullOrWhiteSpace(naturalQuery)) return string.Empty;

        // 1. MASK QUOTES
        var quotesDict = new Dictionary<string, string>();
        int counter = 0;
        var maskedQuery = Regex.Replace(naturalQuery, "\".*?\"", match =>
        {
            string placeholder = $"__QUOTE_PROTECTED_{counter++}__";
            quotesDict[placeholder] = match.Value;
            return placeholder;
        });

        // 2. RUN STANDARD NLP RULES
        maskedQuery = Regex.Replace(maskedQuery, @"(\w+)'s music", "music by $1", RegexOptions.IgnoreCase);
        foreach (var (pattern, replacement) in _rules)
        {
            while (pattern.IsMatch(maskedQuery))
            {
                maskedQuery = pattern.Replace(maskedQuery, replacement, 1);
            }
        }

        // NO MORE "ANY:" INJECTION HERE. The parser handles it naturally now!

        // 3. UNMASK QUOTES
        foreach (var kvp in quotesDict)
        {
            maskedQuery = maskedQuery.Replace(kvp.Key, kvp.Value);
        }

        return maskedQuery.Trim();
    }
}