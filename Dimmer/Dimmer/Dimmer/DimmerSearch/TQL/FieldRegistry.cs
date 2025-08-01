namespace Dimmer.DimmerSearch.TQL;
public enum FieldType { Text, Numeric, Boolean, Duration, Date }

public record FieldDefinition(
    string PrimaryName,
    FieldType Type,
    string[] Aliases,
    string Description,
   string PropertyName 
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
            new("SearchableText", FieldType.Text, new[]{"any"}, "Any text field", nameof(SongModelView.SearchableText)),
            new("Title", FieldType.Text, new[]{"t"}, "The song's title", nameof(SongModelView.Title)),
            new("OtherArtistsName", FieldType.Text, new[]{"ar", "artist"}, "The song's artist(s)", nameof(SongModelView.OtherArtistsName)),
            new("AlbumName", FieldType.Text, new[]{"al", "album"}, "The album name", nameof(SongModelView.AlbumName )),
            new("GenreName", FieldType.Text, new[]{"genre","g"}, "The song's genre", nameof(SongModelView.GenreName)),
            new("Composer", FieldType.Text, new[]{"comp"}, "The composer", nameof(SongModelView.Composer)),
            new("FilePath", FieldType.Text, new[]{"path"}, "The file path", nameof(SongModelView.FilePath)),

            // --- Numeric Fields ---
            new("ReleaseYear", FieldType.Numeric, new[]{"year"}, "The song's release year", nameof(SongModelView.ReleaseYear)),
            new("Rating", FieldType.Numeric, new[]{"rate"}, "The user's rating (0-5)", nameof(SongModelView.Rating)),
            new("TrackNumber", FieldType.Numeric, new[]{"track"}, "The track number", nameof(SongModelView.TrackNumber)),
            new("BPM", FieldType.Numeric, new[]{"beats"}, "Beats per minute", nameof(SongModelView.BPM)),
            new("BitRate", FieldType.Numeric, new[]{"bit"}, "The audio bitrate in kbps", nameof(SongModelView.BitRate)),
            new("FileSize", FieldType.Numeric, new[]{"size"}, "File size in bytes", nameof(SongModelView.FileSize)),
            new("PlayCount", FieldType.Numeric, new[]{"plays"}, "Total number of plays", nameof(SongModelView.PlayCount)),

            // --- Boolean Fields ---
            new("IsFavorite", FieldType.Boolean, new[]{"fav","love"}, "Is the song a favorite?", nameof(SongModelView.IsFavorite)),
            new("PlainLyrics", FieldType.Boolean, new[]{"singable"}, "Does the song have any lyrics?", nameof(SongModelView.HasLyrics)),
            new("HasLyrics", FieldType.Boolean, new[]{"synced","ssingable","syncsingable"}, "Does the song have synced lyrics?", nameof(SongModelView.HasSyncedLyrics)),

            // --- Duration Field ---
            new("DurationInSeconds", FieldType.Duration, new[]{"len","length","time", "duration","dur"}, "The song's duration", nameof(SongModelView.DurationInSeconds)),
            
            // --- Date Fields ---
            new("DateCreated", FieldType.Date, new[]{"added"}, "Date the song was added", nameof(SongModelView.DateCreated)),
            new("LastPlayed", FieldType.Date, new[]{"played"}, "The last time the song was played", nameof(SongModelView.LastPlayed)),

            // --- Advanced Text Fields ---
            new("UserNoteAggregatedText", FieldType.Text, new[]{"note","notes","playlist","pl", "comment"}, "Text in user notes", nameof(SongModelView.UserNoteAggregatedText)),
            new("LyricsText", FieldType.Text, new[]{"lyrics"}, "Full text of all lyrics", nameof(SongModelView.SyncLyrics )),

        }.AsReadOnly();
     

        FieldsByAlias = AllFields
             .SelectMany(def => def.Aliases.Concat(new[] { def.PrimaryName }),
                        (def, alias) => new { alias, def })
             .ToDictionary(x => x.alias, x => x.def, StringComparer.OrdinalIgnoreCase);
    }

}