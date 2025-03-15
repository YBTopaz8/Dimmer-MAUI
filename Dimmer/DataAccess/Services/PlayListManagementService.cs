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
   
    public ObservableCollection<PlaylistModelView> GetPlaylists() => new(AllPlaylists);
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
    //public SongModelView? GetMostPlayedSong(string playlistID)
    //{
    //    var songIds = GetSongIdsForPlaylist(playlistID);
    //    var song = _db.All<SongModel>()
    //                  .Where(s => songIds.Contains(s.LocalDeviceId))
    //                  .OrderByDescending(s => s.NumberOfTimesPlayed)
    //                  .FirstOrDefault();
    //    return song != null ? new SongModelView(song) : null;
    //}

    //// 5. Least played song (returns song with minimum play count)
    //public SongModelView? GetLeastPlayedSong(string playlistID)
    //{
    //    var songIds = GetSongIdsForPlaylist(playlistID);
    //    var song = _db.All<SongModel>()
    //                  .Where(s => songIds.Contains(s.LocalDeviceId))
    //                  .OrderBy(s => s.NumberOfTimesPlayed)
    //                  .FirstOrDefault();
    //    return song != null ? new SongModelView(song) : null;
    //}

    //// 6. Top artist (artist with highest total play count)
    //public (string ArtistName, int PlayCount)? GetTopArtist(string playlistID)
    //{
    //    var songIds = GetSongIdsForPlaylist(playlistID);
    //    var topArtist = _db.All<SongModel>()
    //                       .Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.ArtistName))
    //                       .GroupBy(s => s.ArtistName)
    //                       .Select(g => new { ArtistName = g.Key, TotalPlays = g.Sum(s => s.NumberOfTimesPlayed) })
    //                       .OrderByDescending(x => x.TotalPlays)
    //                       .FirstOrDefault();
    //    return topArtist != null ? (topArtist.ArtistName!, topArtist.TotalPlays) : null;
    //}

    //// 7. Top album (album with highest total play count)
    //public (string AlbumName, int PlayCount)? GetTopAlbum(string playlistID)
    //{
    //    var songIds = GetSongIdsForPlaylist(playlistID);
    //    var topAlbum = _db.All<SongModel>()
    //                      .Where(s => songIds.Contains(s.LocalDeviceId) && !string.IsNullOrEmpty(s.AlbumName))
    //                      .GroupBy(s => s.AlbumName)
    //                      .Select(g => new { AlbumName = g.Key, TotalPlays = g.Sum(s => s.cou) })
    //                      .OrderByDescending(x => x.TotalPlays)
    //                      .FirstOrDefault();
    //    return topAlbum != null ? (topAlbum.AlbumName!, topAlbum.TotalPlays) : null;
    //}

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
}
