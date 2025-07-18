using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree.NL;
public static class NaturalLanguageProcessor
{
    // Define rules from more specific to more general
    private static readonly (Regex Pattern, string Replacement)[] _rules =
    {
        // "songs by <artist>" or "music from <artist>"
        (new Regex(@"songs by (.+?)(?:\s+(and|or)|$)", RegexOptions.IgnoreCase), @"artist:""$1"" $2"),
        (new Regex(@"music from (.+?)(?:\s+(and|or)|$)", RegexOptions.IgnoreCase), @"artist:""$1"" $2"),
        
        // "favorite songs" or "my favorites"
        (new Regex(@"favorite songs|my favorites", RegexOptions.IgnoreCase), "fav:true"),
        
        // "from the 90s" or "in the 80s"
        (new Regex(@"from the (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),
        (new Regex(@"in the (\d{2,4})s", RegexOptions.IgnoreCase), "year:$10-$19"),

        // "longer than 5 minutes"
        (new Regex(@"longer than (\d+)(?::(\d+))?\s+minutes?", RegexOptions.IgnoreCase), "len:>$1:$2"),
        (new Regex(@"shorter than (\d+)(?::(\d+))?\s+minutes?", RegexOptions.IgnoreCase), "len:<$1:$2"),

        // "show me <genre> music"
        (new Regex(@"(\w+)\s+music", RegexOptions.IgnoreCase), "genre:$1"),
    };

    public static string Process(string naturalQuery)
    {
        var processedQuery = naturalQuery;
        foreach (var (pattern, replacement) in _rules)
        {
            processedQuery = pattern.Replace(processedQuery, replacement);
        }
        return processedQuery.Trim();
    }
}