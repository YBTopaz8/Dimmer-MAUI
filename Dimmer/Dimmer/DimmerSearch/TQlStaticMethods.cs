

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

            public static string? SortByDimsDesc () => "desc dims";
       
            public static string? SortByDimsAsc() => "asc dims";

            public static string? SortByAlbumDesc() => "desc album";    

        }

    /// <summary>
    /// Safely injects a chip action into the existing user query.
    /// Example: InjectFilterClause("my fav >> save Favs", "exclude", "ar", "Kanye West")
    /// Returns: "my fav exclude ar:\"Kanye West\" >> save Favs"
    /// </summary>
    public static string InjectFilterClause(string? currentQuery, string keyword, string fieldAlias, string value)
    {
        currentQuery = currentQuery?.Trim() ?? string.Empty;
        var escapedValue = value.Replace("\"", "\\\"");
        var clauseToInject = $"{keyword} {fieldAlias}:\"{escapedValue}\"";

        // Check if there are commands attached (e.g. >> save MyList)
        const string commandStart = " >>";
        int commandStartIndex = currentQuery.LastIndexOf(commandStart, StringComparison.OrdinalIgnoreCase);

        if (commandStartIndex != -1)
        {
            string filterPart = currentQuery.Substring(0, commandStartIndex).Trim();
            string commandPart = currentQuery.Substring(commandStartIndex).Trim();
            return $"{filterPart} {clauseToInject} {commandPart}".Trim();
        }

        return string.IsNullOrEmpty(currentQuery)
            ? clauseToInject
            : $"{currentQuery} {clauseToInject}".Trim();
    }
}

// Ensure you update your SearchResult to hold the facets
//public class SearchResult
//{
//    public List<string> SongsResultIds { get; set; } = new();
//    public RealmQueryPlan? Plan { get; set; }
//    public string? ErrorMessage { get; set; }

//    // NEW: Holds the facets generated during the search
//    public SearchFacets? Facets { get; set; }
//}
public class SearchResult
    {
       
        public RealmQueryPlan? Plan { get; set; }
        public IReadOnlyList<ObjectId>? SongsResultIds { get; set; }
        public string? ErrorMessage { get; set; }
    public SearchFacets? Facets { get; set; }
}