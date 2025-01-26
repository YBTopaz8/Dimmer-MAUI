namespace Dimmer_MAUI.DataAccess.Services;
public class PlayListManagementService : IPlaylistManagementService
{
    Realm? db;
    public IDataBaseService DataBaseService { get; }
    public IList<PlaylistModelView> AllPlaylists { get; set; }
    public ObservableCollection<SongModelView> SongsFromSpecificPlaylist { get; set; }
    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        GetPlaylists();

    }

    public List<PlaylistModelView> GetPlaylists()
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var realmPlayLists = db.All<PlaylistModel>().ToList();
            AllPlaylists = new List<PlaylistModelView>(realmPlayLists.Select(playlist => new PlaylistModelView(playlist)));
            AllPlaylists ??= Enumerable.Empty<PlaylistModelView>().ToList();
            return AllPlaylists.ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting playlists: {ex.Message}");
            return Enumerable.Empty<PlaylistModelView>().ToList();
        }
    }

    public IList<string> GetSongsIDsFromPlaylistID(string playlistID)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            // Get the playlist
            var specificPlaylist = AllPlaylists
                .FirstOrDefault(x => x.LocalDeviceId == playlistID);

            if (specificPlaylist != null)
            {
                // Get the song IDs associated with this playlist
                var songIds = db.All<PlaylistSongLink>()
                                .Where(link => link.PlaylistId == playlistID)
                                .ToList()
                                .Select(link => link.SongId)
                                .ToList();

                return songIds is not null ? songIds : Enumerable.Empty<string>().ToList();
            }

            return Enumerable.Empty<string>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<string>().ToList();
        }
    }

    public bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink=null, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        try
        {
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var checkExist = db.Find<PlaylistModel>(playlist.LocalDeviceId);
            if (IsAddSong)
            {
                db.Write(() =>
                {
                    
                    db.Add(new PlaylistModel(playlist), true);
                    if (playlistSongLink is not null)
                    {
                        if (playlistSongLink.LocalDeviceId is null)
                        {
                            playlistSongLink.LocalDeviceId = GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistModel));
                        }
                        db.Add(playlistSongLink);
                    }
                });
            }
            if (IsRemoveSong)
            {
                db.Add(new PlaylistModel(playlist), true);
            }
            if (IsDeletePlaylist)
            {
                if (checkExist is null)
                {
                    db.Write(() =>
                    {
                        db.Remove(new PlaylistModel(playlist));
                        if (playlistSongLink is not null)
                        {
                            db.Remove(playlistSongLink);
                        }
                    });
                }
                db.Write(() =>
                {
                    db.Remove(new PlaylistModel(playlist));
                    if (playlistSongLink is not null)
                    {
                        db.Remove(playlistSongLink);
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
            db = Realm.GetInstance(DataBaseService.GetRealm());
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.LocalDeviceId == playlistID);
            db.Write(() =>
            {
                db.Remove(specificPlaylist!);
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
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.LocalDeviceId == playlistID);
        db = Realm.GetInstance(DataBaseService.GetRealm());
        
        if (specificPlaylist is null)
        {
            //await Shell.Current.DisplayAlert("Error While Renaming", "No Such Playlist Exists", "OK");
            return false;
        }
        db.Write(() =>
        {
            specificPlaylist.Name = newPlaylistName;
        });

        GetPlaylists();
        return true;
    }
}
