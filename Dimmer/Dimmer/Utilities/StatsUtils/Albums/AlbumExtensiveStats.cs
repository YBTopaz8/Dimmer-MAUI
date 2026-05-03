using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Utilities.StatsUtils.Albums
{
    public static class AlbumExtensiveStats
    {
        // 13. Drop-off Rate (Retention across the tracklist)
        public static List<LabelValue> GetDropOffCurve(IEnumerable<SongModel> albumSongs)
        {
            return albumSongs
                .Where(s => s.TrackNumber.HasValue)
                .OrderBy(s => s.TrackNumber)
                .Select(s => new LabelValue
                {
                    Label = $"Trk {s.TrackNumber}",
                    Value = s.PlayCompletedCount
                })
                .ToList();
        }

        // 17. Lyrical Density (Who talks the most?)
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

        // 20. Event Breakdown 100% Stacked (Plays vs Skips vs Pauses)
        public static List<DimmerStats> GetTrackEventBreakdown(IEnumerable<SongModel> albumSongs)
        {
            return albumSongs.Select(s => new DimmerStats
            {
                ComparisonLabel = s.Title,
                IntValue = s.PlayCompletedCount, // Series 1
                DoubleValue = s.SkipCount,    // Series 2
                SecondaryValue = s.PauseCount // Series 3
            }).ToList();
        }

        // 22. Eddington Number (Math concept: Max 'E' where you've listened to 'E' songs 'E' times)
        public static int CalculateEddingtonNumber(IEnumerable<SongModel> albumSongs)
        {
            var playCounts = albumSongs.Select(s => s.PlayCompletedCount).OrderByDescending(c => c).ToList();
            int eddington = 0;
            for (int i = 0; i < playCounts.Count; i++)
            {
                if (playCounts[i] >= i + 1) eddington = i + 1;
                else break;
            }
            return eddington;
        }
    }
}
