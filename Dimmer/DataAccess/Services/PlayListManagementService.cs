using System.Diagnostics;

namespace Dimmer_MAUI.DataAccess.Services;
public class PlayListManagementService : IPlaylistManagementService
{
    Realm? _db;
    public IDataBaseService DataBaseService { get; }
    public ObservableCollection<PlaylistModelView> AllPlaylists { get; set; }
    public ObservableCollection<SongModelView> SongsFromSpecificPlaylist { get; set; }
    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        GetPlaylists();

    }
    private void LoadPlaylists()
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var realmPlayLists = _db.All<PlaylistModel>().ToList();
            AllPlaylists ??=new();
            AllPlaylists.Clear(); // Clear existing items
            foreach (var playlist in realmPlayLists)
            {
                AllPlaylists.Add(new PlaylistModelView(playlist));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading playlists: {ex.Message}");
        }
    }
    public ObservableCollection<PlaylistModelView> GetPlaylists()
    {
        LoadPlaylists();
        return AllPlaylists.ToObservableCollection(); // Return a copy to prevent external modification

    }

    public IList<string>? GetSongsIDsFromPlaylistID(string? playlistID)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            // Get the playlist
            var specificPlaylist = AllPlaylists
                .FirstOrDefault(x => x.LocalDeviceId == playlistID);

            if (specificPlaylist != null)
            {
                // Get the song IDs associated with this playlist
                var songIds = _db.All<PlaylistSongLink>()
                                .Where(link => link.PlaylistId == playlistID)
                                .ToList()
                                .Select(link => link.SongId)
                                .ToList();

                return songIds is not null ? songIds : Enumerable.Empty<string>().ToList();
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return null;
        }
    }
    public bool AddSongsToPlaylist(string playlistID, List<string> songIDs)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());

            var existingPlaylist = _db.All<PlaylistModel>().Where(p => p.LocalDeviceId == playlistID).ToList();

            if (existingPlaylist is null)
            {
                return false;
            }

            _db.Write(() =>
            {
                foreach (string songID in songIDs)
                {
                    var existingLinks = _db.All<PlaylistSongLink>()
                                         .Where(link => link.PlaylistId == playlistID && link.SongId == songID).ToList();
                    if (existingLinks is not null)
                    {
                        var existingLink = existingLinks.FirstOrDefault();                    
                        if (existingLink == null)
                        {
                            var newLink = new PlaylistSongLink
                            {                            
                                LocalDeviceId = GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistModel)),
                                PlaylistId = playlistID,
                                SongId = songID
                            };
                            _db.Add(newLink);
                        }
                    }
                }
                UpdatePlaylistMetadata(playlistID);
            });
            
            Debug.WriteLine("added songs to PL");
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
        var playlist = _db.All<PlaylistModel>().FirstOrDefault(p => p.LocalDeviceId == playlistID);

        if (playlist != null)
        {
            var songLinks = _db.All<PlaylistSongLink>().Where(link => link.PlaylistId == playlistID).ToList();
            playlist.TotalSongsCount = songLinks.Count;

            double totalDuration = 0;
            foreach (var link in songLinks)
            {
                var song = _db.All<SongModel>().FirstOrDefault(s => s.LocalDeviceId == link.SongId);
                if (song != null)
                {
                    totalDuration += song.DurationInSeconds;
                }
            }
            playlist.TotalDuration = totalDuration;
        }
    }
    public bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink=null, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var checkExist = _db.Find<PlaylistModel>(playlist.LocalDeviceId);
            if (IsAddSong)
            {
                _db.Write(() =>
                {
                    
                    _db.Add(new PlaylistModel(playlist), true);
                    if (playlistSongLink is not null)
                    {
                        if (playlistSongLink.LocalDeviceId is null)
                        {
                            playlistSongLink.LocalDeviceId = GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistModel));
                        }
                        _db.Add(playlistSongLink);
                    }
                });
            }
            if (IsRemoveSong)
            {
                _db.Add(new PlaylistModel(playlist), true);
            }
            if (IsDeletePlaylist)
            {
                if (checkExist is null)
                {
                    _db.Write(() =>
                    {
                        _db.Remove(new PlaylistModel(playlist));
                        if (playlistSongLink is not null)
                        {
                            _db.Remove(playlistSongLink);
                        }
                    });
                }
                _db.Write(() =>
                {
                    _db.Remove(new PlaylistModel(playlist));
                    if (playlistSongLink is not null)
                    {
                        _db.Remove(playlistSongLink);
                    }
                });
            }
            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist: {ex.Message}");
            return false;
        }
    }
        
    public bool DeletePlaylist(string playlistID)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var specificPlaylist = _db.All<PlaylistModel>().FirstOrDefault(p => p.LocalDeviceId == playlistID);
            _db.Write(() =>
            {
                _db.Remove(specificPlaylist!);
            });

            GetPlaylists();
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine("Error when deleting playlist " + ex.Message);
            throw new Exception("Error when deleting playlist" + ex.Message);

        }
    }

    public bool RenamePlaylist(string playlistID, string newPlaylistName)
    {
        try
        {
            _db = Realm.GetInstance(DataBaseService.GetRealm());
            var playlistToRename = _db.All<PlaylistModel>().FirstOrDefault(p => p.LocalDeviceId == playlistID);

            if (playlistToRename != null)
            {
                _db.Write(() =>
                {
                    playlistToRename.Name = newPlaylistName;
                });
                LoadPlaylists(); // Refresh the list
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error renaming playlist: {ex.Message}");
            return false;
        }
    }

}
