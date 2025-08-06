namespace Dimmer.Data.RealmStaticFilters;

public class MusicPowerUserService
{
    private Realm _realm;

    public MusicPowerUserService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
    }
    // Re-using the Stat records for consistency
    public record SongStat(SongModel Song, int PlayCount);
    public record ArtistStat(ArtistModel Artist, int PlayCount);
    public record AlbumStat(AlbumModel Album, int PlayCount);

    #region Category 1: Library Health & Duplicate Detection

    /// <summary>
    /// Q: "Find potential duplicate songs that have the same title and artist but different file paths."
    /// </summary>
    public List<IGrouping<string, SongModel>> GetPotentialDuplicateSongs()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        // RQL can't do this. This is a classic case for LINQ's GroupBy after fetching all songs.
        // It's an expensive operation, so it should be used sparingly.
        return [.. _realm.All<SongModel>().ToList()
            .GroupBy(s => $"{s.Title.ToLower().Trim()}|{s.ArtistName.ToLower().Trim()}")
            .Where(g => g.Count() > 1)];
    }

    /// <summary>
    /// Q: "Find 'ghost' play events that are linked to a song that no longer exists in the database."
    /// </summary>
    public List<DimmerPlayEvent> GetOrphanedPlayEvents()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        // RQL Filter: Find all events where the backlink to a song is empty.
        var query = "SongsLinkingToThisEvent.@count == 0";
        return [.. _realm.All<DimmerPlayEvent>().Filter(query)];
    }

    /// <summary>
    /// Q: "Show me all albums where the number of tracks I have doesn't match the album's official track total."
    /// </summary>
    public List<AlbumModel> GetIncompleteAlbums()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        // RQL Filter: A simple check on properties.
        return [.. _realm.All<AlbumModel>().Filter("TrackTotal > 0 AND SongsInAlbum.@count != TrackTotal")];
    }

    /// <summary>
    /// Q: "Which songs have a rating but have never been played to completion?"
    /// </summary>
    public List<SongModel> GetRatedButUnfinishedSongs()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        // RQL: Find songs with a rating > 0 AND no completed play events.
        var query = "Rating > 0 AND NONE PlayHistory.WasPlayCompleted == true";
        return [.. _realm.All<SongModel>().Filter(query)];
    }

    /// <summary>
    /// Q: "Find songs whose file paths are no longer valid."
    /// </summary>
    public List<SongModel> GetSongsWithMissingFiles()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        return [.. _realm.All<SongModel>().Filter("IsFileExists == false")];
    }

    // (6-10) More health checks
    public List<AlbumModel> GetAlbumsMissingCoverArt()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        return [.. _realm.All<AlbumModel>().Filter("ImagePath == nil OR ImagePath == 'musicalbum.png'")];
    }

    public List<ArtistModel> GetArtistsWithNoSongs() => [.. _realm.All<ArtistModel>().Filter("Songs.@count == 0")];
    public List<GenreModel> GetGenresWithNoSongs() => [.. _realm.All<GenreModel>().Filter("Songs.@count == 0")];
    public List<SongModel> GetSongsWithNoGenre() => [.. _realm.All<SongModel>().Filter("Genre == nil")];
    public List<SongModel> GetSongsWithLowBitrate(int maxBitrate) => [.. _realm.All<SongModel>().Filter("BitRate > 0 AND BitRate < $0", maxBitrate)];

    #endregion

    #region Category 2: Advanced User Behavior & Taste Profiling

    /// <summary>
    /// Q: "Am I a 'skipper' or a 'completer'? Calculate my skip-to-completion ratio."
    /// </summary>
    public double GetSkipToCompletionRatio()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        // RQL can get the two counts separately.
        double skipCount = _realm.All<DimmerPlayEvent>().Filter("PlayType == 5").Count();
        double completeCount = _realm.All<DimmerPlayEvent>().Filter("WasPlayCompleted == true").Count();

        return completeCount > 0 ? skipCount / completeCount : skipCount; // Avoid division by zero
    }

    /// <summary>
    /// Q: "What's my 'musical comfort zone'? The most common decade I listen to."
    /// </summary>
    public string GetPrimaryListeningDecade()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var allEvents = _realm.All<DimmerPlayEvent>();

        if (!allEvents.Any())
            return "N/A";

        var decade = allEvents.ToList()
            .Select(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.ReleaseYear)
            .Where(year => year.HasValue && year > 1900)
            .Select(year => (year.Value / 10) * 10) // Group by decade
            .GroupBy(d => d)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}s")
            .FirstOrDefault();

        return decade ?? "N/A";
    }

    /// <summary>
    /// Q: "Find my 'One-Hit Wonders' - artists where one song dominates their play count."
    /// </summary>
    public List<(ArtistModel Artist, SongModel HitSong, double DominancePercentage)> GetUserOneHitWonders(double dominanceThreshold = 0.8)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var results = new List<(ArtistModel, SongModel, double)>();
        var allArtistEvents = _realm.All<DimmerPlayEvent>().ToList()
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist);

        foreach (var artistGroup in allArtistEvents)
        {
            var artist = artistGroup.Key;
            var totalPlays = artistGroup.Count();

            var topSong = artistGroup
                .GroupBy(e => e.SongsLinkingToThisEvent.First())
                .Select(g => new { Song = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            if (topSong != null && totalPlays > 10) // Ensure enough data
            {
                double dominance = (double)topSong.Count / totalPlays;
                if (dominance >= dominanceThreshold)
                {
                    results.Add((artist, topSong.Song, dominance));
                }
            }
        }
        return results;
    }

    /// <summary>
    /// Q: "Which artists did I discover and then abandon quickly?"
    /// </summary>
    public List<ArtistModel> GetAbandonedArtists(int maxPlays, int daysSinceFirstPlay)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var artistsToConsider = _realm.All<DimmerPlayEvent>().ToList()
            .GroupBy(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist)
            .Where(g => g.Key != null && g.Count() <= maxPlays)
            .Select(g => g.Key);

        var abandonedArtists = new List<ArtistModel>();
        var cutOffDate = DateTimeOffset.UtcNow.AddDays(-daysSinceFirstPlay);

        foreach (var artist in artistsToConsider)
        {
            var firstPlay = _realm.All<DimmerPlayEvent>()
                .Filter("ANY SongsLinkingToThisEvent.Artist.Id == $0", artist.Id)
                .OrderBy(e => e.DatePlayed)
                .FirstOrDefault();

            if (firstPlay != null && firstPlay.DatePlayed < cutOffDate)
            {
                abandonedArtists.Add(artist);
            }
        }
        return abandonedArtists;
    }

    /// <summary>
    /// Q: "Calculate a 'Musical Adventurousness' score - percentage of listening time spent on new artists this month."
    /// </summary>
    public double GetAdventurousnessScore(int days)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);
        var recentEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0").ToList();
        if (recentEvents.Count == 0)
            return 0.0;

        var priorPlayedArtistIds = _realm.All<DimmerPlayEvent>().Filter("DatePlayed < $0", sinceDate).ToList()
            .Select(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist?.Id)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToHashSet();

        int newArtistPlays = recentEvents
            .Count(e =>
            {
                var artistId = e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist?.Id;
                return artistId.HasValue && !priorPlayedArtistIds.Contains(artistId.Value);
            });

        return (double)newArtistPlays / recentEvents.Count;
    }

    // (16-25) More behavior profiling
    public double GetAverageRatingOfFavorites() => _realm.All<SongModel>().Filter("IsFavorite == true AND Rating > 0").ToList().Average(s => s.Rating);
    public string GetMostCommonArtistInPlaylists() { /* LINQ: Group playlist songs by artist and count */ return ""; }
    public List<SongStat> GetGuiltyPleasures(int maxRating, int minPlays) { /* RQL + LINQ: Songs with low rating but high play count */ return new(); }
    public List<ArtistStat> GetArtistsOnTheRise(int days) { /* LINQ: Compare last 30 days of plays to previous 30 days */ return new(); }
    public List<SongModel> GetSongsYouAlwaysSkip() { /* LINQ: Calculate skip rate per song */ return new(); }
    public int GetLibraryChurnRate() { /* LINQ: (Songs added this year) / (Total songs) */ return 0; }
    public string GetDayOfWeekWithMostListens() { /* LINQ: Group events by DayOfWeek and count */ return ""; }
    public List<SongModel> GetSongsYouPutOnRepeat() { /* RQL + LINQ: Find events for the same song within a short time window */ return new(); }
    public List<AlbumModel> GetAlbumsYouListenToInOrder() { /* Complex LINQ: Analyze play history for sequential track numbers from the same album */ return new(); }
    public List<ArtistModel> GetMostFeaturedArtist() { /* RQL + LINQ: Group songs by ArtistToSong list and count */ return new(); }

    #endregion

    #region Category 3: Customization & Dynamic Playlist Generation

    /// <summary>
    /// Q: "Create a 'Time Capsule' playlist of what I was listening to this time last year."
    /// </summary>
    public List<SongModel> CreateTimeCapsulePlaylist(DateTimeOffset targetDate, int dayRange)
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var startDate = targetDate.AddDays(-dayRange);
        var endDate = targetDate.AddDays(dayRange);
        var query = "DatePlayed > $0 AND DatePlayed < $1";

        return [.. _realm.All<DimmerPlayEvent>().Filter(query, startDate, endDate).ToList()
            .Select(e => e.SongsLinkingToThisEvent.FirstOrDefault())
            .Where(s => s != null)
            .Distinct()];
    }

    /// <summary>
    /// Q: "Make a 'Focus' playlist: long, instrumental songs I haven't heard in a while."
    /// </summary>
    public List<SongModel> CreateFocusPlaylist(double minDurationSeconds, int daysSinceLastPlay)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-daysSinceLastPlay);
        // RQL Filter for instrumental songs over a certain length
        var query = "DurationInSeconds > $0 AND HasLyrics == false AND NONE PlayHistory.DatePlayed > $1";
        return [.. _realm.All<SongModel>().Filter(query, minDurationSeconds, sinceDate)];
    }


    /// <summary>
    /// Q: "Create a 'Sunday Morning' playlist: mellow songs with a high personal rating."
    /// </summary>
    public List<SongModel> CreateSundayMorningPlaylist()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var query = "Rating >= 4 AND ANY Tags.Name IN {'Mellow', 'Acoustic', 'Chill'}";
        return [.. _realm.All<SongModel>().Filter(query)
            .ToList()
            .OrderBy(s => s.PlayHistory.LastOrDefault()?.DatePlayed ?? DateTimeOffset.MinValue) // Prioritize less recently played
            .Take(50)];
    }

    // (30-38) More dynamic playlists
    public List<SongModel> CreateArtistDiscoveryPlaylist(ObjectId artistId) { /* Songs from artists in the same genres as artistId that you haven't played */ return new(); }
    public List<SongModel> CreateDeepCutPlaylist(ObjectId artistId) { /* An artist's least-played songs */ return new(); }
    public List<SongModel> CreateSoundtrackOfMyYearPlaylist(int year) { /* Your most played songs from a specific year */ return new(); }
    public List<SongModel> CreateThrowbackPlaylist(int startYear, int endYear) { /* Popular songs from a specific era in your listening history */ return new(); }
    public List<SongModel> CreateCollaborationsPlaylist() { /* Songs where ArtistToSong list is not empty */ return new(); }
    public List<SongModel> CreateHighRatedAndUnheardPlaylist() { /* Songs with Rating >= 4 and PlayHistory.@count == 0 */ return new(); }
    public List<SongModel> CreateForgottenFavoritesPlaylist() { /* Songs with high play count but not played in last 6 months */ return new(); }
    public List<SongModel> CreateGlobalTopHitsPlaylist() { /* This would require external data, but you could simulate it by finding the most played songs across all users if you had a 'UserID' field */ return new(); }
    public List<SongModel> CreateAlbumAnniversaryPlaylist(string albumName, int years) { /* Find album by name, check if its release anniversary is today */ return new(); }

    #endregion

    #region Category 4: Statistical Edge Cases & Fun Facts

    /// <summary>
    /// Q: "What's the longest song I've ever sat through completely?"
    /// </summary>
    public SongModel? GetLongestCompletedSong()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var completedEvents = _realm.All<DimmerPlayEvent>().Filter("WasPlayCompleted == true").ToList();
        return completedEvents
            .Select(e => e.SongsLinkingToThisEvent.FirstOrDefault())
            .Where(s => s != null)
            .OrderByDescending(s => s.DurationInSeconds)
            .FirstOrDefault();
    }

    /// <summary>
    /// Q: "Which artist has the most diverse genres in my library?"
    /// </summary>
    public (ArtistModel? Artist, int GenreCount) GetMostDiverseArtist()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var artistAndGenreCount = _realm.All<ArtistModel>().ToList()
            .Select(a => new
            {
                Artist = a,
                GenreCount = a.Songs.Select(s => s.Genre.Name).Where(n => n != null).Distinct().Count()
            })
            .OrderByDescending(x => x.GenreCount)
            .FirstOrDefault();

        return (artistAndGenreCount?.Artist, artistAndGenreCount?.GenreCount ?? 0);
    }

    // (41-49) More fun facts
    public SongModel? GetShortestSongInLibrary() => _realm.All<SongModel>().OrderBy(s => s.DurationInSeconds).FirstOrDefault();
    public (int Year, int Count) GetYearWithMostSongsAdded() { /* Group songs by DateCreated.Year and count */ return (0, 0); }
    public double GetAveragePlaysBeforeFavorite() { /* LINQ: For favorited songs, find plays before IsFavorite was set (requires date field on favorite) */ return 0.0; }
    public SongModel? GetSongWithMostComposers() { /* LINQ: Find song where Composer string has most separators (e.g., ';') */ return null; }
    public string GetMostCommonLanguageInLibrary() { /* LINQ: Group songs by Language and count */ return ""; }
    public ArtistModel? GetArtistWithLongestAverageSongLength() { /* LINQ: Group songs by artist, calculate avg duration, order */ return null; }
    public (DateTimeOffset Date, int Count) GetDayWithMostMusicListened() { /* LINQ: Group events by date and sum song durations */ return (DateTimeOffset.MinValue, 0); }
    public AlbumModel? GetAlbumWithMostDrasticRatingChange() { /* LINQ: Find album with highest standard deviation in song ratings */ return null; }
    public (SongModel? Song, int SeekCount) GetMostSeekedSong()
    {
        _realm=IPlatformApplication.Current.Services.GetService<IRealmFactory>().GetRealmInstance();

        var seekEvents = _realm.All<DimmerPlayEvent>().Filter("PlayType == 4").ToList(); // 4: Seeked
        var mostSeeked = seekEvents
            .GroupBy(e => e.SongsLinkingToThisEvent.FirstOrDefault())
            .Where(g => g.Key != null)
            .Select(g => new { Song = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        return (mostSeeked?.Song, mostSeeked?.Count ?? 0);
    }

    #endregion
}

