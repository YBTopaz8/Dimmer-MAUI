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
        

        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        // 1. RQL Filter: Get all recent 'Play' or 'Completed' events.
        var recentEvents = _realm.All<DimmerPlayEvent>()
            .Filter("PlayType IN {0, 3} AND DatePlayed > $0", sinceDate);

        if (!recentEvents.Any())
            return null;

        // 2. LINQ Compute: Group by song and find the most frequent.
        return recentEvents.AsEnumerable()
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
        

        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        if (!recentEvents.Any())
            return null;

        return recentEvents.AsEnumerable()
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

        return [.. recentEvents.AsEnumerable()
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

        return recentEvents.AsEnumerable()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Genre?.Name != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Genre.Name)
            .Select(g => new GenreStat(g.Key, g.Count()))
            .OrderByDescending(g => g.PlayCount)
            .FirstOrDefault();
    }

    // (6-10) Variants of the above with different timeframes or filters
    public SongStat? GetFavoritePopSongLastTwoWeeks() => GetFavoriteSongOfGenreInLastNDays("Pop", 14);
    public SongStat? GetFavoriteRockSongLastMonth() => GetFavoriteSongOfGenreInLastNDays("Rock", 30);
     public double GetAverageListeningTimePerDay(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var completedEvents = _realm.All<DimmerPlayEvent>().Filter("WasPlayCompleted == true AND DatePlayed > $0", sinceDate);
        var totalSeconds = completedEvents.AsEnumerable().Sum(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.DurationInSeconds ?? 0);
        return totalSeconds / days;
    }
    public List<TimeStat> GetListeningHabitsByHourOfDay(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);
        return [.. recentEvents.AsEnumerable()
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

        return allEvents.AsEnumerable()
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

        return allEvents.AsEnumerable()
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
        return allEvents.AsEnumerable()
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
        double totalSeconds = completedPlays.AsEnumerable()
            .Sum(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.DurationInSeconds ?? 0);
        return totalSeconds / 3600.0;
    }

    /// <summary>
    /// Q: "How diverse is my taste? How many unique artists have I played?"
    /// </summary>
    public int GetTotalUniqueArtistsPlayed()
    {
        var allEvents = _realm.All<DimmerPlayEvent>();
        return allEvents.AsEnumerable()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .Select(e => e.SongsLinkingToThisEvent.First().Artist.Id)
            .Distinct()
            .Count();
    }

    public double GetAverageSongLengthOfLibrary() => _realm.All<SongModel>().AsEnumerable().Average(s => s.DurationInSeconds);
    public int GetTotalSongsInLibrary() => _realm.All<SongModel>().Count();
    public GenreStat GetMostSkippedGenre()
    {
        var skippedEvents = _realm.All<DimmerPlayEvent>().Filter("PlayType == 5"); // 5: Skipped
        return skippedEvents.AsEnumerable()
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
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistToSong.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();

        if (artistSongIds.Length==0)
            return null;

        // 2. RQL: Now find all play events for THOSE songs.
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        // 3. LINQ: Compute the result.
        return relevantEvents.AsEnumerable()
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
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistToSong.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();
        if (artistSongIds.Length==0)
            return null;
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        return relevantEvents.AsEnumerable()
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
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistToSong.Id == $0", artistId).AsEnumerable();
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();
        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds).AsEnumerable();

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
        return [.. relevantEvents.AsEnumerable()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist)
            .Select(g => new ArtistStat(g.Key, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .Take(topN)];
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
        var artistsInGenre = _realm.All<ArtistModel>().Filter("ANY Songs.Genre.Name ==[c] $0", topGenreName).AsEnumerable();

        // 3. Find which ones I've never played.
        var playedArtistIds = _realm.All<DimmerPlayEvent>().AsEnumerable()
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
   public List<SongModel> GetLongestSongsInLibrary(int topN) => [.. _realm.All<SongModel>().AsEnumerable().OrderByDescending(s => s.DurationInSeconds).Take(topN)];

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
        return [.. _realm.All<AlbumModel>().AsEnumerable().Where(a => a.SongsInAlbum != null && a.SongsInAlbum.Select(s => s.ReleaseYear).Distinct().Count() > 1)];
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
        return [.. _realm.All<SongModel>().AsEnumerable()
            .GroupBy(s => $"{s.Title.ToLower()}|{s.ArtistName.ToLower()}")
            .Where(g => g.Count() > 1)];
    }

    #endregion

    #region Helper Methods used above
    private List<SongStat> GetMostPlayedSongInLastNDays(int days, int minPlays)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate);

        return [.. recentEvents.AsEnumerable()
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

        return recentGenreEvents.AsEnumerable()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault() != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First())
            .Select(g => new SongStat(g.Key, g.Count()))
            .OrderByDescending(s => s.PlayCount)
            .FirstOrDefault();
    }

    private string? GetTopGenreForArtist(ObjectId artistId)
    {
        var artistSongs = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistToSong.Id == $0", artistId);
        var artistSongIds = artistSongs.Select(s => (QueryArgument)s.Id).ToArray();

        if (artistSongIds.Length==0)
            return null;

        var relevantEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds);

        return relevantEvents.AsEnumerable()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Genre?.Name != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Genre.Name)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();
    }
    #endregion


}