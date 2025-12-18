namespace Dimmer.Interfaces;
public interface IPlaybackHistoryService
{
    void RecordPlaybackEvent(SongModelView? songView, PlayType type, double? positionSeconds = null);
    Task<IReadOnlyList<DimmerPlayEvent>> GetPlayHistoryForSongAsync(string songId, int limit = 50);
    Task<IReadOnlyList<DimmerPlayEvent>> GetRecentPlayHistoryAsync(int limit = 100);
}