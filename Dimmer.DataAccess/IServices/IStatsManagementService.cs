namespace Dimmer.DataAccess.IServices;
public interface IStatsManagementService
{
    Task IncrementPlayCount(ObjectId songID);
    
    Task IncrementSkipCount(ObjectId songID);

}
