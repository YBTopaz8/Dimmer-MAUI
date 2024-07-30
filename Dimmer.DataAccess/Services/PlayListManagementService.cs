
using Dimmer.Models;

namespace Dimmer.DataAccess.Services;
public class PlayListManagementService : IPlaylistManagementService
{
    Realm db;
    public IDataBaseService DataBaseService { get; }
    public IList<PlaylistModelView> AllPlaylists { get; set; }

    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        GetPlayLists();
    }

    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }
    
    public void GetPlayLists()
    {

       try
        {
            AllPlaylists?.Clear();
            OpenDB();
            var realmPlayLists = db.All<PlaylistModel>().OrderBy(x => x.DateCreated).ToList();
            AllPlaylists = new List<PlaylistModelView>(realmPlayLists.Select(playlist => new PlaylistModelView(playlist)));
            
            AllPlaylists ??= Enumerable.Empty<PlaylistModelView>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting playlists: {ex.Message}");
        }
    }



    public bool AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        try
        {
            //OpenDB();
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
            if(specificPlaylist is null)
            {
                return false;
            }
            db.Write(() =>
            {
                if (!specificPlaylist!.SongsIDs.Contains(song.Id))
                {
                    specificPlaylist.SongsIDs.Add(song.Id);
                    specificPlaylist.TotalDuration += song.DurationInSeconds;
                    specificPlaylist.TotalSize += song.FileSize;
                    specificPlaylist.TotalSongsCount += 1;
                }
            });
            GetPlayLists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist {ex.Message}");
            return false;
        }
    }

    public bool AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        try
        {
            var songmodel = new SongsModel(song);
            PlaylistModel specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Name == playlistName);

            db.Write(() =>
            {
                if (specificPlaylist is null)
                {
                    specificPlaylist = new PlaylistModel
                    {
                        Id = ObjectId.GenerateNewId(),
                        Name = playlistName,
                        DateCreated = DateTimeOffset.Now,
                        TotalDuration = 0,
                        TotalSize = 0,
                    };

                    db.Add(specificPlaylist);
                    var s = new PlaylistModelView(specificPlaylist);
                }

                if (!specificPlaylist.SongsIDs.Contains(songmodel.Id))
                {
                    specificPlaylist.SongsIDs.Add(songmodel.Id);
                    specificPlaylist.TotalDuration += songmodel.DurationInSeconds;
                    specificPlaylist.TotalSize += songmodel.FileSize;
                    specificPlaylist.TotalSongsCount += 1; 
                }

            });
            GetPlayLists();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist {ex.Message}");
            return false;
        }
    }
    public bool RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        try
        {
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Name == playlistName);
            if (specificPlaylist is null)
            {
                //Shell.Current.DisplayAlert("Error", "Playlist not found", "OK");
                return false;
            }
            db.Write(() =>
            {
                specificPlaylist.SongsIDs.Remove(song.Id);
                specificPlaylist.TotalDuration -= song.DurationInSeconds;
                specificPlaylist.TotalSize -= song.FileSize;
                specificPlaylist.TotalSongsCount -= 1;
            });
            GetPlayLists();
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine(ex.Message);
            throw new Exception("Error when removing from playlist" + ex.Message);
        }
    }

    public bool RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    public bool DeletePlaylist(ObjectId playlistID)
    {
        try
        {
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
            db.Write(() =>
            {
                db.Remove(specificPlaylist);
            });

            GetPlayLists();
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine(ex.Message);
            throw new Exception("Error when deleting playlist" + ex.Message);
            
        }
    }

    public bool RenamePlaylist(ObjectId playlistID, string newPlaylistName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
        if (specificPlaylist is null)
        {
            //await Shell.Current.DisplayAlert("Error While Renaming", "No Such Playlist Exists", "OK");
            return false;
        }
        db.Write(() =>
        {
            specificPlaylist.Name = newPlaylistName;
        });

        GetPlayLists();
        return true;
    }
}
