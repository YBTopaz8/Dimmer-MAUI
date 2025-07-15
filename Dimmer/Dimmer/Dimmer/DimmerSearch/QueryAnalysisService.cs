using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;

public enum TokenType { Field, Value, Keyword, Operator, Parenthesis, Error, EndOfFile, }

public record HighlightableToken(string Text, TokenType Type);

public class QueryAnalysisService
{
    private static readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase)
        { "include", "add", "exclude", "remove", "and", "or" };

    private static readonly HashSet<string> _directives = new(StringComparer.OrdinalIgnoreCase)
        { "asc", "desc", "random", "shuffle", "first", "last" };

    private static readonly HashSet<string> _fields = new(StringComparer.OrdinalIgnoreCase)
        { "t", "title", "ar", "artist", "al", "album", "genre", "len", "year", "fav", "lyrics", "note" }; // Add all your fields

    // FOR FEATURE #6: SYNTAX HIGHLIGHTING
    public List<HighlightableToken> GetTokensForHighlighting(string query)
    {
        var result = new List<HighlightableToken>();
        var tokens = QueryTokenizer.Tokenize(query); // Use your existing tokenizer

        foreach (var token in tokens)
        {
            var lowerToken = token.ToLower();
            if (_keywords.Contains(lowerToken) || _directives.Contains(lowerToken))
                result.Add(new(token, TokenType.Keyword));
            else if (token == "(" || token == ")")
                result.Add(new(token, TokenType.Parenthesis));
            else if (token == "|")
                result.Add(new(token, TokenType.Operator));
            else if (token.Contains(':'))
            {
                var parts = token.Split(':', 2);
                if (_fields.Contains(parts[0].ToLower()))
                {
                    // Add the field part
                    result.Add(new(parts[0] + ":", TokenType.Field));
                    // Add the value part
                    result.Add(new(parts[1], TokenType.Value));
                }
                else
                {
                    result.Add(new(token, TokenType.Error));
                }
            }
            else // It's a general value or an error
            {
                // A simple check: if it's not a known directive, assume it's a value
                if (!_directives.Contains(lowerToken))
                    result.Add(new(token, TokenType.Value));
                else
                    result.Add(new(token, TokenType.Error)); // Or could be a directive without its number
            }
        }
        return result;
    }

    // FOR FEATURE #7: AUTOCOMPLETE
    public List<string> GetSuggestions(string query, int cursorPosition, IEnumerable<string> allArtistNames, IEnumerable<string> allAlbumNames)
    {
        // Find the token the cursor is currently in
        // (This logic can be complex, for now we'll analyze the last token)
        var lastWord = query.Split(' ').LastOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(lastWord))
            return [.. _fields]; // Starting a new word

        if (lastWord.EndsWith(':'))
        {
            var field = lastWord[..^1].ToLower();
            if (field == "artist" || field == "ar")
                return [.. allArtistNames.Where(a => a.ToLower().StartsWith(lastWord.Split(':').Last())).Take(10)];
            if (field == "album" || field == "al")
                return [.. allAlbumNames.Where(a => a.ToLower().StartsWith(lastWord.Split(':').Last())).Take(10)];
        }

        // Suggest fields that start with the current word
        var suggestions = _fields.Where(f => f.StartsWith(lastWord.ToLower())).Select(f => f + ":").ToList();
        suggestions.AddRange(_keywords.Where(k => k.StartsWith(lastWord.ToLower())));

        return suggestions;
    }
}