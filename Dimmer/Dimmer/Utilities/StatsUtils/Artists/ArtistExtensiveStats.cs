namespace Dimmer.Utilities.StatsUtils.Artists;

public static class ArtistExtensiveStats
{
    public static List<LabelValue> GetDecadeDistribution(IEnumerable<SongModel> artistSongs, List<DimmerPlayEvent> events)
    {
        return artistSongs
            .Where(s => s.ReleaseYear > 1900)
            .GroupBy(s => (s.ReleaseYear / 10) * 10)
            .Select(g => new LabelValue
            {
                Label = $"{g.Key}s",
                Value = g.Sum(s => SongStats.GetCompletedPlayCount(s, events)) // Fixed
            })
            .OrderBy(x => x.Label)
            .ToList();
    }

    public static List<LabelValue> GetObsessionRankings(IEnumerable<SongModel> artistSongs, List<DimmerPlayEvent> events)
    {
        return artistSongs
            .Select(s => new LabelValue
            {
                Label = s.Title,
                // Custom Obsession Algorithm calculated purely from the filtered events!
                Value = (events.Count(e => e.SongId == s.Id && (e.PlayType == 6 || e.PlayType == 8)) * 3) +
                        (SongStats.GetCompletedPlayCount(s, events))
            })
            .OrderByDescending(s => s.Value)
            .Take(10)
            .ToList();
    }

    public static List<LabelValue> GetCollaborators(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Where(s => !string.IsNullOrEmpty(s.OtherArtistsName))
            .SelectMany(s => s.OtherArtistsName.Split(','))
            .Select(n => n.Trim())
            .GroupBy(n => n)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Count() })
            .OrderByDescending(g => g.Value)
            .Take(10)
            .ToList();
    }

    public static List<DimmerStats> GetBpmVsEngagement(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Where(s => s.BPM.HasValue && s.BPM > 0)
            .Select(s => new DimmerStats
            {
                StatTitle = s.Title,
                XValue = s.BPM.Value,
                YValue = s.EngagementScore
            })
            .ToList();
    }
}