using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Realms;

namespace Dimmer_MAUI.DataAccess.Services;

public class PlayListManagementService : IPlaylistManagementService
{
    Realm _db;
    private IDisposable _playlistNotificationToken;
    public IDataBaseService DataBaseService { get; }
    public ISongsManagementService SongsManagementService { get; }
    public ObservableCollection<PlaylistModelView> AllPlaylists { get; set; } = new();
    public IList<PlaylistSongLink> PlaylistSongLink { get; set; } 
    public PlayListManagementService(IDataBaseService dataBaseService, ISongsManagementService songsManagementService)
    {
        DataBaseService = dataBaseService;
        SongsManagementService=songsManagementService;
        //LoadPlaylists();
        SubscribeToPlaylistChanges();
    }

    private void SubscribeToPlaylistChanges()
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());
        IQueryable<PlaylistModel> playlists = _db.All<PlaylistModel>();
        _playlistNotificationToken = playlists.SubscribeForNotifications((sender, changes) =>
        {
            // Update AllPlaylists based on the current Realm collection.
            AllPlaylists.Clear();
            foreach (PlaylistModel? playlist in sender)
            {
                AllPlaylists.Add(new PlaylistModelView(playlist));
            }
        });
    }
    // Optimized method: query without materializing the full list repeatedly.
    public List<string?> GetSongIdsForPlaylist(string playlistID)
    {
        return _db.All<PlaylistSongLink>()
                  .Where(link => link.PlaylistId == playlistID)
                  .Select(link => link.SongId)
                  .ToList();
    }
    private void LoadPlaylists()
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());
        AllPlaylists ??= new ObservableCollection<PlaylistModelView>();

        List<PlaylistModel> realmPlayLists = _db.All<PlaylistModel>().ToList();
        AllPlaylists.Clear();
        foreach (PlaylistModel? playlist in realmPlayLists)
            AllPlaylists.Add(new PlaylistModelView(playlist));
    }

    public ObservableCollection<PlaylistModelView> GetPlaylists()
    {        
        return AllPlaylists;
    }

    // Improved AddSongsToPlaylist with filtered queries for existence.
    public bool AddSongsToPlaylist(PlaylistModelView playlist, List<string> songIDs)
    {
        try
        {
            PlaylistModel? existingPlaylist = _db.Find<PlaylistModel>(playlist.LocalDeviceId);

            _db.Write(() =>
            {
                if (existingPlaylist == null)
                {
                    PlaylistModel newPlaylist = new PlaylistModel(playlist);
                    _db.Add(newPlaylist);

                    foreach (string songID in songIDs)
                    {
                        if (!_db.All<PlaylistSongLink>().Any(link => link.PlaylistId == newPlaylist.LocalDeviceId && link.SongId == songID))
                        {
                            PlaylistSongLink link = new PlaylistSongLink
                            {
                                LocalDeviceId = Guid.NewGuid().ToString(),
                                PlaylistId = newPlaylist.LocalDeviceId,
                                SongId = songID
                            };
                            _db.Add(link);
                        }
                    }
                }
                else
                {
                    foreach (string songID in songIDs)
                    {
                        if (!_db.All<PlaylistSongLink>().Any(link => link.PlaylistId == playlist.LocalDeviceId && link.SongId == songID))
                        {
                            PlaylistSongLink link = new PlaylistSongLink
                            {
                                LocalDeviceId = Guid.NewGuid().ToString(),
                                PlaylistId = playlist.LocalDeviceId,
                                SongId = songID
                            };
                            _db.Add(link);
                        }
                    }
                }
                UpdatePlaylistMetadata(playlist.LocalDeviceId);
            });

            Debug.WriteLine("Added songs to playlist.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error adding songs to playlist: " + ex.Message);
            return false;
        }
    }


    public void Dispose()
    {
        _playlistNotificationToken?.Dispose();
        _db?.Dispose();
    }

    public bool RemoveSongsFromPlaylist(string playlistID, List<string> songIDs)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            PlaylistModel? existingPlaylist = _db.Find<PlaylistModel>(playlistID);
            if (existingPlaylist is null)
                return false;

            _db.Write(() =>
            {
                foreach (string songID in songIDs)
                {
                    List<PlaylistSongLink> links = _db.All<PlaylistSongLink>().ToList()
                                .Where(link => link.PlaylistId == playlistID && link.SongId == songID)
                                .ToList();
                    foreach (PlaylistSongLink? lnk in links)
                    {
                        _db.Remove(lnk);
                    }
                }
                UpdatePlaylistMetadata(playlistID);
            });

            Debug.WriteLine("Removed songs from playlist.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing songs from playlist: {ex.Message}");
            return false;
        }
    }

    private void UpdatePlaylistMetadata(string playlistID)
    {
        PlaylistModel? playlist = _db.Find<PlaylistModel>(playlistID);
        if (playlist == null)
            return;

        List<PlaylistSongLink> songLinks = _db.All<PlaylistSongLink>().ToList()
                        .Where(link => link.PlaylistId == playlistID)
                        .ToList();
        List<string?> songIds = songLinks.Select(link => link.SongId).ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> songs = allSongs.Where(song => songIds.Contains(song.LocalDeviceId)).ToList();

        double totalDuration = songs.Sum(x => x.DurationInSeconds);

            playlist.TotalSongsCount = songLinks.Count;
            playlist.TotalDuration = totalDuration;

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
                    if (string.IsNullOrEmpty(playlistSongLink.LocalDeviceId))
                        playlistSongLink.LocalDeviceId = Guid.NewGuid().ToString();
                    _db.Add(playlistSongLink);
                    UpdatePlaylistMetadata(playlist.LocalDeviceId);
                });
            }

            if (IsRemoveSong && playlistSongLink is not null)
            {
                PlaylistSongLink? existingLink = _db.Find<PlaylistSongLink>(playlistSongLink.LocalDeviceId);
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
                PlaylistModel? existingPlaylist = _db.Find<PlaylistModel>(playlist.LocalDeviceId);
                if (existingPlaylist is null)
                    return false;

                _db.Write(() =>
                {
                    List<PlaylistSongLink> relatedLinks = _db.All<PlaylistSongLink>().ToList()
                                        .Where(link => link.PlaylistId == playlist.LocalDeviceId)
                                        .ToList();
                    foreach (PlaylistSongLink? link in relatedLinks)
                        _db.Remove(link);
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
            PlaylistModel? existingPlaylist = _db.Find<PlaylistModel>(playlistID);
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
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            PlaylistModel? existingPlaylist = _db.Find<PlaylistModel>(playlistID);
            if (existingPlaylist == null)
                return false;

            _db.Write(() =>
            {
                List<PlaylistSongLink> relatedLinks = _db.All<PlaylistSongLink>().ToList()
                    .Where(link => link.PlaylistId == playlistID)
                    .ToList();
                foreach (PlaylistSongLink? link in relatedLinks)
                    _db.Remove(link);
                _db.Remove(existingPlaylist);
            });
            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting playlist: {ex.Message}");
            return false;
        }
    }

    // This method materializes play data and then filters in memory.
    private List<PlayDateAndCompletionStateSongLink> GetPlayDataForPlaylist(string playlistID)
    {
        if (_db == null)
            return new List<PlayDateAndCompletionStateSongLink>();
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        if (songIds == null || songIds.Count == 0)
            return new List<PlayDateAndCompletionStateSongLink>();
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        // Filtering by SongId as per original intent.
        List<PlayDateAndCompletionStateSongLink> playData = allPlayData.Where(p => songIds.Contains(p.SongId)).ToList();
        return playData;
    }

    public int GetTotalCompletedPlays(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> playData = GetPlayDataForPlaylist(playlistID);
        return playData.Count(p => p.WasPlayCompleted);
    }

    public double GetAveragePlayDuration(string playlistID)
    {
        List<double> playData = GetPlayDataForPlaylist(playlistID)
                        .Where(p => p.WasPlayCompleted)
                        .Select(p => (p.DateFinished - p.EventDate)?.TotalSeconds ?? 0)
                        .ToList();
        return playData.Any() ? playData.Average() : 0;
    }

    public SongModelView? GetMostPlayedSong(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var grouped = filtered.GroupBy(p => p.LocalDeviceId)
                              .Select(g => new { SongId = g.Key, Count = g.Count() })
                              .OrderByDescending(x => x.Count)
                              .FirstOrDefault();
        if (grouped == null)
            return null;
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        SongModel? song = allSongs.FirstOrDefault(s => s.LocalDeviceId == grouped.SongId);
        return song != null ? new SongModelView(song) : null;
    }

    public SongModelView? GetLeastPlayedSong(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var playCounts = songIds
            .Select(id => new { SongId = id, Count = filtered.Count(p => p.LocalDeviceId == id) })
            .OrderBy(x => x.Count)
            .FirstOrDefault();
        if (playCounts == null)
            return null;
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        SongModel? songFound = allSongs.FirstOrDefault(s => s.LocalDeviceId == playCounts.SongId);
        return songFound != null ? new SongModelView(songFound) : null;
    }

    public (string ArtistName, int PlayCount)? GetTopArtist(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> joined = filtered.Join(allSongs,
                    play => play.LocalDeviceId,
                    song => song.LocalDeviceId,
                    (play, song) => song)
                    .Where(s => !string.IsNullOrEmpty(s.ArtistName))
                    .ToList();
        var group = joined.GroupBy(s => s.ArtistName)
                          .Select(g => new { ArtistName = g.Key, TotalPlays = g.Count() })
                          .OrderByDescending(x => x.TotalPlays)
                          .FirstOrDefault();
        return group != null ? (group.ArtistName, group.TotalPlays) : null;
    }

    public (string AlbumName, int PlayCount)? GetTopAlbum(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> joined = filtered.Join(allSongs,
                    play => play.LocalDeviceId,
                    song => song.LocalDeviceId,
                    (play, song) => song)
                    .Where(s => !string.IsNullOrEmpty(s.AlbumName))
                    .ToList();
        var group = joined.GroupBy(s => s.AlbumName)
                          .Select(g => new { AlbumName = g.Key, TotalPlays = g.Count() })
                          .OrderByDescending(x => x.TotalPlays)
                          .FirstOrDefault();
        return group != null ? (group.AlbumName, group.TotalPlays) : null;
    }

    public Dictionary<string, int> GetSongsCountByGenre(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> filtered = allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.Genre)).ToList();
        return filtered.GroupBy(s => s.Genre)
                       .ToDictionary(g => g.Key!, g => g.Count());
    }

    public double GetAverageSongRating(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<int> ratings = allSongs.Where(s => songIds.Contains(s.LocalDeviceId))
                              .Select(s => s.Rating)
                              .ToList();
        return ratings.Any() ? ratings.Average() : 0;
    }

    public List<SongModelView> GetFavoriteSongs(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> favSongs = allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && s.IsFavorite).ToList();
        return favSongs.Select(s => new SongModelView(s)).ToList();
    }

    public int GetSongsWithLyricsCount(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        return allSongs.Count(s => songIds.Contains(s.LocalDeviceId) && s.HasLyrics);
    }

    public List<SongModelView> GetSkippedSongs(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<string?> skippedSongIds = allPlayData.Where(p => p.PlayType == 5)
                                        .Select(p => p.SongId)
                                        .Distinct()
                                        .ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<SongModel> songs = allSongs.Where(s => skippedSongIds.Contains(s.LocalDeviceId)).ToList();
        return songs.Select(s => new SongModelView(s)).ToList();
    }

    public List<PlayDateAndCompletionStateSongLink> GetPlayDataForSong(string playlistID, string songID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        return allPlayData.Where(p => p.SongId == songID)
                          .OrderBy(p => p.EventDate)
                          .ToList();
    }

    public Dictionary<int, int> GetPlayTypeCounts(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> playData = GetPlayDataForPlaylist(playlistID);
        return playData.GroupBy(p => p.PlayType)
                       .ToDictionary(g => g.Key, g => g.Count());
    }

    public int GetMostActiveHour(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> playData = GetPlayDataForPlaylist(playlistID);
        var hourGroup = playData.GroupBy(p => p.EventDate?.Hour ?? 0)
                                .Select(g => new { Hour = g.Key, Count = g.Count() })
                                .OrderByDescending(x => x.Count)
                                .FirstOrDefault();
        return hourGroup != null ? hourGroup.Hour : -1;
    }

    public int GetDistinctArtistsCount(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        return allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.ArtistName))
                       .Select(s => s.ArtistName)
                       .Distinct()
                       .Count();
    }

    public double GetTotalPlaytimeFromEvents(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> playData = GetPlayDataForPlaylist(playlistID);
        return playData.Where(p => p.WasPlayCompleted)
                       .Select(p => (p.DateFinished - p.EventDate)?.TotalSeconds ?? 0)
                       .Sum();
    }

    public List<(string SongId, int PlayCount)> GetTotalPlaysPerSong(string playlistID)
    {
        List<string?> songIds = GetSongIdsForPlaylist(playlistID);
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        List<(string? SongId, int PlayCount)> result = filtered.GroupBy(p => p.LocalDeviceId)
                     .Select(g => (SongId: g.Key, PlayCount: g.Count()))
                     .ToList();
        return result;
    }

    public List<(string AlbumName, int PlayCount)> GetAlbumPlayCounts(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        // Assuming LocalDeviceId stores the playlist id in play data.
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID).ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        var joined = filtered.Join(allSongs,
                    play => play.SongId,
                    song => song.LocalDeviceId,
                    (play, song) => new { AlbumName = song.AlbumName, SongId = play.SongId })
                    .Where(x => !string.IsNullOrEmpty(x.AlbumName))
                    .ToList();
        var albumPlayCounts = joined.GroupBy(x => x.AlbumName)
                    .Select(g => new { AlbumName = g.Key, TotalPlays = g.Count() })
                    .OrderByDescending(x => x.TotalPlays)
                    .ToList();
        return albumPlayCounts.Select(x => (x.AlbumName, x.TotalPlays)).ToList();
    }

    public List<(string AlbumName, int PlayCount)> GetLeastPlayedAlbums(string playlistID, int take = 10)
    {
        return GetAlbumPlayCounts(playlistID).OrderBy(x => x.PlayCount).Take(take).ToList();
    }

    public List<(string ArtistName, int PlayCount)> GetLeastPlayedArtists(string playlistID, int take = 10)
    {
        return GetArtistPlayCounts(playlistID).OrderBy(x => x.PlayCount).Take(take).ToList();
    }

    public List<(DayOfWeek DayOfWeek, int PlayCount)> GetPlaysPerDayOfWeek(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        return playData.GroupBy(p => p.DateStarted.DayOfWeek)
                       .Select(g => (DayOfWeek: g.Key, PlayCount: g.Count()))
                       .OrderBy(x => x.DayOfWeek)
                       .ToList();
    }

    public List<(int Hour, int PlayCount)> GetPlaysPerHourOfDay(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        return playData.GroupBy(p => p.DateStarted.Hour)
                       .Select(g => (Hour: g.Key, PlayCount: g.Count()))
                       .OrderBy(x => x.Hour)
                       .ToList();
    }

    public double GetAveragePlaysPerDay(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        int days = playData.Select(p => p.DateStarted.Date).Distinct().Count();
        return (double)playData.Count / days;
    }

    public int GetTotalPlayCount(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID).Count;
    }

    public int GetUniqueSongsPlayedCount(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID)
                .Select(p => p.SongId)
                .Distinct()
                .Count();
    }

    public List<(string SongId, string SongTitle, int PlayCount)> GetSongPlayCounts(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        var grouped = playData.GroupBy(p => p.SongId)
                      .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
                      .ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<(string? SongId, string SongTitle, int PlayCount)> result = grouped.Join(allSongs,
                        playCount => playCount.SongId,
                        song => song.LocalDeviceId,
                        (playCount, song) => (playCount.SongId, SongTitle: song.Title, playCount.PlayCount))
                        .OrderByDescending(x => x.PlayCount)
                        .ToList();
        return result;
    }

    public double GetAverageSongCompletionRate(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        return playData.Count(p => p.WasPlayCompleted) / (double)playData.Count;
    }

    public int GetMostCommonPlayType(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        return playData.GroupBy(x => x.PlayType)
                       .OrderByDescending(x => x.Count())
                       .FirstOrDefault()?.Key ?? 0;
    }

    public List<(string DeviceName, int PlayCount)> GetDevicePlayCounts(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceName))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.DeviceName)
                       .Select(g => new { DeviceName = g.Key, PlayCount = g.Count() })
                       .OrderByDescending(x => x.PlayCount)
                       .ToList();
        return grouped.Select(x => (x.DeviceName, x.PlayCount)).ToList();
    }

    public List<(string DeviceFormFactor, int PlayCount)> GetDeviceFormFactorPlayCounts(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceFormFactor))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.DeviceFormFactor)
                       .Select(g => new { DeviceFormFactor = g.Key, PlayCount = g.Count() })
                       .OrderByDescending(x => x.PlayCount)
                       .ToList();
        return grouped.Select(x => (x.DeviceFormFactor, x.PlayCount)).ToList();
    }

    public List<(string SongId, string SongTitle, int RestartCount)> GetRestartedSongs(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && (p.PlayType == 6 || p.PlayType == 7))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.SongId)
                     .Select(g => new { SongId = g.Key, RestartCount = g.Count() })
                     .ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<(string? SongId, string Title, int RestartCount)> result = grouped.Join(allSongs,
                    grp => grp.SongId,
                    song => song.LocalDeviceId,
                    (grp, song) => (grp.SongId, song.Title, grp.RestartCount))
                    .OrderByDescending(x => x.RestartCount)
                    .ToList();
        return result;
    }

    public List<(string ArtistName, int PlayCount)> GetArtistPlayCounts(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> playData = allPlayData.Where(p => p.LocalDeviceId == playlistID).ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        var joined = playData.Join(allSongs,
                    play => play.SongId,
                    song => song.LocalDeviceId,
                    (play, song) => new { ArtistName = song.ArtistName, SongId = play.SongId })
                    .Where(x => !string.IsNullOrEmpty(x.ArtistName))
                    .ToList();
        var grouped = joined.GroupBy(x => x.ArtistName)
                    .Select(g => new { ArtistName = g.Key, TotalPlays = g.Count() })
                    .OrderByDescending(x => x.TotalPlays)
                    .ToList();
        return grouped.Select(x => (x.ArtistName, x.TotalPlays)).ToList();
    }

    public List<(string SongId, string SongTitle, int CompleteCount)> GetCompletedSongs(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> playData = allPlayData.Where(p => p.LocalDeviceId == playlistID && p.WasPlayCompleted).ToList();
        var grouped = playData.GroupBy(p => p.SongId)
                      .Select(g => new { SongId = g.Key, CompleteCount = g.Count() })
                      .ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<(string? SongId, string Title, int CompleteCount)> result = grouped.Join(allSongs,
                        grp => grp.SongId,
                        song => song.LocalDeviceId,
                        (grp, song) => (grp.SongId, song.Title, grp.CompleteCount))
                        .OrderByDescending(x => x.CompleteCount)
                        .ToList();
        return result;
    }

    public double GetAverageSkipPosition(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDateAndCompletionStateSongLink> skippedPlays = allPlayData.Where(p => p.LocalDeviceId == playlistID && p.PlayType == 5).ToList();
        return skippedPlays.Any() ? skippedPlays.Average(p => p.PositionInSeconds) : 0;
    }

    public List<(string SongId, string SongTitle)> GetSongsPlayedOnDate(string playlistID, DateTime date)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID)
                         .Where(p => p.DateStarted.Date == date.Date)
                         .ToList();
        List<string?> distinctSongIds = playData.Select(x => x.SongId).Distinct().ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<(string? songId, string Title)> result = distinctSongIds.Join(allSongs,
                        songId => songId,
                        song => song.LocalDeviceId,
                        (songId, song) => (songId, song.Title))
                        .ToList();
        return result;
    }

    public int GetNumberOfDaysWithPlays(string playlistID)
    {
        return GetPlayDataLinksForPlaylist(playlistID)
                 .Select(p => p.DateStarted.Date)
                 .Distinct()
                 .Count();
    }

    public TimeSpan GetLongestPlaySession(string playlistID)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID).OrderBy(p => p.DateStarted).ToList();
        if (!playData.Any())
            return TimeSpan.Zero;

        TimeSpan maxDuration = TimeSpan.Zero;
        DateTime currentSessionStart = playData.FirstOrDefault().DateStarted;
        DateTime currentSessionEnd = playData.FirstOrDefault().DateStarted;

        for (int i = 1; i < playData.Count; i++)
        {
            if ((playData[i].DateStarted - currentSessionEnd) <= TimeSpan.FromMinutes(5))
            {
                currentSessionEnd = playData[i].DateStarted;
            }
            else
            {
                TimeSpan duration = currentSessionEnd - currentSessionStart;
                if (duration > maxDuration)
                    maxDuration = duration;
                currentSessionStart = playData[i].DateStarted;
                currentSessionEnd = playData[i].DateStarted;
            }
        }

        TimeSpan finalDuration = currentSessionEnd - currentSessionStart;
        if (finalDuration > maxDuration)
            maxDuration = finalDuration;

        return maxDuration;
    }

    public List<(string SongId, string SongTitle, DateTime DateStarted)> GetRecentlyPlayedSongs(string playlistID, int count = 10)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistID)
                        .OrderByDescending(p => p.DateStarted)
                        .Take(count)
                        .ToList();
        List<SongModel> allSongs = _db.All<SongModel>().ToList();
        List<(string? SongId, string Title, DateTime DateStarted)> result = playData.Join(allSongs,
                        p => p.SongId,
                        song => song.LocalDeviceId,
                        (p, song) => (p.SongId, song.Title, p.DateStarted))
                        .ToList();
        return result;
    }

    public (double CompletedPercentage, double NotCompletedPercentage) GetPercentageOfPlaysCompleted(string playlistId)
    {
        List<PlayDateAndCompletionStateSongLink> playData = _db.All<PlayDateAndCompletionStateSongLink>().ToList()
                        .Where(p => p.LocalDeviceId == playlistId)
                        .ToList();
        if (!playData.Any())
            return (0, 0);
        double completed = playData.Count(p => p.WasPlayCompleted);
        return (completed / playData.Count * 100, (playData.Count - completed) / (double)playData.Count * 100);
    }

    public List<(int Month, int Year, int PlayCount)> GetPlaysPerMonth(string playlistId)
    {
        List<PlayDataLink> playData = GetPlayDataLinksForPlaylist(playlistId);
        return playData.GroupBy(p => new { p.DateStarted.Month, p.DateStarted.Year })
                       .Select(g => (g.Key.Month, g.Key.Year, PlayCount: g.Count()))
                       .OrderBy(x => x.Year).ThenBy(x => x.Month)
                       .ToList();
    }

    private List<PlayDataLink> GetPlayDataLinksForPlaylist(string playlistID)
    {
        List<PlayDateAndCompletionStateSongLink> allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        List<PlayDataLink> list = allPlayData.Where(p => p.LocalDeviceId == playlistID)
                              .Select(p => new PlayDataLink(p) { LocalDeviceId = p.LocalDeviceId })
                              .ToList();
        return list;
    }
}
