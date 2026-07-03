
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;


public class ArtistStatsService
{
    private readonly BehaviorSubject<ObjectId?> _currentArtistId = new(null);

    // --- TEXT STATS ---
    public IObservable<TextStat> TotalListeningTime { get; }
    public IObservable<TextStat> ObsessionScore { get; }
    public IObservable<TextStat> CatalogCompletion { get; }
    public IObservable<TextStat> ArtistEddington { get; }

    // --- CHARTS ---
    public IObservable<IReadOnlyList<ChartPoint>> PlaysPerMonth { get; }
    public IObservable<IReadOnlyList<ChartPoint>> HourlyPreference { get; }
    public IObservable<IReadOnlyList<ChartPoint>> GenreBlending { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DeviceFootprint { get; }

    // --- COLLECTIONS ---
    public IObservable<IReadOnlyList<LeaderboardItem>> TopSongs { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> Collaborators { get; }

    public ArtistStatsService(IRealmFactory realmf)
    {
        // 1. THE MASTER PIPELINE
        // When the ID changes, switch to a new stream observing that artist's events
        var artistSnapshotStream = _currentArtistId
            .Where(id => id.HasValue).Select(id => id.Value)
            .Select(artistId =>
            {
                var realm = realmf.GetRealmInstance(); // Thread-local Realm

                // Get songs for this artist (in memory for fast lookup)
                var artistSongs = realm.All<SongModel>()
                    .Where(s => s.Artist.Id == artistId || s.ArtistToSong.Any(a => a.Id == artistId))
                    .ToList();

                var songIds = artistSongs.Select(s => (QueryArgument)s.Id).ToHashSet().ToArray();

                // Watch events for these songs
                return realm.All<DimmerPlayEvent>()
                    .Filter("SongId IN $0", songIds) // Realm fast filtering
                    .AsObservableChangeSet()
                    .Throttle(TimeSpan.FromMilliseconds(250)) // Prevent UI stuttering
                    .ToCollection()
                    .Select(events => CalculateArtistSnapshot(artistSongs, events));
            })
            .Switch() // If ID changes, cancel old subscription, start new one
            .Publish() // Share this single calculation with all 10+ outputs below
            .RefCount();

        // 2. BIND TEXT STATS (O(1) lookups from the snapshot)
        TotalListeningTime = artistSnapshotStream.Select(s => s.TotalTime)
            .ObserveOn(RxSchedulers.UI);
        ObsessionScore = artistSnapshotStream.Select(s => s.Obsession).ObserveOn(RxSchedulers.UI);
        CatalogCompletion = artistSnapshotStream.Select(s => s.Completion).ObserveOn(RxSchedulers.UI);
        ArtistEddington = artistSnapshotStream.Select(s => s.Eddington).ObserveOn(RxSchedulers.UI);

        // 3. BIND CHARTS
        PlaysPerMonth = artistSnapshotStream.Select(s => s.PlaysPerMonth).ObserveOn(RxSchedulers.UI);
        HourlyPreference = artistSnapshotStream.Select(s => s.HourlyPreference).ObserveOn(RxSchedulers.UI);
        GenreBlending = artistSnapshotStream.Select(s => s.GenreBlending).ObserveOn(RxSchedulers.UI);
        DeviceFootprint = artistSnapshotStream.Select(s => s.DeviceFootprint).ObserveOn(RxSchedulers.UI);

        // 4. BIND COLLECTIONS
        TopSongs = artistSnapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        Collaborators = artistSnapshotStream.Select(s => s.Collaborators).ObserveOn(RxSchedulers.UI);
    }

    // Set this from the ViewModel when navigating to an Artist page
    public void SetArtistId(ObjectId id) => _currentArtistId.OnNext(id);

    // --- THE HEAVY LIFTING HAPPENS EXACTLY ONCE PER THROTTLE WINDOW ---
    private ArtistSnapshot CalculateArtistSnapshot(List<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        // TEXT MATH
        double totalSeconds = events.Sum(e => Math.Max(0, e.PositionInSeconds));
        var ts = TimeSpan.FromSeconds(totalSeconds);
        var timeStr = ts.TotalDays > 1 ? $"{(int)ts.TotalDays}d {ts.Hours}h" : $"{ts.Hours}h {ts.Minutes}m";

        int uniqueSongsPlayed = events.Select(e => e.SongId).Distinct().Count();
        double completionPct = songs.Count > 0 ? ((double)uniqueSongsPlayed / songs.Count) * 100 : 0;

        var playCounts = songs.Select(s => events.Count(e => e.SongId == s.Id && e.PlayType == 0)).OrderByDescending(c => c).ToList();
        int eddington = 0;
        for (int i = 0; i < playCounts.Count; i++) { if (playCounts[i] >= i + 1) eddington = i + 1; else break; }

        int obsession = events.Count(e => e.PlayType == 6 || e.PlayType == 8) * 3 + events.Count(e => e.PlayType == 3);

        // CHART MATH
        var hourly = events.Where(e => e.PlayType == 0)
            .GroupBy(e => e.EventDate.ToLocalTime().Hour)
            .Select(g => new ChartPoint($"{g.Key}:00", g.Count())).ToList();

        var deviceFootprint = events.Where(e => !string.IsNullOrEmpty(e.DeviceName))
            .GroupBy(e => e.DeviceName)
            .Select(g => new ChartPoint(g.Key, g.Count())).ToList();

        // COLLECTION MATH
        var topSongs = songs
            .Select(s => new { Song = s, Plays = events.Count(e => e.SongId == s.Id && e.PlayType == 0) })
            .OrderByDescending(x => x.Plays).Take(10)
            .Select((x, i) => new LeaderboardItem($"#{i + 1}", x.Song.Title, $"{x.Plays} plays", x.Song.CoverImagePath))
            .ToList();

        // Return the immutable snapshot
        return new ArtistSnapshot(
            new TextStat("Time Listened", timeStr),
            new TextStat("Obsession Score", obsession.ToString(), "Repeats & Completions"),
            new TextStat("Catalog Played", $"{completionPct:F1}%", $"{uniqueSongsPlayed}/{songs.Count} songs"),
            new TextStat("Eddington Number", $"E-{eddington}", $"Played {eddington} songs {eddington}+ times"),
            new List<ChartPoint>(), // Plays per month logic...
            hourly,
            new List<ChartPoint>(), // Genre blending logic...
            deviceFootprint,
            topSongs,
            new List<LeaderboardItem>() // Collabs logic...
        );
    }

    private record ArtistSnapshot(
        TextStat TotalTime, TextStat Obsession, TextStat Completion, TextStat Eddington,
        IReadOnlyList<ChartPoint> PlaysPerMonth, IReadOnlyList<ChartPoint> HourlyPreference,
        IReadOnlyList<ChartPoint> GenreBlending, IReadOnlyList<ChartPoint> DeviceFootprint,
        IReadOnlyList<LeaderboardItem> TopSongs, IReadOnlyList<LeaderboardItem> Collaborators);
}
