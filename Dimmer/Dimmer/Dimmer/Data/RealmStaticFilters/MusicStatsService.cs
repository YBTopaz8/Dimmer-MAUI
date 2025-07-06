using Realms;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.RealmStaticFilters;

public class MusicStatsService
{
    private Realm _realm;
    public MusicStatsService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
    }

    // A helper to make return types cleaner
    public record SongStat(SongModel Song, int PlayCount);
    public record ArtistStat(ArtistModel Artist, int PlayCount);
    public record AlbumStat(AlbumModel Album, int PlayCount);
    public record GenreStat(string GenreName, int PlayCount);
    public record TimeStat(int HourOfDay, int PlayCount);

    #region Category 1: Current Trends & Listening Habits (Your "For You" Page)

    /// <summary>
    /// Q: "What's my favorite song right now?"
    /// </summary>
    public SongStat? GetMostPlayedSongInLastNDays(int days)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        // 1. RQL Filter: Get all recent 'Play' or 'Completed' events.
        var recentEvents = _realm.All<DimmerPlayEvent>()
            .Filter("PlayType IN {0, 3} AND DatePlayed > $0", sinceDate);

        if (!recentEvents.Any())
            return null;

        // 2. LINQ Compute: Group by song and find the most frequent.
        return recentEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .OrderByDescending(s => s.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Who is my top artist this month?"
    /// </summary>
    public ArtistStat? GetTopArtistInLastNDays(int days)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        if (!recentEvents.Any())
            return null;

        return recentEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new ArtistStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "What albums are in my heavy rotation?"
    /// </summary>
    public List<AlbumStat> GetHeavyRotationAlbums(int days, int minPlays)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        return [.. recentEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Album != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Album)
            .Select(g => new AlbumStat(g.Key, g.Count()))
            .Where(a => a.PlayCount >= minPlays)
            .OrderByDescending(a => a.PlayCount)];
    }

    /// <summary>
    /// Q: "What's my 'On Repeat' playlist right now?" (Songs played multiple times recently)
    /// </summary>
    public List<SongStat> GetOnRepeatPlaylist(int days, int minPlays)
    {
        return GetMostPlayedSongInLastNDays(days, minPlays); // Re-use a more generic version
    }

    /// <summary>
    /// Q: "What genre am I listening to most this week?"
    /// </summary>
    public GenreStat? GetTopGenreInLastNDays(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        if (!recentEvents.Any())
            return null;

        return recentEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Genre?.Name != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Genre.Name)
            .Select(g => new GenreStat(g.Key, g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .FirstOrDefault();
    }

    // (6-10) Variants of the above with different timeframes or filters
    public SongStat? GetFavoritePopSongLastTwoWeeks() => GetFavoriteSongOfGenreInLastNDays("Pop", 14);
    public SongStat? GetFavoriteRockSongLastMonth() => GetFavoriteSongOfGenreInLastNDays("Rock", 30);
    public List<ArtistStat> GetTopArtistsOfYear(int year) { /* similar logic with year filter */ return new(); }
    public double GetAverageListeningTimePerDay(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var completedEvents = _realm.All<DimmerPlayEvent>().Filter("WasPlayCompleted == true AND DatePlayed > $0", sinceDate);
        var totalSeconds = completedEvents.ToList().Sum(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.DurationInSeconds ?? 0);
        return totalSeconds / days;
    }
    public List<TimeStat> GetListeningHabitsByHourOfDay(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);
        return [.. recentEvents.ToList()
            .GroupBy(e => e.DatePlayed.Hour)
            .Select(g => new TimeStat(g.Key, g.Count()))
            .OrderBy(t => t.HourOfDay)];
    }

    #endregion

    #region Category 2: All-Time Statistics & Lifetime Favorites

    /// <summary>
    /// Q: "What is my most played song of all time?"
    /// </summary>
    public SongStat? GetAllTimeMostPlayedSong()
    {
        // RQL Filter: Get all relevant play events.
        var allEvents = _realm.All<DimmerPlayEvent>().Filter("PlayType IN {0, 3}");

        if (!allEvents.Any())
            return null;

        return allEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .OrderByDescending(s => s.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Who is my undisputed favorite artist ever?"
    /// </summary>
    public ArtistStat? GetAllTimeTopArtist()
    {
        var allEvents = _realm.All<DimmerPlayEvent>();

        if (!allEvents.Any())
            return null;

        return allEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new ArtistStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Which album have I listened to the most?"
    /// </summary>
    public AlbumStat? GetAllTimeTopAlbum()
    {
        // Same pattern as above, just grouping by Album
        var allEvents = _realm.All<DimmerPlayEvent>();
        return allEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Album != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Album)
            .Select(g => new AlbumStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "How many hours of music have I listened to in total?"
    /// </summary>
    public double GetTotalListeningHours()
    {
        var completedPlays = _realm.All<DimmerPlayEvent>().Filter("WasPlayCompleted == true");
        double totalSeconds = completedPlays.ToList()
            .Sum(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.DurationInSeconds ?? 0);
        return totalSeconds / 3600.0;
    }

    /// <summary>
    /// Q: "How diverse is my taste? How many unique artists have I played?"
    /// </summary>
    public int GetTotalUniqueArtistsPlayed()
    {
        var allEvents = _realm.All<DimmerPlayEvent>();
        return allEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .Select(e => e.SongsLinkingToThisEvent.First().Artist.Id)
            .Distinct()
            .Count();
    }

    // (16-21) More all-time stats
    public int GetTotalUniqueSongsPlayed() { /* ... similar to above, select Song.Id ... */ return 0; }
    public List<SongStat> GetAllTimeTopNSongs(int n) { /* ... same as top song, just use .Take(n) ... */ return new(); }
    public List<ArtistStat> GetAllTimeTopNArtists(int n) { /* ... same as top artist, just use .Take(n) ... */ return new(); }
    public double GetAverageSongLengthOfLibrary() => _realm.All<SongModel>().ToList().Average(s => s.DurationInSeconds);
    public int GetTotalSongsInLibrary() => _realm.All<SongModel>().Count();
    public GenreStat GetMostSkippedGenre()
    {
        var skippedEvents = _realm.All<DimmerPlayEvent>().Filter("PlayType == 5"); // 5: Skipped
        return skippedEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Genre != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Genre.Name)
            .Select(g => new GenreStat(g.Key, g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .FirstOrDefault();
    }

    #endregion

    #region Category 3: Artist, Album, & Genre Deep Dives

    /// <summary>
    /// Q: "What's the most popular song by [Artist X]?"
    /// </summary>
    public SongStat? GetMostPopularSongByArtist(ObjectId artistId)
    {
        // 1. RQL: Find all songs by this artist first.
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();

        if (artistSongIds.Length==0)
            return null;

        // 2. RQL: Now find all play events for THOSE songs.
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        // 3. LINQ: Compute the result.
        return relevantEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .OrderByDescending(s => s.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Which of [Artist X]'s albums is their 'magnum opus' for me?"
    /// </summary>
    public AlbumStat? GetMostPlayedAlbumByArtist(ObjectId artistId)
    {
        // This follows the same 3-step pattern as the method above.
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();
        if (artistSongIds.Length==0)
            return null;
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        return relevantEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Album != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Album)
            .Select(g => new AlbumStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Show me the 'deep cuts' for [Artist X] - their least played songs."
    /// </summary>
    public List<SongStat> GetArtistDeepCuts(ObjectId artistId)
    {
        // Same as most popular, just order ascending.
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId).ToList();
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds).ToList();

        var playCounts = artistSongs.ToDictionary(
            song => song,
            song => relevantEvents.Count(e => e.SongId == song.Id)
        );

        return [.. playCounts
            .Select(kvp => new SongStat(kvp.Key, kvp.Value))
            .OrderBy(s => s.PlayCount)];
    }

    /// <summary>
    /// Q: "Who are the most popular artists in the [Pop] genre?"
    /// </summary>
    public List<ArtistStat> GetTopArtistsInGenre(string genreName, int topN)
    {
        // 1. RQL: Find all songs in the genre.
        var genreSongs = _realm.All<SongModel>().Filter("Genre.Name ==[c] $0", genreName);
        var genreSongIds = genreSongs.Select(s => (QueryArgument)s.Id).ToArray();

        if (genreSongIds.Length==0)
            return new List<ArtistStat>();

        // 2. RQL: Find all events for those songs.
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", genreSongIds);

        // 3. LINQ: Compute top artists.
        return [.. relevantEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new ArtistStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .Take(topN)];
    }

    // (26-31) More deep dives
    public SongStat GetFirstSongPlayedByArtist(ObjectId artistId) { /* Filter events by artist, sort by DatePlayed ASC */ return null; }
    public SongStat GetLastSongPlayedByArtist(ObjectId artistId) { /* Filter events by artist, sort by DatePlayed DESC */ return null; }
    public double GetPercentageOfListenTimeForGenre(string genreName) { /* Compare total listen seconds to genre listen seconds */ return 0.0; }
    public List<ArtistModel> GetOneHitWonders(int playThreshold) { /* Complex LINQ: artists where one song accounts for >90% of their total plays */ return new(); }
    public List<SongModel> GetSongsByArtistFromDecade(ObjectId artistId, int decadeStartYear) { /* RQL: Filter by artist AND release year range */ return new(); }
    public int GetArtistRankByPlayCount(ObjectId artistId)
    {
        var allArtistStats = GetAllTimeTopNArtists(int.MaxValue);
        var artistStat = allArtistStats.FirstOrDefault(a => a.Artist.Id == artistId);
        return artistStat != null ? allArtistStats.IndexOf(artistStat) + 1 : -1;
    }

    #endregion

    #region Category 4: Discovery & Library Exploration

    /// <summary>
    /// Q: "Find me some 'forgotten gems' I used to love but haven't played this year."
    /// </summary>
    public List<SongModel> GetForgottenGems(int minPlayCount, int daysSinceLastPlay)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-daysSinceLastPlay);
        // A perfect use case for a pure, powerful RQL query.
        var query = "PlayHistory.@count > $0 AND NONE PlayHistory.DatePlayed > $1";
        return [.. _realm.All<SongModel>().Filter(query, minPlayCount, sinceDate)];
    }

    /// <summary>
    /// Q: "Recommend an artist similar to my favorite one." (Genre-based similarity)
    /// </summary>
    public ArtistModel? RecommendArtistSimilarTo(ObjectId favoriteArtistId)
    {
        // 1. Find the favorite artist's top genre.
        var topGenreName = GetTopGenreForArtist(favoriteArtistId);
        if (topGenreName == null)
            return null;

        // 2. Find all artists in that genre.
        var artistsInGenre = _realm.All<ArtistModel>().Filter("ANY Songs.Genre.Name ==[c] $0", topGenreName).ToList();

        // 3. Find which ones I've never played.
        var playedArtistIds = _realm.All<DimmerPlayEvent>().ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .Select(e => e.SongsLinkingToThisEvent.First().Artist.Id)
            .ToHashSet();

        return artistsInGenre
            .FirstOrDefault(a => a.Id != favoriteArtistId && !playedArtistIds.Contains(a.Id));
    }

    /// <summary>
    // Q: "I'm in the mood for short, energetic songs."
    /// </summary>
    public List<SongModel> GetShortEnergeticSongs()
    {
        // Assuming "energetic" is a tag.
        var query = "DurationInSeconds < 180 AND ANY Tags.Name == 'Energetic'";
        return [.. _realm.All<SongModel>().Filter(query).Take(50)];
    }

    // (35-39) More discovery queries
    public List<SongModel> GetRandomAlbum() { /* Get all albums, pick one at random, return its songs */ return new(); }
    public List<SongModel> GetSongsFromThisDayInHistory() { /* Filter events where month and day match today */ return new(); }
    public List<AlbumModel> GetCriticallyAcclaimedAlbums(int minRating) { /* Albums where average song rating > minRating */ return new(); }
    public List<SongModel> GetSongsFromRandomGenre() { /* Get all genres, pick one, get songs */ return new(); }
    public List<SongModel> GetLongestSongsInLibrary(int topN) => [.. _realm.All<SongModel>().OrderByDescending(s => s.DurationInSeconds).Take(topN)];

    #endregion

    #region Category 5: Library Health & Metadata Queries

    /// <summary>
    /// Q: "Find all my songs that aren't attached to an album."
    /// </summary>
    public IQueryable<SongModel> GetSongsWithNoAlbum() => _realm.All<SongModel>().Filter("Album == nil");

    /// <summary>
    /// Q: "Find albums where the tracks have inconsistent release years."
    /// </summary>
    public List<AlbumModel> GetInconsistentAlbums()
    {
        return [.. _realm.All<AlbumModel>().ToList().Where(a => a.SongsInAlbum != null && a.SongsInAlbum.Select(s => s.ReleaseYear).Distinct().Count() > 1)];
    }

    /// <summary>
    /// Q: "Which artists are missing a biography?"
    /// </summary>
    public IQueryable<ArtistModel> GetArtistsMissingBio() => _realm.All<ArtistModel>().Filter("Bio == nil OR Bio == ''");

    /// <summary>
    /// Q: "Find duplicate songs in my library." (Based on Title and Artist name)
    /// </summary>
    public List<IGrouping<string, SongModel>> GetPotentialDuplicateSongs()
    {
        return [.. _realm.All<SongModel>().ToList()
            .GroupBy(s => $"{s.Title.ToLower()}|{s.ArtistName.ToLower()}")
            .Where(g => g.Count() > 1)];
    }

    #endregion

    #region Helper Methods used above
    private List<SongStat> GetMostPlayedSongInLastNDays(int days, int minPlays)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        return [.. recentEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .Where(s => s.PlayCount >= minPlays)
            .OrderByDescending(s => s.PlayCount)];
    }

    private SongStat? GetFavoriteSongOfGenreInLastNDays(string genreName, int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        // RQL Filter: Pre-filter events by date AND song's genre
        var query = "DatePlayed > $0 AND ANY SongsLinkingToThisEvent.Genre.Name ==[c] $1";
        var recentGenreEvents = _realm.All<DimmerPlayEvent>().Filter(query, sinceDate, genreName);

        if (!recentGenreEvents.Any())
            return null;

        return recentGenreEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .OrderByDescending(s => s.PlayCount)
            .FirstOrDefault();
    }

    private string? GetTopGenreForArtist(ObjectId artistId)
    {
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();

        if (artistSongIds.Length==0)
            return null;

        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        return relevantEvents.ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Genre?.Name != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Genre.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
    }
    #endregion
}
/*


A Note on Methodology:

RQL is for Filtering: RQL's primary strength is its ability to efficiently filter large datasets within the database engine.

Multi-Step Operations: Some complex requests, like "get all unique artists from all songs in an album," cannot be resolved in a single RQL filter because RQL does not perform aggregation or collection-flattening like SQL's GROUP BY or JOIN. For these, the process is:

Use a precise RQL query to fetch the necessary parent objects.

Use C# to process the in-memory results (e.g., iterating, flattening, and collecting unique IDs). This is not LINQ for filtering; it's standard C# for data transformation.

Boilerplate Setup

Category 1: Song-Centric Queries

These queries start by filtering the SongModel collection.

Get Songs by Title (Case-Insensitive Wildcard)

Generated csharp
public IQueryable<SongModel> GetSongsByTitle(string searchTerm)
{
    // LIKE is for wildcards: '*' for multiple chars, '?' for single. [c] is case-insensitive.
    return realm.All<SongModel>().Filter("Title LIKE[c] $0", $"*{searchTerm}*");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs by a Minimum Rating

Generated csharp
public IQueryable<SongModel> GetSongsWithMinRating(int minRating)
{
    return realm.All<SongModel>().Filter("Rating >= $0", minRating);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Favorite Songs Released in a Specific Year

Generated csharp
public IQueryable<SongModel> GetFavoriteSongsByYear(int year)
{
    return realm.All<SongModel>().Filter("IsFavorite == true AND ReleaseYear == $0", year);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs Longer Than X Minutes

Generated csharp
public IQueryable<SongModel> GetSongsLongerThanMinutes(double minutes)
{
    var durationInSeconds = minutes * 60;
    return realm.All<SongModel>().Filter("DurationInSeconds > $0", durationInSeconds);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs That Have Synced Lyrics

Generated csharp
public IQueryable<SongModel> GetSongsWithSyncedLyrics()
{
    // Checks for non-empty string.
    return realm.All<SongModel>().Filter("HasSyncedLyrics == true AND SyncLyrics != ''");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs by a Specific File Format

Generated csharp
public IQueryable<SongModel> GetSongsByFileFormat(string format)
{
    return realm.All<SongModel>().Filter("FileFormat ==[c] $0", format);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
Category 2: Artist & Album Queries

Queries focusing on ArtistModel and AlbumModel and their relationships.

Get All Albums by an Artist's Name

Generated csharp
public IQueryable<AlbumModel> GetAlbumsByArtistName(string artistName)
{
    // ANY checks if any object in the collection meets the criteria.
    return realm.All<AlbumModel>().Filter("ANY ArtistIds.Name ==[c] $0", artistName);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get All Artists with a Bio Containing a Keyword

Generated csharp
public IQueryable<ArtistModel> GetArtistsByBioKeyword(string keyword)
{
    // CONTAINS is a powerful substring search.
    return realm.All<ArtistModel>().Filter("Bio CONTAINS[c] $0", keyword);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Albums with a High Number of Tracks

Generated csharp
public IQueryable<AlbumModel> GetAlbumsWithManyTracks(int trackCount)
{
    return realm.All<AlbumModel>().Filter("NumberOfTracks > $0", trackCount);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Albums Released in a Specific Decade

Generated csharp
public IQueryable<AlbumModel> GetAlbumsFromDecade(int startYear)
{
    int endYear = startYear + 9;
    return realm.All<AlbumModel>().Filter("ReleaseYear >= $0 AND ReleaseYear <= $1", startYear, endYear);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Artists With No Associated Songs(Potentially Bad Data)

Generated csharp
public IQueryable<ArtistModel> GetArtistsWithNoSongs()
{
    // @count checks the size of a collection (in this case, a backlink).
    return realm.All<ArtistModel>().Filter("Songs.@count == 0");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Albums With No Songs(Potentially Bad Data)

Generated csharp
public IQueryable<AlbumModel> GetAlbumsWithNoSongs()
{
    return realm.All<AlbumModel>().Filter("SongsInAlbum.@count == 0");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
Category 3: Collection & Relationship Queries(ANY, NONE, SUBQUERY)

These leverage RQL's powerful keywords for querying collections.

Get Songs That Are on at Least One Playlist

Generated csharp
public IQueryable<SongModel> GetSongsOnAnyPlaylist()
{
    // Using the backlink from Song to Playlist.
    return realm.All<SongModel>().Filter("Playlists.@count > 0");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs That Feature a Specific Artist(but not as the main artist)

Generated csharp
public IQueryable<SongModel> GetSongsFeaturingArtist(ObjectId mainArtistId, ObjectId featuredArtistId)
{
    // Query the ArtistIds list for the featured artist, excluding songs by the main one.
    return realm.All<SongModel>().Filter("Artist.Id != $0 AND ANY ArtistIds.Id == $1", mainArtistId, featuredArtistId);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs That Have NO Tags

Generated csharp
public IQueryable<SongModel> GetSongsWithoutTags()
{
    return realm.All<SongModel>().Filter("Tags.@count == 0");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs That Have a Specific Set of Tags(All must be present)

Generated csharp
public IQueryable<SongModel> GetSongsWithAllTags(List<string> tagNames)
{
    // This requires chaining Filter calls, as RQL doesn't have a direct "ALL IN" operator.
    IQueryable<SongModel> query = realm.All<SongModel>();
    for (int i = 0; i < tagNames.Count; i++)
    {
        query = query.Filter($"ANY Tags.Name == ${i}", tagNames[i]);
    }
    return query;
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Albums Containing a Favorite Song

Generated csharp
public IQueryable<AlbumModel> GetAlbumsContainingFavoriteSong()
{
    // Query through the backlink property 'SongsInAlbum'.
    return realm.All<AlbumModel>().Filter("ANY SongsInAlbum.IsFavorite == true");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs Associated With a Pinned User Note

Generated csharp
public IQueryable<SongModel> GetSongsWithPinnedNotes()
{
    // Querying a property on an EmbeddedObject in a list.
    return realm.All<SongModel>().Filter("ANY UserNotes.IsPinned == true");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs by a Specific Composer and Lyricist

Generated csharp
public IQueryable<SongModel> GetSongsByComposerAndLyricist(string composer, string lyricist)
{
    return realm.All<SongModel>().Filter("Composer ==[c] $0 AND Lyricist ==[c] $1", composer, lyricist);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
Category 4: Play History & Event-Based Queries

Queries that dig into user activity via DimmerPlayEvent.

Get Songs Completed at Least N Times

Generated csharp
public IQueryable<SongModel> GetSongsCompletedNTimes(int count)
{
    // SUBQUERY is powerful. It creates a temporary collection to query against.
    // Here, we create a sub-collection of only 'Completed' events and check its size.
    return realm.All<SongModel>().Filter("SUBQUERY(PlayHistory, $event, $event.WasPlayCompleted == true).@count >= $0", count);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs Frequently Skipped(PlayType 5)

Generated csharp
public IQueryable<SongModel> GetFrequentlySkippedSongs(int skipCount)
{
    return realm.All<SongModel>().Filter("SUBQUERY(PlayHistory, $event, $event.PlayType == 5).@count > $0", skipCount);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs Not Played in the Last N Days

Generated csharp
public IQueryable<SongModel> GetSongsNotPlayedRecently(int days)
{
    var date = DateTimeOffset.UtcNow.AddDays(-days);
    // NONE checks that no objects in the collection meet the criteria.
    return realm.All<SongModel>().Filter("NONE PlayHistory.DatePlayed > $0", date);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get All Play Events for a Specific Genre

Generated csharp
public IQueryable<DimmerPlayEvent> GetPlayEventsForGenre(string genreName)
{
    // This requires traversing from the PlayEvent, to the Song, to the Genre.
    // First, get all songs in the genre.
    var songIds = realm.All<SongModel>().Filter("Genre.Name ==[c] $0", genreName).Select(s => s.Id).ToList();

    // Then, use the IN operator to find all play events for those songs.
    return realm.All<DimmerPlayEvent>().Filter("SongId IN $0", songIds);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs Played on a Specific Device Model

Generated csharp
public IQueryable<SongModel> GetSongsPlayedOnDeviceModel(string deviceModel)
{
    return realm.All<SongModel>().Filter("ANY PlayHistory.DeviceModel ==[c] $0", deviceModel);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Most Recently Played Songs(Sorted)

Generated csharp
public IQueryable<DimmerPlayEvent> GetRecentPlayEventsSorted()
{
    // RQL can include sorting. TRUEPREDICATE matches all objects.
    // NOTE: This gets the *events*. You would then process these to get the unique songs.
    return realm.All<DimmerPlayEvent>().Filter("TRUEPREDICATE SORT(DatePlayed DESC)");
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
Category 5: Advanced Multi-Step & Cross-Model Queries

These demonstrate how to combine RQL with C# processing to answer very complex questions.

Get Album Songs by a Single Song ID(Multi-Step)

Generated csharp
public List<SongModel> GetAlbumSongsBySong(ObjectId songId)
{
    // Step 1: Find the single song using its primary key (very fast).
    var song = realm.Find<SongModel>(songId);
    if (song?.Album == null)
    {
        return new List<SongModel>();
    }

    // Step 2: Use the backlink. 'SongsInAlbum' is already a queryable collection of all songs in that album.
    // .ToList() executes the query and returns the results.
    return song.Album.SongsInAlbum.ToList();
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get All Artists in an Album, Found via a Song ID(Multi-Step)

Generated csharp
public List<ArtistModel> GetAllArtistsInAlbumBySongId(ObjectId songId)
{
    // Step 1: Find the song and its album.
    var song = realm.Find<SongModel>(songId);
    if (song?.Album == null)
    {
        return new List<ArtistModel>();
    }

    // Step 2: Use the backlink to get all songs in the album.
    var albumSongsQuery = song.Album.SongsInAlbum;

    // Step 3: Process results in C# to gather all unique artists.
    var allArtists = new Dictionary<ObjectId, ArtistModel>();
    foreach (var albumSong in albumSongsQuery)
    {
        if (albumSong.Artist != null && !allArtists.ContainsKey(albumSong.Artist.Id))
        {
            allArtists.Add(albumSong.Artist.Id, albumSong.Artist);
        }
        foreach (var featuredArtist in albumSong.ArtistIds)
        {
            if (!allArtists.ContainsKey(featuredArtist.Id))
            {
                allArtists.Add(featuredArtist.Id, featuredArtist);
            }
        }
    }
    return allArtists.Values.ToList();
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Find All "Collaborators" of a Given Artist(Multi-Step)

Generated csharp
public List<ArtistModel> GetArtistCollaborators(ObjectId artistId)
{
    // Step 1: Use RQL to find all songs where the artist is either the main or a featured artist.
    var artistSongs = realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId);

    // Step 2: Process results in C# to find all other artists on those tracks.
    var collaborators = new Dictionary<ObjectId, ArtistModel>();
    foreach (var song in artistSongs)
    {
        // Add the main artist if they aren't the one we're searching for
        if (song.Artist != null && song.Artist.Id != artistId && !collaborators.ContainsKey(song.Artist.Id))
        {
            collaborators.Add(song.Artist.Id, song.Artist);
        }
        // Add all featured artists who aren't the one we're searching for
        foreach (var featured in song.ArtistIds)
        {
            if (featured.Id != artistId && !collaborators.ContainsKey(featured.Id))
            {
                collaborators.Add(featured.Id, featured);
            }
        }
    }
    return collaborators.Values.ToList();
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Find Other Artists in the Same Genres as a Given Artist(Multi-Step)

Generated csharp
public List<ArtistModel> GetArtistsInSameGenres(ObjectId artistId)
{
    // Step 1: Get the distinct genre IDs for the given artist.
    var artist = realm.Find<ArtistModel>(artistId);
    if (artist == null)
        return new List<ArtistModel>();

    var genreIds = artist.Songs.Select(s => s.Genre.Id).Distinct().ToList();
    if (!genreIds.Any())
        return new List<ArtistModel>();

    // Step 2: Use RQL with the IN operator to find all other artists who have songs in ANY of those genres.
    var otherArtists = realm.All<ArtistModel>()
        .Filter("Id != $0 AND ANY Songs.Genre.Id IN $1", artistId, genreIds);

    return otherArtists.ToList();
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Get Songs from One Artist on an Album from Another Artist

Generated csharp
public IQueryable<SongModel> GetGuestAppearanceSongs(ObjectId guestArtistId, ObjectId primaryArtistId)
{
    // Find songs by the guest artist that appear on an album where the primary artist is listed.
    return realm.All<SongModel>().Filter("Artist.Id == $0 AND ANY Album.ArtistIds.Id == $1", guestArtistId, primaryArtistId);
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
Category 6: More Assorted Complex Queries

GetUncategorizedSongs: Album == nil AND Genre == nil

GetMultiDiscAlbums: DiscTotal > 1

GetSongsByLyricSnippet: UnSyncLyrics CONTAINS[c] $0

GetSongsWithSyncedLyricsAfterTimestamp: ANY EmbeddedSync.TimestampMs > $0

GetAlbumsByTag: ANY Tags.Name ==[c] $0

GetArtistsByTag: ANY Tags.Name ==[c] $0

GetSongsByMultipleArtists(List<ObjectId> artistIds): Artist.Id IN $0 OR ANY ArtistIds IN $0

GetSongsUpdatedInDateRange: LastDateUpdated > $0 AND LastDateUpdated < $1

GetAlbumsMissingCoverArt: ImagePath == 'musicalbum.png' OR ImagePath == nil

GetSongsFromPlaylistsContainingWord: ANY Playlists.PlaylistName CONTAINS[c] $0

*/