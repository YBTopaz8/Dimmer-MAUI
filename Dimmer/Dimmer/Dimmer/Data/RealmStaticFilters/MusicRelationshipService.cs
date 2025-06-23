using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.RealmStaticFilters;

public class MusicRelationshipService
{
    private readonly Realm _realm;

    public MusicRelationshipService(IRealmFactory factory)
    {
        _realm = factory.GetRealmInstance();
    }
    // Common record types for clean returns
    public record RelationshipStat<T>(T Item, int PlayCount, DateTimeOffset FirstPlayed, DateTimeOffset LastPlayed);
    public record TrendStat(string Period, int PlayCount, int ChangeVsPrevious);
    public record SongDiscovery(SongModel Song, DateTimeOffset DiscoveredDate);
    public record SongRecommendation(SongModel Song, string Reason, double Score);

    #region 1. The User & The Song(s)

    /// <summary>
    /// Q: "What's my relationship with this specific song?"
    /// </summary>
    public RelationshipStat<SongModel>? GetUserSongRelationship(ObjectId songId)
    {
        var song = _realm.Find<SongModel>(songId);
        if (song == null)
            return null;

        var plays = song.PlayHistory.ToList();
        if (!plays.Any())
            return new RelationshipStat<SongModel>(song, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue);

        return new RelationshipStat<SongModel>(
            song,
            plays.Count,
            plays.Min(p => p.DatePlayed),
            plays.Max(p => p.DatePlayed)
        );
    }

    /// <summary>
    /// Q: "What new songs did I discover this month?"
    /// </summary>
    public List<SongDiscovery> GetNewSongDiscoveries(int days)
    {
        var sinceDate = DateTimeOffset.UtcNow.AddDays(-days);

        // Find all songs played for the first time within the date range.
        var allEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed > $0", sinceDate).ToList();
        var priorEvents = _realm.All<DimmerPlayEvent>().Filter("DatePlayed <= $0", sinceDate).ToList()
            .Select(e => e.SongId).Where(id => id.HasValue).ToHashSet();

        return allEvents
            .Where(e => !priorEvents.Contains(e.SongId))
            .GroupBy(e => e.SongsLinkingToThisEvent.FirstOrDefault())
            .Where(g => g.Key != null)
            .Select(g => new SongDiscovery(g.Key, g.Min(e => e.DatePlayed)))
            .OrderBy(d => d.DiscoveredDate)
            .ToList();
    }

    /// <summary>
    /// Q: "How did my listening for this song change last week vs. the week before?"
    /// </summary>
    public List<TrendStat> GetSongWeeklyTrend(ObjectId songId)
    {
        var trends = new List<TrendStat>();
        var songEvents = _realm.All<DimmerPlayEvent>().Filter("SongId == $0", songId).ToList();

        for (int i = 0; i < 4; i++) // Last 4 weeks
        {
            var end = DateTimeOffset.UtcNow.AddDays(-7 * i);
            var start = end.AddDays(-7);
            var prevStart = start.AddDays(-7);

            int currentWeekPlays = songEvents.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prevWeekPlays = songEvents.Count(e => e.DatePlayed > prevStart && e.DatePlayed <= start);

            trends.Add(new TrendStat(
                $"{start:MMM d}-{end:MMM d}",
                currentWeekPlays,
                currentWeekPlays - prevWeekPlays
            ));
        }
        return trends;
    }

    /// <summary>
    /// Q: "Show me my monthly plays for this song over the last year."
    /// </summary>
    public List<TrendStat> GetSongMonthlyTrend(ObjectId songId)
    {
        var trends = new List<TrendStat>();
        var songEvents = _realm.All<DimmerPlayEvent>().Filter("SongId == $0", songId).ToList();

        for (int i = 0; i < 12; i++) // Last 12 months
        {
            var end = DateTimeOffset.UtcNow.AddMonths(-i);
            var start = end.AddMonths(-1);
            var prevStart = start.AddMonths(-1);

            int currentMonthPlays = songEvents.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prevMonthPlays = songEvents.Count(e => e.DatePlayed > prevStart && e.DatePlayed <= start);

            trends.Add(new TrendStat(
                end.ToString("yyyy-MM"),
                currentMonthPlays,
                currentMonthPlays - prevMonthPlays
            ));
        }
        return trends;
    }

    /// <summary>
    /// Q: "Compare my listening stats for this song between two specific dates."
    /// </summary>
    public (int PlayCount, int Skips, double CompletionRate) GetSongStatsBetweenDates(ObjectId songId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var query = "SongId == $0 AND DatePlayed > $1 AND DatePlayed <= $2";
        var events = _realm.All<DimmerPlayEvent>().Filter(query, songId, startDate, endDate).ToList();

        int totalPlays = events.Count(e => e.PlayType == 0 || e.PlayType == 3);
        int skips = events.Count(e => e.PlayType == 5);
        int completions = events.Count(e => e.WasPlayCompleted);

        return (events.Count, skips, totalPlays > 0 ? (double)completions / totalPlays : 0.0);
    }

    // (6-10) More User-Song methods
    public List<SongDiscovery> GetTopDiscoveriesOfMonth(int year, int month) { /* Filter GetNewSongDiscoveries by month and order by plays */ return new(); }
    public SongModel? GetSongThatHookedMeOnAnArtist(ObjectId artistId) => _realm.All<DimmerPlayEvent>().Filter("ANY SongsLinkingToThisEvent.Artist.Id == $0", artistId).OrderBy(e => e.DatePlayed).FirstOrDefault()?.SongsLinkingToThisEvent.FirstOrDefault();
    public SongModel? GetMyMostRatedSong() => _realm.All<SongModel>().ToList().OrderByDescending(s => s.Rating).FirstOrDefault();
    public List<SongModel> GetBuriedTreasures() { var query = "Rating >= 4 AND PlayHistory.@count < 3"; return _realm.All<SongModel>().Filter(query).ToList(); }
    public List<SongModel> GetSongsIShareTheMost() { /* Conceptual: requires a "ShareEvent" similar to DimmerPlayEvent */ return new(); }

    #endregion

    #region 2. The User & The Artist

    /// <summary>
    /// Q: "Tell me everything about my history with this artist."
    /// </summary>
    public (RelationshipStat<ArtistModel>? CoreStats, SongModel? FirstSong, SongModel? TopSong) GetUserArtistRelationship(ObjectId artistId)
    {
        var artist = _realm.Find<ArtistModel>(artistId);
        if (artist == null)
            return (null, null, null);

        var artistSongIds = artist.Songs.Select(s => (QueryArgument)s.Id).ToArray();
        var plays = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds).ToList();

        if (!plays.Any())
            return (new RelationshipStat<ArtistModel>(artist, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue), null, null);

        var coreStats = new RelationshipStat<ArtistModel>(
            artist,
            plays.Count,
            plays.Min(p => p.DatePlayed),
            plays.Max(p => p.DatePlayed)
        );

        var firstSong = plays.OrderBy(p => p.DatePlayed).First().SongsLinkingToThisEvent.FirstOrDefault();
        var topSong = plays.GroupBy(p => p.SongId).OrderByDescending(g => g.Count()).Select(g => _realm.Find<SongModel>(g.Key.Value)).FirstOrDefault();

        return (coreStats, firstSong, topSong);
    }

    /// <summary>
    /// Q: "How many new artists have I discovered this year vs. last year?"
    /// </summary>
    public (int ThisYear, int LastYear) GetNewArtistDiscoveryComparison()
    {
        var thisYearStart = new DateTimeOffset(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var lastYearStart = thisYearStart.AddYears(-1);

        var allEvents = _realm.All<DimmerPlayEvent>().ToList();
        var artistFirstPlay = allEvents
            .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
            .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist.Id)
            .ToDictionary(g => g.Key, g => g.Min(e => e.DatePlayed));

        int thisYearDiscoveries = artistFirstPlay.Count(kvp => kvp.Value >= thisYearStart);
        int lastYearDiscoveries = artistFirstPlay.Count(kvp => kvp.Value >= lastYearStart && kvp.Value < thisYearStart);

        return (thisYearDiscoveries, lastYearDiscoveries);
    }

    /// <summary>
    /// Q: "Show me my weekly play count for this artist."
    /// </summary>
    public List<TrendStat> GetArtistWeeklyTrend(ObjectId artistId)
    {
        var artistSongIds = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId).Select(s => (QueryArgument)s.Id).ToArray();
        var trends = new List<TrendStat>();
        var artistEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds).ToList();

        // Duplicates GetSongWeeklyTrend logic, but on a larger event set
        for (int i = 0; i < 4; i++)
        {
            var end = DateTimeOffset.UtcNow.AddDays(-7 * i);
            var start = end.AddDays(-7);
            var prevStart = start.AddDays(-7);
            int currentWeekPlays = artistEvents.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prevWeekPlays = artistEvents.Count(e => e.DatePlayed > prevStart && e.DatePlayed <= start);
            trends.Add(new TrendStat($"{start:MMM d}-{end:MMM d}", currentWeekPlays, currentWeekPlays - prevWeekPlays));
        }
        return trends;
    }

    /// <summary>
    /// Q: "What's my 'binge score' for this artist? The most songs I've played by them in a single day."
    /// </summary>
    public (DateTimeOffset Date, int PlayCount) GetArtistBingeScore(ObjectId artistId)
    {
        var artistSongIds = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId)
            .ToArray().Select(s => (QueryArgument)s.Id).ToArray();
        var artistEvents = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", artistSongIds).ToList();

        if (!artistEvents.Any())
            return (DateTimeOffset.MinValue, 0);

        return artistEvents
            .GroupBy(e => e.DatePlayed.Date)
            .Select(g => (Date: g.Key, PlayCount: g.Count()))
            .OrderByDescending(x => x.PlayCount)
            .Select(x => (new DateTimeOffset(x.Date), x.PlayCount))
            .FirstOrDefault();
    }

    // (15-20) More User-Artist methods
    public List<TrendStat> GetArtistMonthlyTrend(ObjectId artistId) { /* Logic similar to weekly trend */ return new(); }
    public (int PlayCount, int SongsPlayed, int AlbumsPlayed) GetArtistStatsBetweenDates(ObjectId artistId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        //var artistSongIds = _realm.All<SongModel>().Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistId).Select(s => (QueryArgument)s.Id).ToArray();
        //var query = "SongId IN $0 AND DatePlayed > $1 AND DatePlayed <= $2";
        //var events = _realm.All<DimmerPlayEvent>().Filter(query, artistSongIds, startDate, endDate).ToList();
        //return (
        //    events.Count,
        //    events.Select(e => e.SongId).Distinct().Count(),
        //    events.Select(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Album?.Id).Distinct().Count()
        //);
        return (0, 0, 0);
    }
    public double GetArtistLoyaltyIndex(ObjectId artistId)
    {
        var artistPlays = _realm.All<DimmerPlayEvent>().Filter("ANY SongsLinkingToThisEvent.Artist.Id == $0", artistId).Count();
        var totalPlays = _realm.All<DimmerPlayEvent>().Count();
        return totalPlays > 0 ? (double)artistPlays / totalPlays : 0.0;
    }
    public List<ArtistModel> GetMyCoreArtists(int topN) => _realm.All<DimmerPlayEvent>().ToList()
        .Where(e => e.SongsLinkingToThisEvent.FirstOrDefault()?.Artist != null)
        .GroupBy(e => e.SongsLinkingToThisEvent.First().Artist).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(topN).ToList();
    //public List<ArtistDiscovery> GetNewArtistDiscoveries(int days) { /* Logic similar to song discoveries */ return new(); }
    public ArtistModel? GetArtistWhoseCatalogIHaveExploredTheMost()
    {
        return _realm.All<ArtistModel>().ToList()
            .Where(a => a.Songs.Any())
            .Select(a => new {
                Artist = a,
                Exploration = (double)_realm.All<DimmerPlayEvent>().Filter("ANY SongsLinkingToThisEvent.Artist.Id == $0", a.Id).ToArray()
                                .Select(e => e.SongId).Distinct().Count() / a.Songs.Count()
            })
            .OrderByDescending(x => x.Exploration)
            .FirstOrDefault()?.Artist;
    }

    #endregion

    #region 3. The User & The Album

    // (21-28) User-Album Methods: These follow the same pattern as the Artist methods.
    public (RelationshipStat<AlbumModel>? CoreStats, double CompletionRate) GetUserAlbumRelationship(ObjectId albumId)
    {
        var album = _realm.Find<AlbumModel>(albumId);
        if (album == null)
            return (null, 0);

        var albumSongIds = album.SongsInAlbum.Select(s => (QueryArgument)s.Id).ToArray();
        var plays = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", albumSongIds).ToList();
        if (!plays.Any())
            return (new RelationshipStat<AlbumModel>(album, 0, DateTimeOffset.MinValue, DateTimeOffset.MinValue), 0);

        var coreStats = new RelationshipStat<AlbumModel>(album, plays.Count, plays.Min(p => p.DatePlayed), plays.Max(p => p.DatePlayed));
        var completions = plays.Count(p => p.WasPlayCompleted);

        return (coreStats, plays.Count > 0 ? (double)completions / plays.Count : 0);
    }

    public List<TrendStat> GetAlbumWeeklyTrend(ObjectId albumId) { /* Similar to artist/song */ return new(); }
    public List<TrendStat> GetAlbumMonthlyTrend(ObjectId albumId) { /* Similar to artist/song */ return new(); }
    public (int PlayCount, int UniqueSongsPlayed) GetAlbumStatsBetweenDates(ObjectId albumId, DateTimeOffset startDate, DateTimeOffset endDate) { /* Similar to artist/song */ return (0, 0); }
    public List<AlbumModel> GetNewAlbumDiscoveries(int days) { /* Albums where first play of any song was within N days */ return new(); }
    public AlbumModel? GetAlbumOfTheYearForUser(int year) { /* Find album with most plays in a given year */ return null; }
    public List<AlbumModel> GetAlbumsIListenToFrontToBack() { /* Very complex: analyze play history for sequential track numbers */ return new(); }
    public List<AlbumModel> GetMyDesertIslandDiscs(int topN) { /* Simply the top N most played albums */ return new(); }

    #endregion

    #region 4. The User & The Genre

    // (29-36) User-Genre Methods: Similar patterns, grouping by Genre.Name string.
    public (int PlayCount, int UniqueArtists, int UniqueSongs) GetUserGenreRelationship(string genreName)
    {
        var genreSongs = _realm.All<SongModel>().Filter("Genre.Name ==[c] $0", genreName).Select(s => (QueryArgument)s.Id).ToArray();
        var plays = _realm.All<DimmerPlayEvent>().Filter("SongId IN $0", genreSongs).ToList();

        return (
            plays.Count,
            plays.Select(p => p.SongsLinkingToThisEvent.FirstOrDefault()?.Artist?.Id).Distinct().Count(),
            plays.Select(p => p.SongId).Distinct().Count()
        );
    }

    public List<TrendStat> GetGenreWeeklyTrend(string genreName) { /* ... */ return new(); }
    public List<TrendStat> GetGenreMonthlyTrend(string genreName) { /* ... */ return new(); }
    public (int PlayCount, int UniqueArtists) GetGenreStatsBetweenDates(string genreName, DateTimeOffset startDate, DateTimeOffset endDate) { /* ... */ return (0, 0); }
    public List<string> GetMyNicheGenres(int maxArtistCount)
    {
        return _realm.All<GenreModel>().ToList()
            .Where(g => g.Songs.Select(s => s.Artist.Id).Distinct().Count() <= maxArtistCount)
            .Select(g => g.Name)
            .ToList();
    }
    public string? GetGatewayGenre() { /* Genre of the songs played most in the user's first month */ return null; }
    public List<string> GetMyEvergreenGenres() { /* Genres with consistent play counts across multiple years */ return new(); }
    public (string? Rising, string? Fading) GetGenreMomentum() { /* Similar to artist momentum */ return (null, null); }

    #endregion

    #region 5. The User & The Playlist (Intelligent Suggestions)

    /// <summary>
    /// Q: "Based on this 'Chill' playlist, what songs should I add?"
    /// Suggests songs with similar tags, from artists already on the playlist, that are NOT on the playlist.
    /// </summary>
    public List<SongRecommendation> SuggestSongsForPlaylist(ObjectId playlistId, int limit = 10)
    {
        var playlist = _realm.Find<PlaylistModel>(playlistId);
        if (playlist == null || !playlist.SongsInPlaylist.Any())
            return new List<SongRecommendation>();

        var songsOnPlaylist = playlist.SongsInPlaylist.ToList();
        var songIdsOnPlaylist = songsOnPlaylist.Select(s => s.Id).ToHashSet();

        // --- Strategy 1: Find songs from artists already on the playlist ---
        var artistsOnPlaylist = songsOnPlaylist.Select(s => s.Artist?.Id).Where(id => id.HasValue).Select(id => (QueryArgument)id.Value).Distinct().ToArray();
        var candidateSongsFromArtists = _realm.All<SongModel>()
            .Filter("Artist.Id IN $0", artistsOnPlaylist).ToList()
            .Where(s => !songIdsOnPlaylist.Contains(s.Id));

        var recommendations = candidateSongsFromArtists
            .Select(s => new SongRecommendation(s, $"From a similar artist: {s.ArtistName}", 0.7))
            .ToList();

        // --- Strategy 2: Find songs with matching "mood" tags ---
        var tagsOnPlaylist = songsOnPlaylist.SelectMany(s => s.Tags.Select(t => t.Name)).GroupBy(t => t).OrderByDescending(g => g.Count()).Select(g => (QueryArgument)g.Key).Take(3).ToArray();
        if (tagsOnPlaylist.Any())
        {
            var candidateSongsFromTags = _realm.All<SongModel>().Filter("ANY Tags.Name IN $0", tagsOnPlaylist).ToList().Where(s => !songIdsOnPlaylist.Contains(s.Id));
            recommendations.AddRange(candidateSongsFromTags.Select(s => new SongRecommendation(s, $"Has a similar vibe ({tagsOnPlaylist.First()})", 0.6)));
        }

        // --- Finalize and Score ---
        return recommendations
            .GroupBy(r => r.Song.Id) // Remove duplicates
            .Select(g => g.First()) // Could be more sophisticated by combining scores
            .OrderByDescending(r => r.Song.PlayHistory.Count(p => p.WasPlayCompleted)) // Prioritize popular songs
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Q: "Is this playlist getting stale? Calculate its 'freshness'."
    /// </summary>
    public double GetPlaylistFreshnessScore(ObjectId playlistId)
    {
        var playlist = _realm.Find<PlaylistModel>(playlistId);
        if (playlist == null || !playlist.SongsInPlaylist.Any())
            return 0.0;

        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var plays = _realm.All<DimmerPlayEvent>()
            .Filter("ANY SongsLinkingToThisEvent.Playlists.Id == $0 AND DatePlayed > $1", playlistId, thirtyDaysAgo)
            .ToList();

        if (plays.Count == 0)
            return 0.0;

        // Freshness = % of unique songs played from the playlist recently
        int uniqueSongsPlayed = plays.Select(p => p.SongId).Distinct().Count();
        return (double)uniqueSongsPlayed / playlist.SongsInPlaylist.Count();
    }

    // (39-58) More Playlist and Suggestion methods
    public List<SongModel> GetPlaylistDeepCuts(ObjectId playlistId) { /* Songs on the playlist with the fewest plays */ return new(); }
    public SongModel? GetPlaylistAnthem(ObjectId playlistId) { /* Song on the playlist with the most plays */ return null; }
    public (SongModel? SongToAdd, SongModel? SongToRemove) SuggestPlaylistSwap(ObjectId playlistId) { /* Find most-skipped song to remove, suggest a similar but unplayed song to add */ return (null, null); }
    public List<SongModel> CreatePlaylistFromSong(ObjectId songId, int size) { /* Find similar artists and tagged songs based on a seed song */ return new(); }
    public List<SongModel> CreatePlaylistFromArtist(ObjectId artistId, int size) { /* Mix of artist's top hits and songs from similar artists */ return new(); }
    public PlaylistModel? IdentifyMyMostPlayedPlaylist()
    {
        return _realm.All<PlaylistModel>().ToList()
            .Select(p => new { Playlist = p, PlayCount = _realm.All<DimmerPlayEvent>().Filter("ANY SongsLinkingToThisEvent.Playlists.Id == $0", p.Id).Count() })
            .OrderByDescending(x => x.PlayCount)
            .FirstOrDefault()?.Playlist;
    }
    public double GetPlaylistCohesionScore(ObjectId playlistId) { /* Calculate how many songs share the same top 3 genres/tags */ return 0.0; }
    public List<SongRecommendation> SuggestSongsToBridgeTwoPlaylists(ObjectId p1Id, ObjectId p2Id) { /* Find songs that fit the profile of both playlists */ return new(); }
    public List<SongRecommendation> CompleteAnAlbumOnAPlaylist(ObjectId playlistId) { /* Find playlists with 2+ songs from an album and suggest adding the rest */ return new(); }
    public string GetPlaylistDominantMood(ObjectId playlistId) { /* Find the most common "mood" tag across all songs in a playlist */ return ""; }
    public List<SongRecommendation> SuggestSongsFromFavoriteArtistsNotInPlaylist(ObjectId playlistId) { /* Find user's top artists and suggest any of their songs not already on the playlist */ return new(); }
    public List<SongRecommendation> SuggestThrowbackSongsForPlaylist(ObjectId playlistId) { /* Find songs with similar mood but released 10+ years ago */ return new(); }
    public List<PlaylistModel> FindRedundantPlaylists() { /* Find pairs of playlists with >80% song overlap */ return new(); }
    public (PlaylistModel? Playlist, double Score) FindMyMostDiversePlaylist() { /* Playlist with the highest number of unique artists/genres */ return (null, 0.0); }
    public List<SongRecommendation> SuggestSongsBasedOnTimeOfDay(string timeOfDay) { /* Find songs user most plays during a specific time (e.g., "Morning") and suggest similar ones */ return new(); }
    public List<SongRecommendation> SuggestSongsBasedOnNextInQueue(ObjectId currentlyPlayingSongId) { /* Analyze play history to see what song user most often plays after the current one */ return new(); }
    public List<SongRecommendation> FillPlaylistToDuration(ObjectId playlistId, double targetMinutes) { /* Add songs that fit the playlist's mood until it reaches a target duration */ return new(); }
    public List<SongRecommendation> SuggestSongsFromSimilarUsers() { /* Conceptual: If you have user profiles, find users with similar top artists and suggest songs they love that you haven't heard */ return new(); }
    public List<SongRecommendation> RefreshStalePlaylist(ObjectId playlistId) { /* Identify least-played songs and suggest fresh, similar-sounding tracks */ return new(); }
    public List<SongRecommendation> CreateSurpriseMePlaylist() { /* A mix of favorites, forgotten gems, and new discoveries based on taste profile */ return new(); }

    #endregion
}
