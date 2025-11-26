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
            new("Format", FieldType.Text, new[]{"type"}, "The file path", nameof(SongModel.FileFormat)),

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







/*
 *   //.ForMember(dest => dest.PlaylistsHavingSong, opt => opt.Ignore())
                //    .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.OtherArtistsName))
                //    .ForMember(dest => dest.AlbumName, opt => opt.MapFrom(src => src.AlbumName))
                //    .ForMember(dest => dest.GenreName, opt => opt.MapFrom(src => src.Genre.Name))

                //    .ForMember(dest => dest.Album, opt => opt.MapFrom(scr=>scr.Album)) // Must ignore RealmObject properties
                //    .ForMember(dest => dest.Artist, opt => opt.MapFrom(scr=>scr.Artist)) // Must ignore RealmObject properties
                //    .ForMember(dest => dest.Genre, opt => opt.MapFrom(src => src.Genre))
                //    .ForMember(dest => dest.HasLyricsColumnIsFiltered, opt => opt.Ignore())
                //    .ForMember(dest => dest.PlayEvents, opt => opt.MapFrom(src => src.PlayHistory))
                //    .ForMember(dest => dest.UserNoteAggregatedCol, opt => opt.MapFrom(src => src.UserNotes))
                //    .ForMember(dest => dest.EmbeddedSync, opt => opt.MapFrom(src => src.EmbeddedSync))
                //    .ForMember(dest => dest.SkipCount, opt => opt.Ignore())
                //    .ForMember(dest => dest.IsCurrentPlayingHighlight, opt => opt.Ignore())
                //    .ForMember(dest => dest.SearchableText, opt => opt.Ignore()) // This is computed in AfterMap
                //    .ForMember(dest => dest.CurrentPlaySongDominantColor, opt => opt.Ignore()) // This is computed in AfterMap

                //    .ForMember(dest => dest.ArtistToSong, opt => opt.MapFrom(src => src.ArtistToSong))
                //    .ForMember(dest => dest.PlayCount, opt => opt.MapFrom(src => src.PlayCount))
                //    .ForMember(dest => dest.PlayCompletedCount, opt => opt.MapFrom(src => src.PlayCompletedCount))
                //    .ForMember(dest => dest.LastPlayed, opt => opt.MapFrom(src => src.LastPlayed))
                //    .AfterMap(

                //         (src, dest) =>
                //        {

                //            // calculate play counts and last played and skip count
                //            //dest.RefreshDenormalizedProperties();

                //            })

                //    ;
*/