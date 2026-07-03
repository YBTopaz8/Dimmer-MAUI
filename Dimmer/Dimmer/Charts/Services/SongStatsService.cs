using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;

public class SongStatsService
{
    private readonly Realm _realm;

    // INPUT TRIGGER from ViewModel
    private readonly BehaviorSubject<ObjectId?> _currentSongId = new(null);
    public void SetSongId(ObjectId id) => _currentSongId.OnNext(id);

    // OUTPUTS
    public IObservable<TextStat> SkipRate { get; }
    public IObservable<IReadOnlyList<ChartPoint>> ActionRadarChart { get; }
    public IObservable<IReadOnlyList<InsightStat>> SongInsights { get; }

    public SongStatsService(IRealmFactory realmF)
    {
        _realm = realmF.GetRealmInstance();

        var songSnapshotStream = _currentSongId
           .Where(id => id.HasValue).Select(id => id.Value)
           .Select(songId =>
           {
               // Get the song for metadata (duration, title, etc)
               var song = _realm.Find<SongModel>(songId);

               return _realm.All<DimmerPlayEvent>()
                   .Where(e => e.SongId == songId)
                   .AsObservableChangeSet()
                   .Throttle(TimeSpan.FromMilliseconds(250))
                   .ToCollection()
                   // OFFLOAD TO BACKGROUND THREAD
                   .Select(events => Observable.FromAsync(() => Task.Run(() =>
                       CalculateSongSnapshot(song, events)
                   )))
                   .Switch();
           })
           .Switch()
           .Publish()
           .RefCount();

        SkipRate = songSnapshotStream.Select(s => s.SkipRate).ObserveOn(RxSchedulers.UI);
        ActionRadarChart = songSnapshotStream.Select(s => s.ActionRadar).ObserveOn(RxSchedulers.UI);
        SongInsights = songSnapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private SongSnapshot CalculateSongSnapshot(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var insights = new List<InsightStat>();

        int plays = events.Count(e => e.PlayType == 0);
        int skips = events.Count(e => e.PlayType == 5);

        // Basic Text Stat
        double rate = plays > 0 ? ((double)skips / plays) * 100 : 0;
        var skipRateStat = new TextStat("Skip Rate", $"{rate:F1}%");

        // Action Radar
        var radar = new List<ChartPoint>
        {
            new("Plays", plays),
            new("Skips", skips),
            new("Pauses", events.Count(e => e.PlayType == 1)),
            new("Repeats", events.Count(e => e.PlayType == 6))
        };

        // Insight A: Drop-off Point Analysis (Answers Q5)
        // Logic: Cluster the skip positions. If 50%+ of skips happen within a 15-second window, it's a trend.
        var skipEvents = events.Where(e => e.PlayType == 5 && e.PositionInSeconds > 0).ToList();
        if (skipEvents.Count > 3)
        {
            // Group skips into 15-second "buckets"
            var dropOffBucket = skipEvents
                .GroupBy(e => Math.Floor(e.PositionInSeconds / 15) * 15)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (dropOffBucket != null && (double)dropOffBucket.Count() / skipEvents.Count > 0.4) // 40% of skips in this bucket
            {
                TimeSpan start = TimeSpan.FromSeconds(dropOffBucket.Key);
                TimeSpan end = TimeSpan.FromSeconds(dropOffBucket.Key + 15);
                insights.Add(new InsightStat("Common Skip Point",
                    $"When you skip this song, you usually do it between {start:m\\:ss} and {end:m\\:ss}.", "⏭️"));
            }
        }

        // Insight B: Time of Day Signature (Answers Q4)
        // Logic: Group play events by Morning, Afternoon, Evening, Night
        if (plays > 4)
        {
            var timeOfDay = events.Where(e => e.PlayType == 0)
                .GroupBy(e => e.EventDate.ToLocalTime().Hour switch
                {
                    >= 5 and < 12 => "Morning",
                    >= 12 and < 17 => "Afternoon",
                    >= 17 and < 22 => "Evening",
                    _ => "Late Night"
                })
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (timeOfDay != null && (double)timeOfDay.Count() / plays > 0.5) // 50% of plays in one block
            {
                insights.Add(new InsightStat("Time of Day",
                    $"This is definitively a {timeOfDay.Key} track for you.", "🕰️"));
            }
        }

        // Insight C: The Binge Factor
        var bingeCount = events.Count(e => e.PlayType == 6 || e.PlayType == 8);
        if (bingeCount > plays * 0.3 && plays > 5) // If 30% of interactions are rewinds/repeats
        {
            insights.Add(new InsightStat("Highly Addictive",
                "You tend to put this song on repeat or rewind it frequently.", "🔁"));
        }

        return new SongSnapshot(skipRateStat, radar, insights);
    }

    private record SongSnapshot(
        TextStat SkipRate,
        IReadOnlyList<ChartPoint> ActionRadar,
        IReadOnlyList<InsightStat> Insights);

}