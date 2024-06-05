namespace Dimmer.DataAccess.Services;
public class StatsManagementService : IStatsManagementService
{
    Realm db;
    public StatsManagementService(IDataBaseService dataBaseService)
    {
        DataBaseService = dataBaseService;
    }
    Realm OpenDB()
    {
        db = DataBaseService.GetRealm();
        return db;
    }
    public IDataBaseService DataBaseService { get; }

    public void IncrementPlayCount(string songTitle, double songDuration)
    {
        try
        {
            OpenDB();
            var song = db.All<SongsModel>().FirstOrDefault(s => s.Title == songTitle && s.DurationInSeconds == songDuration);

            if (song is null)
            {
                Debug.WriteLine("Song not found.");
                return;
            }

            using var transaction = db.BeginWrite();

            song.PlayCount++;
            song.LastPlayed = DateTime.Now;
            if (transaction.State == TransactionState.Running)
            {
                transaction.Commit();
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error incrementing play count: {ex.Message}");
            
        }
    }

    public void IncrementSkipCount(string songTitle, string artistName)
    {
        try
        {
            OpenDB();
            var song = db.All<SongsModel>().FirstOrDefault(s => s.Title == songTitle && s.ArtistName == artistName);

            if (song is null)
            {
                return;
            }
            db.Write(() =>
            {
                song.SkipCount++;
                song.LastPlayed = DateTime.Now;
            });
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);

        }
    }

    public void SetAsFavorite(ObjectId songID)
    {
        try
        {
            OpenDB();
            var song = db.All<SongsModel>().FirstOrDefault(s => s.Id == songID);
            if (song is not null)
            {
                db.Write(() =>
                {
                    song.IsFavorite = true;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
}
