

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
            AllSongs = Enumerable.Empty<SongsModelView>().ToList();
            OpenDB();
            var realmSongs = db.All<SongsModel>().OrderBy(x => x.DateAdded).ToList();
            AllSongs = new List<SongsModelView>(realmSongs.Select(song => new SongsModelView(song)));
            //foreach (var song in realmSongs)
            //{
            //    AllSongs?.Add(new SongsModelView(song));
            //}
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
            var s = AllSongs.Count;

            var songs = new List<SongsModel>(songss.Select(s => new SongsModel(s)
            {
                Title = s.Title,
                FilePath = s.FilePath,
            }));
            await db.WriteAsync(() =>
            {

                foreach (var song in songs)
                {
                    db.Add(song);                    
                    Debug.WriteLine("Added Song " + song.Title);
                }
            }
            );
            
           return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw new Exception("Error when adding in Batch " + ex.Message);
        }
    }

    public void Dispose()
    {
        db?.Dispose();
    }
}
