namespace Dimmer.DataAccess.Services;
public class StatsManagementService : IStatsManagementService
{
    Realm db;
    public StatsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
        OpenDB();
    }
    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }
    public IDataBaseService DataBaseService { get; }

    public async Task IncrementPlayCount(ObjectId songID)
    {
        
        try
        {
            await db.WriteAsync(() =>
            {
                var existingSong = db.Find<SongsModel>(songID);
                if (existingSong != null)
                {
                    existingSong.PlayCount++;
                    existingSong.LastPlayed = DateTime.Now;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error incrementing play count: {ex.Message}");
            
        }
    }

    public async Task IncrementSkipCount(ObjectId songID)
    {
        try
        {
            OpenDB();
            var song = db.Find<SongsModel>(songID);

            if (song is null)
            {
                return ;
            }
            if (db.IsInTransaction)
            {
                Debug.WriteLine("Tried to save inc but was in transaction");
                song.SkipCount++;
                song.LastPlayed = DateTime.Now;
            }
            else
            {
                await db.WriteAsync(() =>
                {
                    song.SkipCount++;
                    song.LastPlayed = DateTime.Now;
                });
            }
            }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);

        }
    }

}
