using System.Diagnostics;
using Realms;

namespace Dimmer_MAUI.DataAccess.Services;

public class PlayListManagementService : IPlaylistManagementService
{
    Realm? _db;

    public IDataBaseService DataBaseService { get; }
    public ObservableCollection<PlaylistModelView> AllPlaylists { get; set; } = new();

    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        LoadPlaylists();
    }

    private void LoadPlaylists()
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());
        AllPlaylists ??= new();

        var realmPlayLists = _db.All<PlaylistModel>().ToList();
        AllPlaylists.Clear();

        foreach (var playlist in realmPlayLists)
            AllPlaylists.Add(new PlaylistModelView(playlist));
    }

    public ObservableCollection<PlaylistModelView> GetPlaylists()
    {
        LoadPlaylists();
        return new(AllPlaylists);
    }

    public List<string?> GetSongIdsForPlaylist(string? playlistID)
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());

        // Materialize the query results first
        var links = _db.All<PlaylistSongLink>()
                       .Where(link => link.PlaylistId == playlistID)
                       .ToList();

        // Then use LINQ Select on the in-memory list
        return links.Select(link => link.SongId).ToList();
    }


    public bool AddSongsToPlaylist(string playlistID, List<string> songIDs)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());

            var existingPlaylist = _db.Find<PlaylistModel>(playlistID);
            if (existingPlaylist is null)
                return false;

            _db.Write(() =>
            {
                foreach (var songID in songIDs)
                {
                    var linkExists = _db.All<PlaylistSongLink>()
                        .Any(link => link.PlaylistId == playlistID && link.SongId == songID);

                    if (!linkExists)
                    {
                        _db.Add(new PlaylistSongLink
                        {
                            LocalDeviceId = GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistSongLink)),
                            PlaylistId = playlistID,
                            SongId = songID
                        });
                    }
                }

                UpdatePlaylistMetadata(playlistID);

                var pl = AllPlaylists.FirstOrDefault(x => x.LocalDeviceId==playlistID);
               
            });

            Debug.WriteLine("Added songs to playlist.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding songs to playlist: {ex.Message}");
            return false;
        }
    }

    private void UpdatePlaylistMetadata(string playlistID)
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());

        var playlist = _db.Find<PlaylistModel>(playlistID);
        if (playlist is null)
            return;

        var songLinks = _db.All<PlaylistSongLink>()
                           .Where(link => link.PlaylistId == playlistID)
                           .ToList();

        var totalDuration = _db.All<SongModel>()
                               .Where(song => songLinks.Any(link => link.SongId == song.LocalDeviceId))
                               .Sum(song => song.DurationInSeconds);

        _db.Write(() =>
        {
            playlist.TotalSongsCount = songLinks.Count;
            playlist.TotalDuration = totalDuration;
        });

    }

    public bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink = null,
                               bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());

            if (IsAddSong && playlistSongLink is not null)
            {
                _db.Write(() =>
                {
                    _db.Add(new PlaylistModel(playlist), true);
                    playlistSongLink.LocalDeviceId ??= GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistSongLink));
                    _db.Add(playlistSongLink);
                    UpdatePlaylistMetadata(playlist.LocalDeviceId);
                });
            }

            if (IsRemoveSong && playlistSongLink is not null)
            {
                var existingLink = _db.Find<PlaylistSongLink>(playlistSongLink.LocalDeviceId);
                if (existingLink is not null)
                {
                    _db.Write(() =>
                    {
                        _db.Remove(existingLink);
                        UpdatePlaylistMetadata(playlist.LocalDeviceId);
                    });
                }
            }

            if (IsDeletePlaylist)
            {
                var existingPlaylist = _db.Find<PlaylistModel>(playlist.LocalDeviceId);

                if (existingPlaylist is null)
                    return false;

                _db.Write(() =>
                {
                    var relatedLinks = _db.All<PlaylistSongLink>()
                                         .Where(link => link.PlaylistId == playlist.LocalDeviceId);

                    _db.RemoveRange(relatedLinks);
                    _db.Remove(existingPlaylist);
                });
            }

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating playlist: {ex.Message}");
            return false;
        }
    }

    public bool RenamePlaylist(string playlistID, string newPlaylistName)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var existingPlaylist = _db.Find<PlaylistModel>(playlistID);

            if (existingPlaylist is null)
                return false;

            _db.Write(() =>
            {
                existingPlaylist.Name = newPlaylistName;
            });

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error renaming playlist: {ex.Message}");
            return false;
        }
    }

 

    public bool DeletePlaylist(string playlistID)
    {
        throw new NotImplementedException();
    }

    // Helper: Get play data for songs in a playlist
    private IQueryable<PlayDateAndCompletionStateSongLink> GetPlayDataForPlaylist(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        if (_db is null)
        {
            return null;
        }
        return _db.All<PlayDateAndCompletionStateSongLink>()
                  .Where(p => songIds.Contains(p.SongId));
    }

    // 1. Total play count for all songs in a playlist
    //public int GetTotalPlayCount(string playlistID)
    //{
    //    var songIds = GetSongIdsForPlaylist(playlistID);
    //    return _db.All<SongModel>()
    //              .Where(s => songIds.Contains(s.LocalDeviceId))
    //              .Sum(s => s.NumberOfTimesPlayed);
    //}

    // 2. Total count of completed plays (from play data events)
    public int GetTotalCompletedPlays(string playlistID)
    {
        return GetPlayDataForPlaylist(playlistID)
               .Count(p => p.WasPlayCompleted);
    }

    // 3. Average play duration (in seconds) for completed plays
    public double GetAveragePlayDuration(string playlistID)
    {
        var durations = GetPlayDataForPlaylist(playlistID)
                        .Where(p => p.WasPlayCompleted)
                        .Select(p => (p.DateFinished - p.EventDate).Value.TotalSeconds)
                        .ToList();
        return durations.Any() ? durations.Average() : 0;
    }

    //// 4. Most played song (returns top song by play count)
    public SongModelView? GetMostPlayedSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var mostPlayedSongId = _db.All<PlayDateAndCompletionStateSongLink>()
            .Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3)
            .GroupBy(p => p.LocalDeviceId)
            .Select(g => new { SongId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault()?.SongId;

        if (mostPlayedSongId == null)
            return null;

        var song = _db.All<SongModel>().FirstOrDefault(s => s.LocalDeviceId == mostPlayedSongId);
        return song != null ? new SongModelView(song) : null;
    }


    // 5. Least played song (returns song with minimum play count)
    public SongModelView? GetLeastPlayedSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);

        // Get counts for songs that have play logs (completed plays)
        var playCounts = _db.All<PlayDateAndCompletionStateSongLink>()
            .Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3)
            .GroupBy(p => p.LocalDeviceId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Include songs with 0 plays
        var leastPlayedSongId = songIds
            .Select(id => new { SongId = id, Count = playCounts.ContainsKey(id) ? playCounts[id] : 0 })
            .OrderBy(x => x.Count)
            .FirstOrDefault()?.SongId;

        if (leastPlayedSongId == null)
            return null;

        var song = _db.All<SongModel>().FirstOrDefault(s => s.LocalDeviceId == leastPlayedSongId);
        return song != null ? new SongModelView(song) : null;
    }

    // 6. Top artist (artist with highest total play count)
    public (string ArtistName, int PlayCount)? GetTopArtist(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var topArtist = _db.All<PlayDateAndCompletionStateSongLink>()
            .Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3)
            .Join(_db.All<SongModel>(),
                  play => play.LocalDeviceId,
                  song => song.LocalDeviceId,
                  (play, song) => song)
            .Where(s => !string.IsNullOrEmpty(s.ArtistName))
            .GroupBy(s => s.ArtistName)
            .Select(g => new { ArtistName = g.Key, TotalPlays = g.Count() })
            .OrderByDescending(x => x.TotalPlays)
            .FirstOrDefault();

        return topArtist != null ? (topArtist.ArtistName, topArtist.TotalPlays) : null;
    }

    // 7. Top album (album with highest total play count)
    public (string AlbumName, int PlayCount)? GetTopAlbum(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var topAlbum = _db.All<PlayDateAndCompletionStateSongLink>()
            .Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3)
            .Join(_db.All<SongModel>(),
                  play => play.LocalDeviceId,
                  song => song.LocalDeviceId,
                  (play, song) => song)
            .Where(s => !string.IsNullOrEmpty(s.AlbumName))
            .GroupBy(s => s.AlbumName)
            .Select(g => new { AlbumName = g.Key, TotalPlays = g.Count() })
            .OrderByDescending(x => x.TotalPlays)
            .FirstOrDefault();

        return topAlbum != null ? (topAlbum.AlbumName, topAlbum.TotalPlays) : null;
    }


    // 8. Count of songs by genre in a playlist
    public Dictionary<string, int> GetSongsCountByGenre(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        return _db.All<SongModel>()
                  .Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.Genre))
                  .GroupBy(s => s.Genre)
                  .ToDictionary(g => g.Key!, g => g.Count());
    }

    // 9. Average rating for songs in the playlist
    public double GetAverageSongRating(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var ratings = _db.All<SongModel>()
                         .Where(s => songIds.Contains(s.LocalDeviceId))
                         .Select(s => s.Rating);
        return ratings.Any() ? ratings.Average() : 0;
    }

    // 10. List of favorite songs (IsFavorite == true)
    public List<SongModelView> GetFavoriteSongs(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var favSongs = _db.All<SongModel>()
                          .Where(s => songIds.Contains(s.LocalDeviceId) && s.IsFavorite)
                          .ToList();
        return favSongs.Select(s => new SongModelView(s)).ToList();
    }

    // 11. Count of songs with lyrics
    public int GetSongsWithLyricsCount(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        return _db.All<SongModel>()
                  .Count(s => songIds.Contains(s.LocalDeviceId) && s.HasLyrics);
    }

    // 12. List of skipped songs (assumes PlayType 5 means "Skipped")
    public List<SongModelView> GetSkippedSongs(string playlistID)
    {
        var skippedSongIds = GetPlayDataForPlaylist(playlistID)
                             .Where(p => p.PlayType == 5)
                             .Select(p => p.SongId)
                             .Distinct()
                             .ToList();
        var songs = _db.All<SongModel>()
                       .Where(s => skippedSongIds.Contains(s.LocalDeviceId))
                       .ToList();
        return songs.Select(s => new SongModelView(s)).ToList();
    }

    // 13. Get play data history for a specific song in the playlist
    public List<PlayDateAndCompletionStateSongLink> GetPlayDataForSong(string playlistID, string songID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        if (!songIds.Contains(songID))
            return new List<PlayDateAndCompletionStateSongLink>();

        return _db.All<PlayDateAndCompletionStateSongLink>()
                  .Where(p => p.SongId == songID)
                  .OrderBy(p => p.EventDate)
                  .ToList();
    }

    // 14. Get counts for each play type (e.g., Play, Pause, Skip, etc.)
    public Dictionary<int, int> GetPlayTypeCounts(string playlistID)
    {
        return GetPlayDataForPlaylist(playlistID)
               .GroupBy(p => p.PlayType)
               .ToDictionary(g => g.Key, g => g.Count());
    }

    // 15. Most active hour (hour of day with the most play events)
    public int GetMostActiveHour(string playlistID)
    {
        var hourGroup = GetPlayDataForPlaylist(playlistID)
                        .GroupBy(p => p.EventDate.Value.Hour)
                        .Select(g => new { Hour = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .FirstOrDefault();
        return hourGroup != null ? hourGroup.Hour : -1;
    }

    // 16. Count of distinct artists in the playlist
    public int GetDistinctArtistsCount(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        return _db.All<SongModel>()
                  .Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.ArtistName))
                  .Select(s => s.ArtistName)
                  .Distinct()
                  .Count();
    }

    // 17. Total playtime (in seconds) derived from play data events
    public double GetTotalPlaytimeFromEvents(string playlistID)
    {
        return GetPlayDataForPlaylist(playlistID)
               .Where(p => p.WasPlayCompleted)
               .Select(p => (p.DateFinished - p.EventDate).Value.TotalSeconds)
               .Sum();
    }

    // 1. Total plays per song
    public List<(string SongId, int PlayCount)> GetTotalPlaysPerSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var result = _db.All<PlayDateAndCompletionStateSongLink>()
            .Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3)
            .GroupBy(p => p.LocalDeviceId)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .AsEnumerable()
            .Select(x => (x.Key, x.Count))

            .ToList();
        return result;
    }
    public List<(string AlbumName, int PlayCount)> GetAlbumPlayCounts(string playlistID)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>()
                             .Where(p => p.LocalDeviceId == playlistID);

        var albumPlayCounts = playData
            .Join(_db.All<SongModel>(),
                  play => play.SongId,
                  song => song.LocalDeviceId,
                  (play, song) => new { AlbumName = song.AlbumName, SongId = play.SongId })
            .Where(x => !string.IsNullOrEmpty(x.AlbumName))
            .GroupBy(x => x.AlbumName)
            .Select(g => new { AlbumName = g.Key, TotalPlays = g.Count() });

        return albumPlayCounts
            .OrderByDescending(x => x.TotalPlays)
            .Select(x => new { x.AlbumName, x.TotalPlays })
            .AsEnumerable()
            .Select(x=>(x.AlbumName, x.TotalPlays))
            .ToList();
    }
    public List<(string AlbumName, int PlayCount)> GetLeastPlayedAlbums(string playlistID, int take = 10)
    {
        return GetAlbumPlayCounts(playlistID).OrderBy(x => x.PlayCount).Take(take).ToList();
    }

    //30. Least played artists
    public List<(string ArtistName, int PlayCount)> GetLeastPlayedArtists(string playlistID, int take = 10)
    {
        return GetArtistPlayCounts(playlistID).OrderBy(x => x.PlayCount).Take(take).ToList();
    }
    // 8. Plays Per Day of Week (List)
    public List<(DayOfWeek DayOfWeek, int PlayCount)> GetPlaysPerDayOfWeek(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);

        return playData
            .GroupBy(p => p.DateStarted.DayOfWeek)
            .Select(g => (DayOfWeek: g.Key, PlayCount: g.Count()))
            .OrderBy(x => x.DayOfWeek)
            .ToList();
    }

    // 9. Plays Per Hour of Day (List)
    public List<(int Hour, int PlayCount)> GetPlaysPerHourOfDay(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);

        return playData
            .GroupBy(p => p.DateStarted.Hour)
            .Select(g => (Hour: g.Key, PlayCount: g.Count()))
            .OrderBy(x => x.Hour)
            .ToList();
    }

    // 10. Average Plays Per Day (Single Value)
    public double GetAveragePlaysPerDay(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;

        var days = playData.Select(p => p.DateStarted.Date).Distinct().Count();
        return (double)playData.Count / days;
    }

    // 11. Total Play Count (Single Value)
    public int GetTotalPlayCount(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID).Count;
    }

    // 12. Number of Unique Songs Played (Single Value)
    public int GetUniqueSongsPlayedCount(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID)
            .Select(p => p.SongId)
            .Distinct()
            .Count();
    }

    // 13. List of Songs and Their Play Counts (List)
    public List<(string SongId, string SongTitle, int PlayCount)> GetSongPlayCounts(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);

        return playData
            .GroupBy(p => p.SongId)
            .Select(g => (SongId: g.Key, PlayCount: g.Count()))
            .Join(_db.All<SongModel>(),
                  playCount => playCount.SongId,
                  song => song.LocalDeviceId, // Assuming LocalDeviceId is the song identifier
                  (playCount, song) => (playCount.SongId, SongTitle: song.Title, playCount.PlayCount)) //Add the title here.
            .OrderByDescending(x => x.PlayCount)
            .ToList();
    }

    // 14. Average Song Completion Rate (Single Value)
    public double GetAverageSongCompletionRate(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;

        return playData.Count(p => p.WasPlayCompleted) / (double)playData.Count;
    }

    // 15. Most Common Play Type (Single Value)
    public int GetMostCommonPlayType(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;

        return playData.GroupBy(x => x.PlayType).OrderByDescending(x => x.Count()).FirstOrDefault().Key;
    }

    // 16. List of Devices and Their Play Counts (List)
    public List<(string DeviceName, int PlayCount)> GetDevicePlayCounts(string playlistID)
    {
        return _db.All<PlayDateAndCompletionStateSongLink>()
                  .Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceName))
                  .GroupBy(p => p.DeviceName)
                  .Select(g => new { DeviceName = g.Key, PlayCount = g.Count() }) // Anonymous type
                  .OrderByDescending(x => x.PlayCount)
                  .ToList()
                  .Select(x => (x.DeviceName, x.PlayCount)) // Convert to tuple *after* ToList()
                  .ToList();
    }

    // 17. Play Counts by Device Form Factor (List)
    public List<(string DeviceFormFactor, int PlayCount)> GetDeviceFormFactorPlayCounts(string playlistID)
    {
        return _db.All<PlayDateAndCompletionStateSongLink>()
                 .Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceFormFactor))
                 .GroupBy(p => p.DeviceFormFactor)
                 .Select(g => new { DeviceFormFactor = g.Key, PlayCount = g.Count() }) // Anonymous type
                 .OrderByDescending(x => x.PlayCount)
                 .ToList()
                 .Select(x => (x.DeviceFormFactor, x.PlayCount))  // Convert to tuple *after* ToList()
                 .ToList();
    }

    // 19. Restarted Songs (List)
    public List<(string SongId, string SongTitle, int RestartCount)> GetRestartedSongs(string playlistID)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>()
                          .Where(p => p.LocalDeviceId == playlistID && (p.PlayType == 6 || p.PlayType == 7));

        return playData
           .GroupBy(p => p.SongId)
           .Select(g => new { SongId = g.Key, RestartCount = g.Count() }) // Anonymous type
           .Join(_db.All<SongModel>(),
                   restartCount => restartCount.SongId,
                   song => song.LocalDeviceId,
                   (restartCount, song) => new { restartCount.SongId, song.Title, restartCount.RestartCount }) // Anonymous type
           .OrderByDescending(x => x.RestartCount)
           .ToList()
           .Select(x => (x.SongId, x.Title, x.RestartCount)) // Convert to tuple *after* ToList()
           .ToList();
    }

    // 24. Artists and their total play counts
    public List<(string ArtistName, int PlayCount)> GetArtistPlayCounts(string playlistID)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>()
                         .Where(p => p.LocalDeviceId == playlistID);

        var artistPlayCounts = playData
            .Join(_db.All<SongModel>(),
                  play => play.SongId,
                  song => song.LocalDeviceId,
                  (play, song) => new { ArtistName = song.ArtistName, SongId = play.SongId })
            .Where(x => !string.IsNullOrEmpty(x.ArtistName))
            .GroupBy(x => x.ArtistName)
            .Select(g => new { ArtistName = g.Key, TotalPlays = g.Count() }); // Anonymous type

        return artistPlayCounts
             .OrderByDescending(x => x.TotalPlays)
             .ToList() // Materialize the query
             .Select(x => (x.ArtistName, x.TotalPlays)) // *Now* create the tuple
             .ToList();
    }

    // 27. Completion counts per songs.
    public List<(string SongId, string SongTitle, int CompleteCount)> GetCompletedSongs(string playlistID)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>()
                        .Where(p => p.LocalDeviceId == playlistID && p.WasPlayCompleted);

        return playData
           .GroupBy(p => p.SongId)
           .Select(g => new { SongId = g.Key, CompleteCount = g.Count() })  // Anonymous type
           .Join(_db.All<SongModel>(),
                   completedCount => completedCount.SongId,
                   song => song.LocalDeviceId,
                   (completedCount, song) => new { completedCount.SongId, song.Title, completedCount.CompleteCount }) // Anonymous type
           .OrderByDescending(x => x.CompleteCount)
           .ToList()
           .Select(x => (x.SongId, x.Title, x.CompleteCount))  // Convert to tuple *after* ToList()
           .ToList();
    }

    // 20. Average Position (in seconds) Where Songs Are Skipped (Single Value)
    public double GetAverageSkipPosition(string playlistID)
    {
        var skippedPlays = _db.All<PlayDateAndCompletionStateSongLink>()
                             .Where(p => p.LocalDeviceId == playlistID && p.PlayType == 5) // 5 = Skipped
                             .ToList();

        return skippedPlays.Any() ? skippedPlays.Average(p => p.PositionInSeconds) : 0;
    }

    // 21.  Songs Played on a Specific Date (List)
    public List<(string SongId, string SongTitle)> GetSongsPlayedOnDate(string playlistID, DateTime date)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID)
                         .Where(p => p.DateStarted.Date == date.Date);

        return playData.Select(x => x.SongId).Distinct() //Only get a songId once.
           .Join(_db.All<SongModel>(),
                   songId => songId,
                   song => song.LocalDeviceId,
                   (songId, song) => (songId, song.Title))
           .ToList();
    }

    // 22. Number of Days with Plays (Single Value)
    public int GetNumberOfDaysWithPlays(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID)
             .Select(p => p.DateStarted.Date)
             .Distinct()
             .Count();
    }

    // 23. Longest Play Session Duration (Single Value)
    public TimeSpan GetLongestPlaySession(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return TimeSpan.Zero;

        // Group by consecutive plays within a short time window (e.g., 5 minutes)
        // This is a simplified session definition; adjust as needed.

        var sortedPlays = playData.OrderBy(p => p.DateStarted).ToList();
        var maxDuration = TimeSpan.Zero;
        var currentSessionStart = sortedPlays.First().DateStarted;
        var currentSessionEnd = sortedPlays.First().DateStarted;


        for (int i = 1; i < sortedPlays.Count; i++)
        {
            if ((sortedPlays[i].DateStarted - currentSessionEnd) <= TimeSpan.FromMinutes(5))
            {
                currentSessionEnd = sortedPlays[i].DateStarted;  // Extend the session
            }
            else
            {
                // Calculate the duration of the previous session
                var duration = currentSessionEnd - currentSessionStart;
                maxDuration = duration > maxDuration ? duration : maxDuration;

                // Start a new session
                currentSessionStart = sortedPlays[i].DateStarted;
                currentSessionEnd = sortedPlays[i].DateStarted;
            }
        }

        // Check the last session
        var finalDuration = currentSessionEnd - currentSessionStart;
        maxDuration = finalDuration > maxDuration ? finalDuration : maxDuration;

        return maxDuration;
    }

    

    ////25. Albums and their total play counts
    //public List<(string AlbumName, int PlayCount)> GetAlbumPlayCounts(string playlistID)
    //{
    //    var playData = _db.All<PlayDateAndCompletionStateSongLink>()
    //                         .Where(p => p.LocalDeviceId == playlistID);

    //    var albumPlayCounts = playData
    //        .Join(_db.All<SongModel>(),
    //              play => play.SongId,
    //              song => song.LocalDeviceId,
    //              (play, song) => new { AlbumName = song.AlbumName, SongId = play.SongId })
    //        .Where(x => !string.IsNullOrEmpty(x.AlbumName))
    //        .GroupBy(x => x.AlbumName)
    //        .Select(g => new { AlbumName = g.Key, TotalPlays = g.Count() });

    //    return albumPlayCounts
    //        .OrderByDescending(x => x.TotalPlays).Select(x => (x.AlbumName, x.TotalPlays))
    //        .ToList();
    //}

    // 26. Recently Played Songs (List, last N songs)
    public List<(string SongId, string SongTitle, DateTime DateStarted)> GetRecentlyPlayedSongs(string playlistID, int count = 10)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID)
            .OrderByDescending(p => p.DateStarted)
            .Take(count);

        return playData
            .Join(_db.All<SongModel>(),
                playData => playData.SongId,
                song => song.LocalDeviceId,
                (playData, song) => (playData.SongId, song.Title, playData.DateStarted))
            .ToList();
    }

    

    //28. Get percentage of plays completed vs not completed.
    public (double CompletedPercentage, double NotCompletedPercentage) GetPercentageOfPlaysCompleted(string playlistId)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>().Where(p => p.LocalDeviceId == playlistId).ToList();
        if (!playData.Any())
            return (0, 0);
        double completed = playData.Count(p => p.WasPlayCompleted);
        return (completed / playData.Count * 100, (playData.Count - completed) / playData.Count * 100);
    }

    //29. Plays grouped by month
    public List<(int Month, int Year, int PlayCount)> GetPlaysPerMonth(string playlistId)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistId);
        return playData.GroupBy(p => new { p.DateStarted.Month, p.DateStarted.Year })
            .Select(x => (x.Key.Month, x.Key.Year, PlayCount: x.Count()))
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ToList();
    }



    private List<PlayDataLink> GetPlayDataLinksForPlaylist(string playlistID)
    {
        return _db.All<PlayDateAndCompletionStateSongLink>()
                 .Where(p => p.LocalDeviceId == playlistID)
                 .ToList()
                 .Select(p => new PlayDataLink(p) { LocalDeviceId = p.LocalDeviceId})
                 .ToList();
    }
}
