namespace Dimmer.DataAccess.IServices;
public interface IStatsManagementService
{
    void IncrementPlayCount(string songTitle, double songDuration);
    
    void IncrementSkipCount(string songTitle, string artistName);

    void SetAsFavorite(ObjectId songID);
}
