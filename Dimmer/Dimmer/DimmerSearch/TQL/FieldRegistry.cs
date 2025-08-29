using DynamicData;

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
            new("SearchableText", FieldType.Text, new[]{"any"}, "Any text field", nameof(SongModel.SearchableText)),
            new("Title", FieldType.Text, new[]{"t"}, "The song's title", nameof(SongModel.Title)),
            new("OtherArtistsName", FieldType.Text, new[]{"ar", "artist"}, "The song's artist(s)", nameof(SongModel.OtherArtistsName)),
            new("AlbumName", FieldType.Text, new[]{"al", "album"}, "The album name", nameof(SongModel.AlbumName )),
            new("GenreName", FieldType.Text, new[]{"genre","g"}, "The song's genre", nameof(SongModel.GenreName)),
            new("Composer", FieldType.Text, new[]{"comp"}, "The composer", nameof(SongModel.Composer)),
            new("FilePath", FieldType.Text, new[]{"path"}, "The file path", nameof(SongModel.FilePath)),

            // --- Numeric Fields ---
            new("ReleaseYear", FieldType.Numeric, new[]{"year"}, "The song's release year", nameof(SongModel.ReleaseYear)),
            new("Rating", FieldType.Numeric, new[]{"rate"}, "The user's rating (0-5)", nameof(SongModel.Rating)),
            new("TrackNumber", FieldType.Numeric, new[]{"track"}, "The track number", nameof(SongModel.TrackNumber)),
            new("BPM", FieldType.Numeric, new[]{"beats"}, "Beats per minute", nameof(SongModel.BPM)),
            new("BitRate", FieldType.Numeric, new[]{"bit"}, "The audio bitrate in kbps", nameof(SongModel.BitRate)),
            new("FileSize", FieldType.Numeric, new[]{"size"}, "File size in bytes", nameof(SongModel.FileSize)),
            new("PlayCount", FieldType.Numeric,new[]{"plays"}, "Total number of plays", nameof(SongModel.PlayCount)),
            new("PlayCompletedCount", FieldType.Numeric, new[]{"dimms","dims",}, "Total number of plays completed", nameof(SongModel.PlayCompletedCount)),
            new("SkipCount", FieldType.Numeric, new[]{"skips",}, "Skip Counts", nameof(SongModel.SkipCount)),
            new("RankInArtist", FieldType.Numeric, new[]{"rar",}, "Rank In RankInArtist", nameof(SongModel.RankInArtist)),
            new("RankInAlbum", FieldType.Numeric, new[]{"ral",}, "Rank In Album", nameof(SongModel.RankInAlbum)),
            new("GlobalRank", FieldType.Numeric, new[]{"rglo",}, "Global Rank", nameof(SongModel.GlobalRank)),

            // --- Boolean Fields ---
            new("IsFavorite", FieldType.Boolean, new[]{"fav","love"}, "Is the song a favorite?", nameof(SongModel.IsFavorite)),
            new("PlainLyrics", FieldType.Boolean, new[]{"singable"}, "Does the song have any lyrics?", nameof(SongModel.HasLyrics)),
            new("HasLyrics", FieldType.Boolean, new[]{"synced","ssingable","syncsingable"}, "Does the song have synced lyrics?", nameof(SongModel.HasSyncedLyrics)),

            // --- Duration Field ---
            new("DurationInSeconds", FieldType.Duration, new[]{"len","length","time", "duration","dur"}, "The song's duration", nameof(SongModel.DurationInSeconds)),
            
            // --- Date Fields ---
            new("DateCreated", FieldType.Date, new[]{"added"}, "Date the song was added", nameof(SongModel.DateCreated)),
            new("LastPlayed", FieldType.Date, new[]{"played"}, "The last time the song was played", nameof(SongModel.LastPlayed)),

            // --- Advanced Text Fields ---
            new("UserNoteAggregatedCol", FieldType.Text, new[]{"note","notes","playlist","pl", "comment"}, "Text in user notes", nameof(SongModel.UserNoteAggregatedText)),
            new("LyricsText", FieldType.Text, new[]{"lyrics"}, "Full text of all lyrics", nameof(SongModel.SyncLyrics )),

        }.AsReadOnly();
     

        FieldsByAlias = AllFields
             .SelectMany(def => def.Aliases.Concat(new[] { def.PrimaryName }),
                        (def, alias) => new { alias, def })
             .ToDictionary(x => x.alias, x => x.def, StringComparer.OrdinalIgnoreCase);
    }

}