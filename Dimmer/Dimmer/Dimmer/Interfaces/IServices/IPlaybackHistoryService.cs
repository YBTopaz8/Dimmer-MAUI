using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.IServices;
public interface IPlaybackHistoryService
{
    void RecordPlaybackEvent(SongModelView? songView, PlayType type, double? positionSeconds = null);
    Task<IReadOnlyList<DimmerPlayEvent>> GetPlayHistoryForSongAsync(string songId, int limit = 50);
    Task<IReadOnlyList<DimmerPlayEvent>> GetRecentPlayHistoryAsync(int limit = 100);
}