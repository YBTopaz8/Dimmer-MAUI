using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
        AllPlaylists ??= new ObservableCollection<PlaylistModelView>();

        var realmPlayLists = _db.All<PlaylistModel>().ToList();
        AllPlaylists.Clear();
        foreach (var playlist in realmPlayLists)
            AllPlaylists.Add(new PlaylistModelView(playlist));
    }

    public ObservableCollection<PlaylistModelView> GetPlaylists()
    {
        LoadPlaylists();
        return new ObservableCollection<PlaylistModelView>(AllPlaylists);
    }

    public List<string?> GetSongIdsForPlaylist(string? playlistID)
    {
        _db = Realm.GetInstance(DataBaseService.GetRealm());
        var links = _db.All<PlaylistSongLink>().ToList()
                    .Where(link => link.PlaylistId == playlistID)
                    .ToList();
        return links.Select(link => link.SongId).ToList();
    }

    public bool AddSongsToPlaylist(PlaylistModelView playlist, List<string> songIDs)
    {
        try
        {
            bool isNew;
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var existingPlaylist = _db.Find<PlaylistModel>(playlist.LocalDeviceId);
           

            _db.Write(() =>
            {
                if (existingPlaylist is null)
                {
                    var newPlaylist = new PlaylistModel(playlist);
                    isNew = true;

                    _db.Add(newPlaylist);
                    var liss = songIDs.Select(songID => new PlaylistSongLink() 
                    { 
                        SongId = songID,
                        PlaylistId = newPlaylist.LocalDeviceId,
                        LocalDeviceId = Guid.NewGuid().ToString()
                    });
                    _db.Add(liss);
                    
                }
                else
                {
                    foreach (var songID in songIDs)
                    {
                        // Materialize the links then check in memory.
                        var allLinks = _db.All<PlaylistSongLink>().ToList();
                        var exists = allLinks.Any(link => link.PlaylistId == playlist.LocalDeviceId && link.SongId == songID);
                        if (!exists)
                        {
                            _db.Add(new PlaylistSongLink
                            {
                                LocalDeviceId = Guid.NewGuid().ToString(),
                                PlaylistId = playlist.LocalDeviceId,
                                SongId = songID
                            });
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
            Debug.WriteLine($"Error adding songs to playlist: {ex.Message}");
            return false;
        }
    }

    public bool RemoveSongsFromPlaylist(string playlistID, List<string> songIDs)
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
                    var links = _db.All<PlaylistSongLink>().ToList()
                                .Where(link => link.PlaylistId == playlistID && link.SongId == songID)
                                .ToList();
                    foreach (var lnk in links)
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
        var playlist = _db.Find<PlaylistModel>(playlistID);
        if (playlist == null)
            return;

        var songLinks = _db.All<PlaylistSongLink>().ToList()
                        .Where(link => link.PlaylistId == playlistID)
                        .ToList();
        var songIds = songLinks.Select(link => link.SongId).ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var songs = allSongs.Where(song => songIds.Contains(song.LocalDeviceId)).ToList();

        var totalDuration = songs.Sum(x => x.DurationInSeconds);

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
                    var relatedLinks = _db.All<PlaylistSongLink>().ToList()
                                        .Where(link => link.PlaylistId == playlist.LocalDeviceId)
                                        .ToList();
                    foreach (var link in relatedLinks)
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
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var existingPlaylist = _db.Find<PlaylistModel>(playlistID);
            if (existingPlaylist == null)
                return false;

            _db.Write(() =>
            {
                var relatedLinks = _db.All<PlaylistSongLink>().ToList()
                    .Where(link => link.PlaylistId == playlistID)
                    .ToList();
                foreach (var link in relatedLinks)
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
        var songIds = GetSongIdsForPlaylist(playlistID);
        if (songIds == null || songIds.Count == 0)
            return new List<PlayDateAndCompletionStateSongLink>();
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        // Filtering by SongId as per original intent.
        var playData = allPlayData.Where(p => songIds.Contains(p.SongId)).ToList();
        return playData;
    }

    public int GetTotalCompletedPlays(string playlistID)
    {
        var playData = GetPlayDataForPlaylist(playlistID);
        return playData.Count(p => p.WasPlayCompleted);
    }

    public double GetAveragePlayDuration(string playlistID)
    {
        var playData = GetPlayDataForPlaylist(playlistID)
                        .Where(p => p.WasPlayCompleted)
                        .Select(p => (p.DateFinished - p.EventDate)?.TotalSeconds ?? 0)
                        .ToList();
        return playData.Any() ? playData.Average() : 0;
    }

    public SongModelView? GetMostPlayedSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var grouped = filtered.GroupBy(p => p.LocalDeviceId)
                              .Select(g => new { SongId = g.Key, Count = g.Count() })
                              .OrderByDescending(x => x.Count)
                              .FirstOrDefault();
        if (grouped == null)
            return null;
        var allSongs = _db.All<SongModel>().ToList();
        var song = allSongs.FirstOrDefault(s => s.LocalDeviceId == grouped.SongId);
        return song != null ? new SongModelView(song) : null;
    }

    public SongModelView? GetLeastPlayedSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var playCounts = songIds
            .Select(id => new { SongId = id, Count = filtered.Count(p => p.LocalDeviceId == id) })
            .OrderBy(x => x.Count)
            .FirstOrDefault();
        if (playCounts == null)
            return null;
        var allSongs = _db.All<SongModel>().ToList();
        var songFound = allSongs.FirstOrDefault(s => s.LocalDeviceId == playCounts.SongId);
        return songFound != null ? new SongModelView(songFound) : null;
    }

    public (string ArtistName, int PlayCount)? GetTopArtist(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var joined = filtered.Join(allSongs,
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
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var joined = filtered.Join(allSongs,
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
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allSongs = _db.All<SongModel>().ToList();
        var filtered = allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.Genre)).ToList();
        return filtered.GroupBy(s => s.Genre)
                       .ToDictionary(g => g.Key!, g => g.Count());
    }

    public double GetAverageSongRating(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allSongs = _db.All<SongModel>().ToList();
        var ratings = allSongs.Where(s => songIds.Contains(s.LocalDeviceId))
                              .Select(s => s.Rating)
                              .ToList();
        return ratings.Any() ? ratings.Average() : 0;
    }

    public List<SongModelView> GetFavoriteSongs(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allSongs = _db.All<SongModel>().ToList();
        var favSongs = allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && s.IsFavorite).ToList();
        return favSongs.Select(s => new SongModelView(s)).ToList();
    }

    public int GetSongsWithLyricsCount(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allSongs = _db.All<SongModel>().ToList();
        return allSongs.Count(s => songIds.Contains(s.LocalDeviceId) && s.HasLyrics);
    }

    public List<SongModelView> GetSkippedSongs(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var skippedSongIds = allPlayData.Where(p => p.PlayType == 5)
                                        .Select(p => p.SongId)
                                        .Distinct()
                                        .ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var songs = allSongs.Where(s => skippedSongIds.Contains(s.LocalDeviceId)).ToList();
        return songs.Select(s => new SongModelView(s)).ToList();
    }

    public List<PlayDateAndCompletionStateSongLink> GetPlayDataForSong(string playlistID, string songID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        return allPlayData.Where(p => p.SongId == songID)
                          .OrderBy(p => p.EventDate)
                          .ToList();
    }

    public Dictionary<int, int> GetPlayTypeCounts(string playlistID)
    {
        var playData = GetPlayDataForPlaylist(playlistID);
        return playData.GroupBy(p => p.PlayType)
                       .ToDictionary(g => g.Key, g => g.Count());
    }

    public int GetMostActiveHour(string playlistID)
    {
        var playData = GetPlayDataForPlaylist(playlistID);
        var hourGroup = playData.GroupBy(p => p.EventDate?.Hour ?? 0)
                                .Select(g => new { Hour = g.Key, Count = g.Count() })
                                .OrderByDescending(x => x.Count)
                                .FirstOrDefault();
        return hourGroup != null ? hourGroup.Hour : -1;
    }

    public int GetDistinctArtistsCount(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allSongs = _db.All<SongModel>().ToList();
        return allSongs.Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.ArtistName))
                       .Select(s => s.ArtistName)
                       .Distinct()
                       .Count();
    }

    public double GetTotalPlaytimeFromEvents(string playlistID)
    {
        var playData = GetPlayDataForPlaylist(playlistID);
        return playData.Where(p => p.WasPlayCompleted)
                       .Select(p => (p.DateFinished - p.EventDate)?.TotalSeconds ?? 0)
                       .Sum();
    }

    public List<(string SongId, int PlayCount)> GetTotalPlaysPerSong(string playlistID)
    {
        var songIds = GetSongIdsForPlaylist(playlistID);
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => songIds.Contains(p.LocalDeviceId) && p.PlayType == 3).ToList();
        var result = filtered.GroupBy(p => p.LocalDeviceId)
                     .Select(g => (SongId: g.Key, PlayCount: g.Count()))
                     .ToList();
        return result;
    }

    public List<(string AlbumName, int PlayCount)> GetAlbumPlayCounts(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        // Assuming LocalDeviceId stores the playlist id in play data.
        var filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID).ToList();
        var allSongs = _db.All<SongModel>().ToList();
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
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        return playData.GroupBy(p => p.DateStarted.DayOfWeek)
                       .Select(g => (DayOfWeek: g.Key, PlayCount: g.Count()))
                       .OrderBy(x => x.DayOfWeek)
                       .ToList();
    }

    public List<(int Hour, int PlayCount)> GetPlaysPerHourOfDay(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        return playData.GroupBy(p => p.DateStarted.Hour)
                       .Select(g => (Hour: g.Key, PlayCount: g.Count()))
                       .OrderBy(x => x.Hour)
                       .ToList();
    }

    public double GetAveragePlaysPerDay(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        var days = playData.Select(p => p.DateStarted.Date).Distinct().Count();
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
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        var grouped = playData.GroupBy(p => p.SongId)
                      .Select(g => new { SongId = g.Key, PlayCount = g.Count() })
                      .ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var result = grouped.Join(allSongs,
                        playCount => playCount.SongId,
                        song => song.LocalDeviceId,
                        (playCount, song) => (playCount.SongId, SongTitle: song.Title, playCount.PlayCount))
                        .OrderByDescending(x => x.PlayCount)
                        .ToList();
        return result;
    }

    public double GetAverageSongCompletionRate(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        return playData.Count(p => p.WasPlayCompleted) / (double)playData.Count;
    }

    public int GetMostCommonPlayType(string playlistID)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID);
        if (!playData.Any())
            return 0;
        return playData.GroupBy(x => x.PlayType)
                       .OrderByDescending(x => x.Count())
                       .FirstOrDefault()?.Key ?? 0;
    }

    public List<(string DeviceName, int PlayCount)> GetDevicePlayCounts(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceName))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.DeviceName)
                       .Select(g => new { DeviceName = g.Key, PlayCount = g.Count() })
                       .OrderByDescending(x => x.PlayCount)
                       .ToList();
        return grouped.Select(x => (x.DeviceName, x.PlayCount)).ToList();
    }

    public List<(string DeviceFormFactor, int PlayCount)> GetDeviceFormFactorPlayCounts(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && !string.IsNullOrEmpty(p.DeviceFormFactor))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.DeviceFormFactor)
                       .Select(g => new { DeviceFormFactor = g.Key, PlayCount = g.Count() })
                       .OrderByDescending(x => x.PlayCount)
                       .ToList();
        return grouped.Select(x => (x.DeviceFormFactor, x.PlayCount)).ToList();
    }

    public List<(string SongId, string SongTitle, int RestartCount)> GetRestartedSongs(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var filtered = allPlayData.Where(p => p.LocalDeviceId == playlistID && (p.PlayType == 6 || p.PlayType == 7))
                                  .ToList();
        var grouped = filtered.GroupBy(p => p.SongId)
                     .Select(g => new { SongId = g.Key, RestartCount = g.Count() })
                     .ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var result = grouped.Join(allSongs,
                    grp => grp.SongId,
                    song => song.LocalDeviceId,
                    (grp, song) => (grp.SongId, song.Title, grp.RestartCount))
                    .OrderByDescending(x => x.RestartCount)
                    .ToList();
        return result;
    }

    public List<(string ArtistName, int PlayCount)> GetArtistPlayCounts(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var playData = allPlayData.Where(p => p.LocalDeviceId == playlistID).ToList();
        var allSongs = _db.All<SongModel>().ToList();
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
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var playData = allPlayData.Where(p => p.LocalDeviceId == playlistID && p.WasPlayCompleted).ToList();
        var grouped = playData.GroupBy(p => p.SongId)
                      .Select(g => new { SongId = g.Key, CompleteCount = g.Count() })
                      .ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var result = grouped.Join(allSongs,
                        grp => grp.SongId,
                        song => song.LocalDeviceId,
                        (grp, song) => (grp.SongId, song.Title, grp.CompleteCount))
                        .OrderByDescending(x => x.CompleteCount)
                        .ToList();
        return result;
    }

    public double GetAverageSkipPosition(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var skippedPlays = allPlayData.Where(p => p.LocalDeviceId == playlistID && p.PlayType == 5).ToList();
        return skippedPlays.Any() ? skippedPlays.Average(p => p.PositionInSeconds) : 0;
    }

    public List<(string SongId, string SongTitle)> GetSongsPlayedOnDate(string playlistID, DateTime date)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID)
                         .Where(p => p.DateStarted.Date == date.Date)
                         .ToList();
        var distinctSongIds = playData.Select(x => x.SongId).Distinct().ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var result = distinctSongIds.Join(allSongs,
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
        var playData = GetPlayDataLinksForPlaylist(playlistID).OrderBy(p => p.DateStarted).ToList();
        if (!playData.Any())
            return TimeSpan.Zero;

        var maxDuration = TimeSpan.Zero;
        var currentSessionStart = playData.FirstOrDefault().DateStarted;
        var currentSessionEnd = playData.FirstOrDefault().DateStarted;

        for (int i = 1; i < playData.Count; i++)
        {
            if ((playData[i].DateStarted - currentSessionEnd) <= TimeSpan.FromMinutes(5))
            {
                currentSessionEnd = playData[i].DateStarted;
            }
            else
            {
                var duration = currentSessionEnd - currentSessionStart;
                if (duration > maxDuration)
                    maxDuration = duration;
                currentSessionStart = playData[i].DateStarted;
                currentSessionEnd = playData[i].DateStarted;
            }
        }

        var finalDuration = currentSessionEnd - currentSessionStart;
        if (finalDuration > maxDuration)
            maxDuration = finalDuration;

        return maxDuration;
    }

    public List<(string SongId, string SongTitle, DateTime DateStarted)> GetRecentlyPlayedSongs(string playlistID, int count = 10)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistID)
                        .OrderByDescending(p => p.DateStarted)
                        .Take(count)
                        .ToList();
        var allSongs = _db.All<SongModel>().ToList();
        var result = playData.Join(allSongs,
                        p => p.SongId,
                        song => song.LocalDeviceId,
                        (p, song) => (p.SongId, song.Title, p.DateStarted))
                        .ToList();
        return result;
    }

    public (double CompletedPercentage, double NotCompletedPercentage) GetPercentageOfPlaysCompleted(string playlistId)
    {
        var playData = _db.All<PlayDateAndCompletionStateSongLink>().ToList()
                        .Where(p => p.LocalDeviceId == playlistId)
                        .ToList();
        if (!playData.Any())
            return (0, 0);
        double completed = playData.Count(p => p.WasPlayCompleted);
        return (completed / playData.Count * 100, (playData.Count - completed) / (double)playData.Count * 100);
    }

    public List<(int Month, int Year, int PlayCount)> GetPlaysPerMonth(string playlistId)
    {
        var playData = GetPlayDataLinksForPlaylist(playlistId);
        return playData.GroupBy(p => new { p.DateStarted.Month, p.DateStarted.Year })
                       .Select(g => (g.Key.Month, g.Key.Year, PlayCount: g.Count()))
                       .OrderBy(x => x.Year).ThenBy(x => x.Month)
                       .ToList();
    }

    private List<PlayDataLink> GetPlayDataLinksForPlaylist(string playlistID)
    {
        var allPlayData = _db.All<PlayDateAndCompletionStateSongLink>().ToList();
        var list = allPlayData.Where(p => p.LocalDeviceId == playlistID)
                              .Select(p => new PlayDataLink(p) { LocalDeviceId = p.LocalDeviceId })
                              .ToList();
        return list;
    }
}
