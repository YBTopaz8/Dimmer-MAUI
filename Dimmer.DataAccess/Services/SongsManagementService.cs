

using Realms;

namespace Dimmer.DataAccess.Services;
public class SongsManagementService : ISongsManagementService, IDisposable
{   
    Realm db ;

    public IList<SongsModelView> AllSongs { get; set; }
    public IDataBaseService DataBaseService { get; }
    public SongsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        db = DataBaseService.GetRealm();
        GetSongs();
    }


    public void GetSongs()
    {
        
        try
        {
            AllSongs?.Clear();                        
            var realmSongs = db.All<SongsModel>().OrderBy(x => x.DateAdded).ToList();
            AllSongs = new List<SongsModelView>(realmSongs.Select(song => new SongsModelView(song)));
            
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

    public async Task<bool> AddSongBatchAsync(IEnumerable<SongsModelView> songs)
    {
        try
        {
            var songsToAdd = songs
                .Where(song => !AllSongs.Any(s => s.Title == song.Title && s.DurationInSeconds == song.DurationInSeconds && s.ArtistName == song.ArtistName))
                .Select(song => new SongsModel(song))
                .ToList();

            using var realm = DataBaseService.GetRealm();
            await realm.WriteAsync(() =>
            {
                foreach (var song in songsToAdd)
                {
                    realm.Add(song);
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

    public async Task<bool> UpdateSongDetailsAsync(SongsModelView songsModelView)
    {
        try
        {
            await db.WriteAsync(() =>
            {
                var existingSong = db.Find<SongsModel>(songsModelView.Id);

                if (existingSong != null)
                {
                    existingSong.IsFavorite = songsModelView.IsFavorite;
                    existingSong.IsPlaying = false;
                    existingSong.PlayCount = songsModelView.PlayCount;
                    existingSong.LastPlayed = songsModelView.LastPlayed;
                }
                else
                {
                    var newSong = new SongsModel(songsModelView)
                    {
                        IsPlaying = false
                    };
                    db.Add(newSong, update: true);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when updating song: " + ex.Message);
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
