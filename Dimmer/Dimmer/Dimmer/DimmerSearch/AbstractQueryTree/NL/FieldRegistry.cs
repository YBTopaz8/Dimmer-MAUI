using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree.NL;
public enum FieldType { Text, Numeric, Boolean, Duration }

public record FieldDefinition(
    string PrimaryName,
    FieldType Type,
    string[] Aliases,
    string Description,
    Func<SongModelView, object> PropertyAccessor
);

public static class FieldRegistry
{
    public static readonly IReadOnlyList<FieldDefinition> AllFields;
    public static readonly IReadOnlyDictionary<string, FieldDefinition> FieldsByAlias;

    static FieldRegistry()
    {
        AllFields = new List<FieldDefinition>
        {
            new("SearchableText", FieldType.Text, new[]{"any"}, "Any text field", s => s.SearchableText),
            new("Title", FieldType.Text, new[]{"t"}, "The song's title", s => s.Title),
            new("OtherArtistsName", FieldType.Text, new[]{"ar", "artist"}, "The song's artist(s)", s => s.OtherArtistsName),
            new("AlbumName", FieldType.Text, new[]{"al", "album"}, "The album name", s => s.AlbumName),
            new("GenreName", FieldType.Text, new[]{"genre"}, "The song's genre", s => s.Genre?.Name),
            new("ReleaseYear", FieldType.Numeric, new[]{"year"}, "The song's release year", s => s.ReleaseYear),
            new("Rating", FieldType.Numeric, new[]{"rating"}, "The user's rating (0-5)", s => s.Rating),
            new("DurationInSeconds", FieldType.Duration, new[]{"len"}, "The song's duration", s => s.DurationInSeconds),
            new("IsFavorite", FieldType.Boolean, new[]{"fav"}, "Is the song a favorite?", s => s.IsFavorite),
            new("HasLyrics", FieldType.Boolean, new[]{"haslyrics"}, "Does the song have lyrics?", s => s.HasLyrics),
            new("HasSyncedLyrics", FieldType.Boolean, new[]{"synced"}, "Does the song have synced lyrics?", s => s.HasSyncedLyrics),
            new("UserNoteAggregatedText", FieldType.Text, new[]{"note", "comment"}, "Text in user notes", s => s.UserNoteAggregatedText),
        }.AsReadOnly();

        FieldsByAlias = AllFields
            .SelectMany(def => def.Aliases.Concat(new[] { def.PrimaryName }),
                       (def, alias) => new { alias, def })
            .ToDictionary(x => x.alias, x => x.def, StringComparer.OrdinalIgnoreCase);
    }
}