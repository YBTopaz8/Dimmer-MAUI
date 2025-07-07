using Dimmer.Utilities.Extensions;

namespace Dimmer.Data.ModelView.NewFolder;
/// <summary>
/// Provides methods for generating "Top X" ranked lists from play event data.
/// These are designed for creating leaderboards and dynamic ranked collections.
/// </summary>
public static class TopStats
{
    // Define PlayType constants for clarity, matching your model's documentation
    private const int PlayType_Play = 0;
    private const int PlayType_Completed = 3;
    private const int PlayType_Seeked = 4;
    private const int PlayType_Skipped = 5;

    #region --- Core Ranking Methods ---

    /// <summary>
    /// Gets the top songs ranked by a specific event type (e.g., completions, skips).
    /// </summary>
    /// <param name="songs">The collection of all songs.</param>
    /// <param name="events">The collection of all play events.</param>
    /// <param name="count">The number of top songs to return.</param>
    /// <param name="playType">The type of play event to count for ranking. See <see cref="DimmerPlayEvent.PlayType"/>.</param>
    /// <param name="startDate">Optional start date to filter events.</param>
    /// <param name="endDate">Optional end date to filter events.</param>
    /// <returns>A ranked list of songs and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopSongsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var filteredEvents = FilterEvents(events, playType, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue)
            .GroupBy(e => e.SongId!.Value)
            .Select(g => new { SongId = g.Key, EventCount = g.Count() })
            .OrderByDescending(x => x.EventCount)
            .Take(count)
            .Where(x => songLookup.ContainsKey(x.SongId)) // Ensure song still exists in library
            .Select(x => (new DimmerStats(){Song=songLookup[x.SongId].ToModelView(),Count=x.EventCount }))];
    }

    /// <summary>
    /// Gets the top artists ranked by a specific event type.
    /// </summary>
    /// <returns>A ranked list of artist names and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopArtistsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        return GetTopRankedByProperty(songs, events, count, playType, s => s.ArtistName, startDate, endDate);
    }

    /// <summary>
    /// Gets the top albums ranked by a specific event type.
    /// </summary>
    /// <returns>A ranked list of album names and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopAlbumsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        return GetTopRankedByProperty(songs, events, count, playType, s => s.AlbumName, startDate, endDate);
    }

    /// <summary>
    /// Gets the top songs ranked by total listening time.
    /// </summary>
    /// <returns>A ranked list of songs and their total listening time in seconds.</returns>
    public static List<DimmerStats> GetTopSongsByListeningTime(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        // For listening time, we don't filter by a specific playType
        var filteredEvents = FilterEvents(events, null, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue && e.DateFinished > e.DatePlayed)
            .GroupBy(e => e.SongId!.Value)
            .Select(g => new
            {
                SongId = g.Key,
                // Sum the duration of each play session
                Time = g.Sum(ev => (ev.DateFinished - ev.DatePlayed).TotalSeconds)
            })
            .OrderByDescending(x => x.Time)
            .Take(count)
            .Where(x => songLookup.ContainsKey(x.SongId))
            .Select(x => (new DimmerStats (){Song= songLookup[x.SongId].ToModelView(), TotalSecondsNumeric=x.Time }))];
    }

    #endregion

    #region --- Convenience "Top" Methods ---

    // These wrap the core methods for easier use.

    // --- Based on COMPLETED PLAYS (Your Primary Use Case) ---
    public static List<DimmerStats> GetTopCompletedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Completed, start, end);

    public static List<DimmerStats> GetTopCompletedArtists(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopArtistsByEventType(s, e, count, PlayType_Completed, start, end);

    public static List<DimmerStats> GetTopCompletedAlbums(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopAlbumsByEventType(s, e, count, PlayType_Completed, start, end);

    // --- Based on SKIPS ---
    public static List<DimmerStats> GetTopSkippedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Skipped, start, end);

    public static List<DimmerStats> GetTopSkippedArtists(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopArtistsByEventType(s, e, count, PlayType_Skipped, start, end);

    // --- Based on SEEKS ---
    public static List<DimmerStats> GetTopSeekedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Seeked, start, end);

    #endregion

    #region --- Private Helpers ---

    /// <summary>
    /// A generic helper to rank by a string property of SongModelView (e.g., ArtistName, AlbumName).
    /// </summary>
    private static List<DimmerStats> GetTopRankedByProperty(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        Func<SongModelView, string?> propertySelector, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var filteredEvents = FilterEvents(events, playType, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .Select(e => propertySelector(songLookup[e.SongId.Value].ToModelView())) // Get the property (e.g., ArtistName)
            .Where(name => !string.IsNullOrEmpty(name))
            .GroupBy(name => name!)
            .Select(g =>( new DimmerStats(){Name= g.Key, Count= g.Count() }))
            .OrderByDescending(x => x.Count)
            .Take(count)];
    }

    /// <summary>
    /// Centralized logic for filtering events by type and date range.
    /// </summary>
    private static IEnumerable<DimmerPlayEvent> FilterEvents(IReadOnlyCollection<DimmerPlayEvent> events, int? playType, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        IEnumerable<DimmerPlayEvent> query = events;

        if (playType.HasValue)
        {
            query = query.Where(e => e.PlayType == playType.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // To be inclusive of the end date, we check for less than the *next* day
            query = query.Where(e => e.EventDate < endDate.Value.AddDays(1));
        }
        return query;
    }

    #endregion
}
