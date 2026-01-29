

namespace Dimmer.DimmerSearch;
public static class TQlStaticMethods
{
   

    /// <summary>
    /// Creates a search query string for a given key and value,
    /// automatically wrapping the value in quotes.
    /// It also handles cases where the value itself contains a quote.
    /// </summary>
    /// <param name="key">The search key (e.g., "artist", "album").</param>
    /// <param name="value">The value to search for.</param>
    public static string SetQuotedSearch(string? key, string? value)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        // This makes the logic more robust. If an album title is 'My "Cool" Album',
        // this will escape the inner quotes, resulting in a valid search string.
        var escapedValue = value.Replace("\"", "\\\"");

        // Set the text on the search bar
     return   $"{key}:\"{escapedValue}\"";
    }
    public static class PresetQueries
    {
        // --- Simple Sorts ---
        public static string SortByTitleAsc() => "asc title";
        public static string SortByTrackAsc() => "asc track";
        public static string SortByTitleDesc() => "desc title";
        public static string SortByTrackDesc() => "desc track";
        public static string SortByArtistAsc() => "asc artist";
        public static string SortByArtistDesc() => "desc artist";
        public static string SortByAlbumAsc() => "asc album";
        public static string SortByYearDesc() => "desc year";
        public static string SortByRatingDesc() => "desc rating";
        public static string Shuffle() => "shuffle";
        public static string DescAdded() => "desc added";
        public static string AscAdded() => "asc added";

        // --- Simple Filters ---
        public static string Favorites() => "fav:true";
        public static string SongsWithLyrics() => "haslyrics:true";
        public static string SongsWithSyncedLyrics() => "synced:true";
        public static string FiveStarSongs() => "rating:5";

        // --- Filters with Parameters ---
        public static string ByArtist(string artistName) => $"artist:\"{artistName}\"";
        public static string ByAlbum(string albumName) => $"album:\"{albumName}\"";
        public static string ExactlyByAlbum(string albumName) => $"album:=\"{albumName}\"";
        public static string ByGenre(string genreName) => $"genre:\"{genreName}\"";
        public static string FromYear(int year) => $"year:>={year}";

        // --- Combined Queries ---

        /// <summary>
        /// Finds all songs by a specific artist, sorted by album and then track number.
        /// </summary>
        public static string AllAlbumsByArtist(string artistName)
        {
            // Note: The parser correctly handles multiple sort directives.
            return $"artist:\"{artistName}\" asc album";
        }

        /// <summary>
        /// Gets 50 random favorite songs.
        /// </summary>
        public static string RandomFavorites() => "fav:true random 50";

        public static string ShowMyFav()
        {
            return $"my fav";
        }
    }
}
