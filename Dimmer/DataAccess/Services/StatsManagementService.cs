namespace Dimmer_MAUI.DataAccess.Services;
public class StatsManagementService : IStatsManagementService
{
    Realm  db;
    public StatsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
    }
    public IDataBaseService DataBaseService { get; }

    public async Task IncrementPlayCount(string songID)
    {

        try
        {
            await db.WriteAsync(() =>
            {
                var existingSong = db.Find<SongModel>(songID);
                if (existingSong != null)
                {
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error incrementing play count: {ex.Message}");

        }
    }

    public async Task IncrementSkipCount(string songID)
    {
        try
        {
            var song = db.Find<SongModel>(songID);

            if (song is null)
            {
                return;
            }
            if (db.IsInTransaction)
            {
                Debug.WriteLine("Tried to save inc but was in transaction");
            }
            else
            {
                await db.WriteAsync(() =>
                {
                    //song.LastPlayed = DateTime.Now;
                });
            }
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);

        }
    }

}
