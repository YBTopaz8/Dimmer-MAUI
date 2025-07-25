using Dimmer.DimmerSearch.TQL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;
public class QueryModel
{
    public string FilterText { get; set; } = string.Empty;
    public string? SortField { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public LimiterClause? Limiter { get; set; }

    // This ToString method correctly generates the query WITHOUT brackets.
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(FilterText))
        {
            parts.Add(FilterText.Trim());
        }

        if (!string.IsNullOrEmpty(SortField))
        {
            string direction = SortDirection.ToString().ToLowerInvariant();
            if (direction == "ascending")
                direction = "asc";
            if (direction == "descending")
                direction = "desc";

            // Handle random separately as it doesn't need a field
            if (SortDirection == SortDirection.Random)
            {
                // We'll let the Limiter handle the 'random' keyword
            }
            else
            {
                parts.Add($"{direction} {SortField}");
            }
        }

        if (Limiter != null)
        {
            string limiterType = Limiter.Type.ToString().ToLowerInvariant();
            if (Limiter.Type == LimiterType.Random)
            {
                parts.Add(Limiter.Count == int.MaxValue ? "random" : $"random {Limiter.Count}");
            }
            else
            {
                parts.Add($"{limiterType} {Limiter.Count}");
            }
        }

        return string.Join(" ", parts).Trim();
    }

    // The robust static parser method.
    public static QueryModel Parse(string rawQuery)
    {
        var model = new QueryModel();
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return model;
        }

        var tokens = Lexer.Tokenize(rawQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        var filterParts = new List<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var nextToken = (i + 1 < tokens.Count) ? tokens[i + 1] : null;

            // --- Look for Sorter ---
            if ((token.Type == TokenType.Asc || token.Type == TokenType.Desc) && nextToken?.Type == TokenType.Identifier)
            {
                model.SortDirection = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;
                model.SortField = nextToken.Text;
                i++; // Consume the field name token
                continue;
            }

            // --- Look for Limiter (First or Random) ---
            if (token.Type == TokenType.First || token.Type == TokenType.Random || token.Type == TokenType.Shuffle)
            {
                var type = token.Type == TokenType.First ? LimiterType.First : LimiterType.Random;
                int count = (type == LimiterType.Random) ? int.MaxValue : 1;

                if (nextToken?.Type == TokenType.Number && int.TryParse(nextToken.Text, out int parsedCount))
                {
                    count = parsedCount;
                    i++; // Consume the number token
                }
                model.Limiter = new LimiterClause(type, count);

                // If it's a random limiter, that also implies a random sort
                if (type == LimiterType.Random)
                {
                    model.SortDirection = SortDirection.Random;
                    model.SortField = "RandomSort"; // A placeholder name
                }
                continue;
            }

            // If it's not a recognized directive, it's part of the filter
            filterParts.Add(token.Text);
        }

        model.FilterText = string.Join(" ", filterParts);
        return model;
    }
}