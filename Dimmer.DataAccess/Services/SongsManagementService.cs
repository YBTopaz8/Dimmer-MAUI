

using Realms;

namespace Dimmer.DataAccess.Services;
public class SongsManagementService : ISongsManagementService, IDisposable
{   Realm db;

    public IList<SongsModelView> AllSongs { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        GetSongs();
    }

    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }

    public void GetSongs()
    {
        
        try
        {
            AllSongs?.Clear();
            OpenDB();
                        
            var realmSongs = db.All<SongsModel>().OrderBy(x => x.DateAdded).ToList();
            AllSongs = new List<SongsModelView>(realmSongs.Select(song => new SongsModelView(song)));
            
            AllSongs ??= Enumerable.Empty<SongsModelView>().ToList();
         
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
        
    }

    public Task<SongsModel> FindSongsByTitleAsync(string searchText)
    {
        throw new NotImplementedException();        
    }

    public async Task<bool> AddSongAsync(SongsModel song)
    {
        try
        {
            return await db.WriteAsync(() =>
            {
                db.Add(song);
                return true;
            });
        }
        catch (Exception ex)
        {
            throw new Exception("Failed while inserting Song " + ex.Message) ;
        }
    }

    public async Task<bool> AddSongBatchAsync(IEnumerable<SongsModelView> songss)
    {
        try
        {
            var songs = new List<SongsModel>();
            List<SongsModelView> songsToAddOnly = new List<SongsModelView>();
            foreach (var song in songss)
            {
                if (!AllSongs.Any(s => s.Title == song.Title && s.DurationInSeconds == song.DurationInSeconds && s.ArtistName == song.ArtistName))
                {
                    songs.Add(new SongsModel(song));
                }
            }
            
            await db.WriteAsync(() =>
            {

                foreach (var song in songs)
                {
                    db.Add(song);                    
                    Debug.WriteLine("Added Song " + song.Title);
                }
            });
            GetSongs();
           return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when batch add song " + ex.Message);
            throw new Exception("Error when adding Songs in Batch " + ex.Message);
        }
    }

    public bool UpdateSongDetails(SongsModelView songsModelView)
    {
        try
        {
            // Run the write operation in a separate task
          
            db.Write(() =>
            {
                var song = new SongsModel(songsModelView);
                song.IsPlaying = false;
                db.Add(song, update: true);
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song " + ex.Message);
            return false;
        }
    }


    public async Task<bool> AddArtistsBatchAsync(IEnumerable<ArtistModelView> artistss)
    {
        try
        {
            var artists = new List<ArtistModel>();
            artists.AddRange(artistss.Select(art => new ArtistModel(art)));
            
            await db.WriteAsync(() =>
            {
                db.Add(artists);
            });
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine("Error when batchaddArtist " + ex.Message);
            return false;
        }
    }

    public void Dispose()
    {
        db?.Dispose();
    }

}
