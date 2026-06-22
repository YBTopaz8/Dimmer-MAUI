namespace Dimmer.Utilities.StatsUtils;

public static class SongExtensiveStats
{
    public static List<LabelValue> GetEngagementFunnel(SongModel song, List<DimmerPlayEvent> events)
    {
        return new List<LabelValue>
        {
            new() { Label = "Total Plays", Value = SongStats.GetPlayCount(song, events) },
            new() { Label = "Completed", Value = SongStats.GetCompletedPlayCount(song, events) },
            // Favorited is usually a toggle, but if you track it as an event, count it here. Otherwise fallback to the model.
            new() { Label = "Favorited", Value = song.NumberOfTimesFaved },
            new() { Label = "In Playlists", Value = song.PlaylistsHavingSong?.Count ?? 0 }
        };
    }

    public static List<DimmerStats> GetEventHotspots(IEnumerable<DimmerPlayEvent> events, int playType)
    {
        return events
            .Where(e => e.PlayType == playType)
            .GroupBy(e => Math.Floor(e.PositionInSeconds / 10) * 10)
            .Select(g => new DimmerStats
            {
                ComparisonLabel = $"{g.Key}s - {g.Key + 10}s",
                DoubleValue = g.Count(),
                XValue = g.Key
            })
            .OrderBy(x => x.XValue)
            .ToList();
    }

    public static List<LabelValue> GetDeviceEcosystem(IEnumerable<DimmerPlayEvent> events)
    {
        return events
            .Where(e => !string.IsNullOrEmpty(e.DeviceFormFactor))
            .GroupBy(e => e.DeviceFormFactor)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Count() })
            .ToList();
    }

    public static List<LabelValue> GetActionRadar(SongModel song, List<DimmerPlayEvent> events)
    {
        return new List<LabelValue>
        {
            new() { Label = "Pauses", Value = events.Count(e => e.SongId == song.Id && e.PlayType == 1) },
            new() { Label = "Resumes", Value = events.Count(e => e.SongId == song.Id && e.PlayType == 2) },
            new() { Label = "Seeks", Value = events.Count(e => e.SongId == song.Id && e.PlayType == 4) },
            new() { Label = "Skips", Value = SongStats.GetSkipCount(song, events) },
            new() { Label = "Repeats", Value = events.Count(e => e.SongId == song.Id && (e.PlayType == 6 || e.PlayType == 8)) },
            new() { Label = "Completions", Value = SongStats.GetCompletedPlayCount(song, events) }
        };
    }
}