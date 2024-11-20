namespace Dimmer_MAUI.DataAccess.IServices;
public interface IStatsManagementService
{
    Task IncrementPlayCount(ObjectId songID);
    Task IncrementSkipCount(ObjectId songID);

}
