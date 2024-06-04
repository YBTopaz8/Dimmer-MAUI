
namespace Dimmer.DataAccess.Services;
public class PlayListManagementService : IPlaylistManagementService
{
    Realm db;
    public IDataBaseService DataBaseService { get; }
    public IList<PlaylistModelDup> PlayLists { get; set; }
    public PlayListManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
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
            OpenDB();
            var realmPlayLists = db.All<PlaylistModel>().OrderBy(x => x.DateCreated);
            PlayLists = DetachPlayListsFromDB(realmPlayLists);
            PlayLists ??= Enumerable.Empty<PlaylistModelDup>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting playlists: {ex.Message}");
        }
    }

    private IList<PlaylistModelDup> DetachPlayListsFromDB(IEnumerable<PlaylistModel> realmPlayLists)
    {
        return realmPlayLists.Select(playlist => playlist.Detach()).ToList();
    }


    public bool AddSongToPlayListWithPlayListID(SongsModel song, ObjectId playlistID)
    {
        try
        {
            OpenDB();
            var specificPlaylist = db.All<PlaylistModel>().FirstOrDefault(p => p.Id == playlistID);
            if (specificPlaylist is not null)
            {
                specificPlaylist.SongsID.Add(song.Id);
                db.Write(() =>
                {
                    db.Add(specificPlaylist);
                });
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist {ex.Message}");
            return false;
        }
    }

    public bool AddSongToPlayListWithPlayListName(SongsModel song, string playlistName)
    {
        try
        {
            OpenDB();
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
                }

                if (!specificPlaylist.SongsID.Contains(song.Id))
                {
                    specificPlaylist.SongsID.Add(song.Id);
                    specificPlaylist.TotalDuration += song.DurationInSeconds;
                    specificPlaylist.TotalSize += song.FileSize;
                }

            });
            
            return true;
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error exception when adding to playlist {ex.Message}");
            return false;
        }
    }
}
