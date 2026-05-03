using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Utilities.StatsUtils.Artists;

public static class ArtistExtensiveStats
{
    // 23. Era / Decade Distribution
    public static List<LabelValue> GetDecadeDistribution(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Where(s => s.ReleaseYear > 1900)
            .GroupBy(s => (s.ReleaseYear / 10) * 10) // e.g., 1994 -> 1990
            .Select(g => new LabelValue
            {
                Label = $"{g.Key}s",
                Value = g.Sum(s => s.PlayCompletedCount)
            })
            .OrderBy(x => x.Label)
            .ToList();
    }

    // 24. Most Obsessed-Over Formula (Not just raw plays)
    public static List<LabelValue> GetObsessionRankings(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Select(s => new LabelValue
            {
                Label = s.Title,
                // Custom Obsession Algorithm
                Value = (s.RepeatCount * 3) + (s.RestartCount * 2) + (s.NumberOfTimesFaved * 5) + s.PlayCompletedCount
            })
            .OrderByDescending(s => s.Value)
            .Take(10)
            .ToList();
    }

    // 30. Collaborator Network
    public static List<LabelValue> GetCollaborators(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Where(s => !string.IsNullOrEmpty(s.OtherArtistsName))
            .SelectMany(s => s.OtherArtistsName.Split(',')) // Assuming comma separated
            .Select(n => n.Trim())
            .GroupBy(n => n)
            .Select(g => new LabelValue { Label = g.Key, Value = g.Count() })
            .OrderByDescending(g => g.Value)
            .Take(10)
            .ToList();
    }

    // 31. Vibe / BPM Spread
    public static List<DimmerStats> GetBpmVsEngagement(IEnumerable<SongModel> artistSongs)
    {
        return artistSongs
            .Where(s => s.BPM.HasValue && s.BPM > 0)
            .Select(s => new DimmerStats
            {
                StatTitle = s.Title,
                XValue = s.BPM.Value,           // X-Axis
                YValue = s.EngagementScore            // Y-Axis
            })
            .ToList();
    }
}