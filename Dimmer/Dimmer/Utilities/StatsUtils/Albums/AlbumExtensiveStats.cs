using System.Collections.Generic;
using System.Linq;

namespace Dimmer.Utilities.StatsUtils.Albums;

public static class AlbumExtensiveStats
{
    public static List<LabelValue> GetDropOffCurve(IEnumerable<SongModel> albumSongs, List<DimmerPlayEvent> events)
    {
        return albumSongs
            .Where(s => s.TrackNumber.HasValue)
            .OrderBy(s => s.TrackNumber)
            .Select(s => new LabelValue
            {
                Label = $"Trk {s.TrackNumber}",
                Value = SongStats.GetCompletedPlayCount(s, events) // Fixed: Now uses filtered events!
            })
            .ToList();
    }

    public static List<LabelValue> GetLyricalDensity(IEnumerable<SongModel> albumSongs)
    {
        return albumSongs
            .OrderByDescending(s => s.EmbeddedSync?.Count ?? 0)
            .Select(s => new LabelValue
            {
                Label = s.Title,
                Value = s.EmbeddedSync?.Count ?? 0
            })
            .ToList();
    }

    public static List<DimmerStats> GetTrackEventBreakdown(IEnumerable<SongModel> albumSongs, List<DimmerPlayEvent> events)
    {
        return albumSongs.Select(s => new DimmerStats
        {
            ComparisonLabel = s.Title,
            IntValue = SongStats.GetCompletedPlayCount(s, events), // Series 1 (Plays)
            DoubleValue = SongStats.GetSkipCount(s, events),       // Series 2 (Skips)
            SecondaryValue = events.Count(e => e.SongId == s.Id && e.PlayType == 1) // Series 3 (Pauses)
        }).ToList();
    }

    public static int CalculateEddingtonNumber(IEnumerable<SongModel> albumSongs, List<DimmerPlayEvent> events)
    {
        var playCounts = albumSongs.Select(s => SongStats.GetCompletedPlayCount(s, events)).OrderByDescending(c => c).ToList();
        int eddington = 0;
        for (int i = 0; i < playCounts.Count; i++)
        {
            if (playCounts[i] >= i + 1) eddington = i + 1;
            else break;
        }
        return eddington;
    }
}