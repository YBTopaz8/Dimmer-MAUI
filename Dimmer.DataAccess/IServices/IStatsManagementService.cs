namespace Dimmer.DataAccess.IServices;
public interface IStatsManagementService
{
    void IncrementPlayCount(ObjectId songID);
    
    void IncrementSkipCount(ObjectId songID);

    void SetAsFavorite(ObjectId songID);
}
