namespace Dimmer_MAUI.DataAccess.IServices;
public interface IStatsManagementService
{
    Task IncrementPlayCount(string songID);
    Task IncrementSkipCount(string songID);

}
