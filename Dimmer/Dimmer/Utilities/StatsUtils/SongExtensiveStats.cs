using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Utilities.StatsUtils
{
    public static class SongExtensiveStats
    {
        // 1. Engagement Funnel
        public static List<LabelValue> GetEngagementFunnel(SongModel song)
        {
            return new List<LabelValue>
        {
            new() { Label = "Total Plays", Value = song.PlayCount },
            new() { Label = "Completed", Value = song.PlayCompletedCount },
            new() { Label = "Favorited", Value = song.NumberOfTimesFaved },
            new() { Label = "In Playlists", Value = song.PlaylistsHavingSong?.Count ?? 0 }
        };
        }

        // 2. & 3. Hotspots (Skips & Replays mapped by Second in Song)
        public static List<DimmerStats> GetEventHotspots(IEnumerable<DimmerPlayEvent> events, int playType)
        {
            // Groups exact seconds into 10-second chunks to make the chart readable
            return events
                .Where(e => e.PlayType == playType)
                .GroupBy(e => Math.Floor(e.PositionInSeconds / 10) * 10)
                .Select(g => new DimmerStats
                {
                    ComparisonLabel = $"{g.Key}s - {g.Key + 10}s",
                    DoubleValue = g.Count(), // Number of times skipped/seeked in this chunk
                    XValue = g.Key // X-Axis point for Scatter/Line
                })
                .OrderBy(x => x.XValue)
                .ToList();
        }

        // 6. Device Ecosystem
        public static List<LabelValue> GetDeviceEcosystem(IEnumerable<DimmerPlayEvent> events)
        {
            return events
                .Where(e => !string.IsNullOrEmpty(e.DeviceFormFactor))
                .GroupBy(e => e.DeviceFormFactor)
                .Select(g => new LabelValue { Label = g.Key, Value = g.Count() })
                .ToList();
        }

        // 8. Action Radar (Spider/Radar Chart)
        public static List<LabelValue> GetActionRadar(SongModel song)
        {
            return new List<LabelValue>
        {
            new() { Label = "Pauses", Value = song.PauseCount },
            new() { Label = "Resumes", Value = song.ResumeCount },
            new() { Label = "Seeks", Value = song.SeekCount },
            new() { Label = "Skips", Value = song.SkipCount },
            new() { Label = "Repeats", Value = song.RepeatCount },
            new() { Label = "Completions", Value = song.PlayCompletedCount }
        };
        }

     
    }
}
