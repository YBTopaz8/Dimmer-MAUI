using Microsoft.Maui.Platform;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree.NL;
public enum FieldType { Text, Numeric, Boolean, Duration, Date }

public record FieldDefinition(
    string PrimaryName,
    FieldType Type,
    string[] Aliases,
    string Description,
    //Func<SongModelView, object> PropertyAccessor,
    Expression<Func<SongModelView, object>> PropertyExpression
);

public static class FieldRegistry
{
    public static readonly IReadOnlyList<FieldDefinition> AllFields;
    public static readonly IReadOnlyDictionary<string, FieldDefinition> FieldsByAlias;

    static FieldRegistry()
    {
        AllFields = new List<FieldDefinition>
        {
            // --- Core Text Fields ---
            new("SearchableText", FieldType.Text, new[]{"any"}, "Any text field", s => s.SearchableText),
            new("Title", FieldType.Text, new[]{"t"}, "The song's title", s => s.Title),
            new("OtherArtistsName", FieldType.Text, new[]{"ar", "artist"}, "The song's artist(s)", s => s.OtherArtistsName),
            new("AlbumName", FieldType.Text, new[]{"al", "album"}, "The album name", s => s.AlbumName ?? "Unknown Album"),
            new("GenreName", FieldType.Text, new[]{"genre"}, "The song's genre", s => s.Genre.Name?? "Unknown Genre"),
            new("Composer", FieldType.Text, new[]{"comp"}, "The composer", s => s.Composer),
            new("FilePath", FieldType.Text, new[]{"path"}, "The file path", s => s.FilePath),

            // --- Numeric Fields ---
            new("ReleaseYear", FieldType.Numeric, new[]{"year"}, "The song's release year", s => s.ReleaseYear ?? 0),
            new("Rating", FieldType.Numeric, new[]{"rate"}, "The user's rating (0-5)", s => s.Rating),
            new("TrackNumber", FieldType.Numeric, new[]{"track"}, "The track number", s => s.TrackNumber ?? 0),
            new("BPM", FieldType.Numeric, new[]{"beats"}, "Beats per minute", s => s.BPM ?? 0),
            new("BitRate", FieldType.Numeric, new[]{"bit"}, "The audio bitrate in kbps", s => s.BitRate ?? 0),
            new("FileSize", FieldType.Numeric, new[]{"size"}, "File size in bytes", s => s.FileSize),
            new("PlayCount", FieldType.Numeric, new[]{"plays"}, "Total number of plays", s => s.PlayCount),

            // --- Boolean Fields ---
            new("IsFavorite", FieldType.Boolean, new[]{"fav","love"}, "Is the song a favorite?", s => s.IsFavorite),
            new("HasLyrics", FieldType.Boolean, new[]{"singable"}, "Does the song have any lyrics?", s => s.HasLyrics),
            new("HasSyncedLyrics", FieldType.Boolean, new[]{"synced","ssingable","syncsingable"}, "Does the song have synced lyrics?", s => s.HasSyncedLyrics),
            new("HasCoverArt", FieldType.Boolean, new[]{"hascover"}, "Does the song have cover art?", s => !string.IsNullOrEmpty(s.CoverImagePath)),

            // --- Duration Field ---
            new("DurationInSeconds", FieldType.Duration, new[]{"len","length","time", "duration"}, "The song's duration", s => s.DurationInSeconds),
            
            // --- Date Fields ---
            new("DateCreated", FieldType.Date, new[]{"added"}, "Date the song was added", s => s.DateCreated ?? DateTime.MinValue),
            new("LastPlayed", FieldType.Date, new[]{"played"}, "The last time the song was played", s => s.LastPlayed),

            // --- Advanced Text Fields ---
            new("UserNoteAggregatedText", FieldType.Text, new[]{"note", "comment"}, "Text in user notes", s => s.UserNoteAggregatedText),
            new("LyricsText", FieldType.Text, new[]{"lyrics"}, "Full text of all lyrics", s => s.SyncLyrics ),

        }.AsReadOnly();

        FieldsByAlias = AllFields
             .SelectMany(def => def.Aliases.Concat(new[] { def.PrimaryName }),
                        (def, alias) => new { alias, def })
             .ToDictionary(x => x.alias, x => x.def, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsDate(string fieldAlias)
    {
        if (FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
        {
            return fieldDef.Type == FieldType.Date;
        }
        return false;
    }
}