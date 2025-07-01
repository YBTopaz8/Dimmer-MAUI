using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;


public class AstEvaluator
{
    public static Dictionary<string, string> FieldMappings => _fieldMappings;
    private static readonly Dictionary<string, string> _fieldMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        {"t", "Title"}, {"title", "Title"}, {"ar", "OtherArtistsName"}, {"artist", "OtherArtistsName"},
        {"al", "AlbumName"}, {"album", "AlbumName"}, {"genre", "Genre.Name"}, {"composer", "Composer"},
        {"lang", "Language"}, {"year", "ReleaseYear"}, {"bpm", "BitRate"}, {"len", "DurationInSeconds"},
        {"rating", "Rating"}, {"track", "TrackNumber"}, {"disc", "DiscNumber"}, {"haslyrics", "HasLyrics"},
        {"synced", "HasSyncedLyrics"}, {"fav", "IsFavorite"}, {"lyrics", "SyncLyrics"}, {"lyric", "SyncLyrics"},
        {"slyrics", "EmbeddedSync"}, {"note", "UserNoteText"}, {"comment", "UserNoteText"}
    };

    private static readonly HashSet<string> _numericFields = new() { "ReleaseYear", "BitRate", "DurationInSeconds", "Rating", "TrackNumber", "DiscNumber" };
    private static readonly HashSet<string> _booleanFields = new() { "HasLyrics", "HasSyncedLyrics", "IsFavorite" };

    public Func<SongModelView, bool> CreatePredicate(IQueryNode rootNode)
    {
        return song => Evaluate(rootNode, song);
    }

    private bool Evaluate(IQueryNode node, SongModelView song) => node switch
    {
        LogicalNode n => EvaluateLogical(n, song),
        NotNode n => !Evaluate(n.NodeToNegate, song),
        ClauseNode n => EvaluateClause(n, song),
        _ => true
    };

    private bool EvaluateLogical(LogicalNode node, SongModelView song)
    {
        bool leftResult = Evaluate(node.Left, song);
        if (node.Operator == LogicalOperator.And && !leftResult)
            return false;
        if (node.Operator == LogicalOperator.Or && leftResult)
            return true;
        return Evaluate(node.Right, song);
    }

    private bool EvaluateClause(ClauseNode node, SongModelView song)
    {
        string propertyName = _fieldMappings.TryGetValue(node.Field, out var name) ? name : "Title";

        if (_numericFields.Contains(propertyName))
        {
            double songValue = SemanticQueryHelpers.GetNumericProp(song, propertyName);
            double queryValue = (propertyName == "DurationInSeconds") ? ParseDuration(node.Value.ToString()) : Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);
            return node.Operator switch
            {
                ">" => songValue > queryValue,
                "<" => songValue < queryValue,
                ">=" => songValue >= queryValue,
                "<=" => songValue <= queryValue,
                "-" => songValue >= queryValue && songValue <= ParseDuration(node.UpperValue?.ToString() ?? "0"),
                _ => Math.Abs(songValue - queryValue) < 0.001
            };
        }
        else if (_booleanFields.Contains(propertyName))
        {
            bool songValue = SemanticQueryHelpers.GetBoolProp(song, propertyName);
            bool queryValue = "true|yes|1".Contains(node.Value.ToString()?.ToLower() ?? "false");
            return songValue == queryValue;
        }
        else // Text fields
        {
            string songValue = SemanticQueryHelpers.GetStringProp(song, propertyName) ?? "";
            string queryValue = node.Value.ToString() ?? "";

            return node.Operator switch
            {
                "=" => songValue.Equals(queryValue, StringComparison.OrdinalIgnoreCase),
                "^" => songValue.StartsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                "$" => songValue.EndsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                "~" => SemanticQueryHelpers.LevenshteinDistance(songValue.ToLowerInvariant(), queryValue.ToLowerInvariant()) <= 2,
                "contains" or _ => songValue.IndexOf(queryValue, StringComparison.OrdinalIgnoreCase) >= 0
            };
        }
    }

    private static double ParseDuration(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        double totalSeconds = 0;
        var parts = text.Split(':');
        double multiplier = 1;
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                totalSeconds += value * multiplier;
                multiplier *= 60;
            }
            else
            { return 0; }
        }
        return totalSeconds;
    }
}