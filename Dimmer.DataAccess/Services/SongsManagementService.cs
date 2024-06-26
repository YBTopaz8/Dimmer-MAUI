﻿

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
            songs.AddRange(songss.Select(s => new SongsModel(s)));
            await db.WriteAsync(() =>
            {

                foreach (var song in songs)
                {
                    db.Add(song);                    
                    Debug.WriteLine("Added Song " + song.Title);
                }
            });
            
           return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw new Exception("Error when adding Songs in Batch " + ex.Message);
        }
    }

    public bool UpdateSongDetails(SongsModelView songsModelView)
    {
        try
        {
            var song = new SongsModel(songsModelView);
            db.Write(() =>
            {
                db.Add(song, true);
               
            });
            return true;
        }
        catch (Exception ex)
        {

            Debug.WriteLine(ex.Message);
            throw new Exception("Error when updating song " + ex.Message);
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

            Debug.WriteLine(ex.Message);
            throw new Exception("Error when adding Artists in Batch " + ex.Message);
        }
    }

    public void Dispose()
    {
        db?.Dispose();
    }

}
