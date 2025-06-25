using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;
public class SemanticQueryParser
{
    public event Action<QueryClause> ClauseParsed;

    private static readonly Dictionary<string, (string FieldName, Type FieldType)> _fieldMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            {"t", ("Title", typeof(string))}, {"title", ("Title", typeof(string))},
            {"ar", ("OtherArtistsName", typeof(string))}, {"artist", ("OtherArtistsName", typeof(string))},
            {"al", ("AlbumName", typeof(string))}, {"album", ("AlbumName", typeof(string))},
            {"genre", ("Genre.Name", typeof(string))}, {"composer", ("Composer", typeof(string))},
            {"lang", ("Language", typeof(string))}, {"year", ("ReleaseYear", typeof(int))},
            {"bpm", ("BitRate", typeof(int))}, {"len", ("DurationInSeconds", typeof(double))},
            {"rating", ("Rating", typeof(int))}, {"track", ("TrackNumber", typeof(int))},
            {"disc", ("DiscNumber", typeof(int))}, {"lyrics", ("HasLyrics", typeof(bool))},
            {"synced", ("HasSyncedLyrics", typeof(bool))}, {"fav", ("IsFavorite", typeof(bool))},
        };

    private static readonly Regex _searchRegex;

    static SemanticQueryParser()
    {
        var keys = string.Join("|", _fieldMappings.Keys.Select(Regex.Escape));
        _searchRegex = new Regex(
            $@"\b({keys}):(!)?(>|<|\^|\$|~)?(?:""([^""]*)""|(\S+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public SemanticQuery Parse(string searchText)
    {
        var query = new SemanticQuery();

        var remainingText = _searchRegex.Replace(searchText, match =>
        {
            string prefix = match.Groups[1].Value.ToLower();
            bool isNegated = match.Groups[2].Success;
            string op = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;
            string value = match.Groups[4].Success ? match.Groups[4].Value : match.Groups[5].Value;

            if (_fieldMappings.TryGetValue(prefix, out var mapping))
            {
                var clause = CreateClause(mapping.FieldName, mapping.FieldType, prefix, isNegated, op, value);
                if (clause != null)
                {
                    query.Clauses.Add(clause);
                    ClauseParsed?.Invoke(clause);
                }
            }
            return string.Empty;
        }).Trim();

        if (!string.IsNullOrWhiteSpace(remainingText))
        {
            if (remainingText.Contains('|'))
                query.GeneralOrTerms.AddRange(remainingText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            else
                query.GeneralAndTerms.AddRange(remainingText.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return query;
    }

    private QueryClause CreateClause(string fieldName, Type fieldType, string keyword, bool isNegated, string op, string value)
    {
        var baseProps = new Action<QueryClause>(c => {
            c.FieldName = fieldName;
            c.Keyword = keyword;
            c.IsNegated = isNegated;
            c.RawValue = value;
        });

        if (fieldType == typeof(string))
        {
            var clause = new TextClause { Operator = TextOperator.Contains };
            if (value.Contains('|'))
            { clause.Operator = TextOperator.Or; clause.Values = value.Split('|').ToList(); }
            else
            { clause.Values.Add(value); }

            if (op == "^")
                clause.Operator = TextOperator.StartsWith;
            else if (op == "$")
                clause.Operator = TextOperator.EndsWith;
            else if (op == "~")
                clause.Operator = TextOperator.Levenshtein;

            baseProps(clause);
            return clause;
        }
        if (fieldType == typeof(int) || fieldType == typeof(double))
        {
            var clause = new NumericClause { Operator = NumericOperator.Equals };
            if (op == ">")
                clause.Operator = NumericOperator.GreaterThan;
            else if (op == "<")
                clause.Operator = NumericOperator.LessThan;

            if (value.Contains('-'))
            {
                clause.Operator = NumericOperator.Between;
                var parts = value.Split('-');
                if (double.TryParse(parts[0], out double start))
                    clause.Value = start;
                if (double.TryParse(parts[1], out double end))
                    clause.UpperValue = end;
            }
            else if (double.TryParse(value, out double numValue))
            {
                clause.Value = numValue;
            }

            baseProps(clause);
            return clause;
        }
        if (fieldType == typeof(bool))
        {
            var clause = new FlagClause { TargetValue = "true|yes|1".Contains(value.ToLower()) };
            baseProps(clause);
            return clause;
        }
        return null;
    }
}