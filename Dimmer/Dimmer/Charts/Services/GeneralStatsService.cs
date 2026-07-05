using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;

public enum DateRangeFilter
{
    
    AllTime,
    Today,
    Last7Days,
    Last15Days,
    Last30Days,
    Last365Days,
    Last90Days,
}

public class GeneralStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;

    private readonly BehaviorSubject<DateRangeFilter> _dateFilter = new(DateRangeFilter.AllTime);
    public void SetDateFilter(DateRangeFilter filter) => _dateFilter.OnNext(filter);

    // --- OUTPUTS ---
    public IObservable<TextStat> TotalListeningTime { get; }
    public IObservable<TextStat> IntrovertExtrovertScore { get; }
    public IObservable<TextStat> SkipToCompletionRatio { get; }
    public IObservable<TextStat> CenterOfGravityYear { get; }
    public IObservable<TextStat> ParetoPrinciple { get; }
    public IObservable<TextStat> PrimaryDecade { get; }

    public IObservable<IReadOnlyList<LeaderboardItem>> TopSongs { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopArtists { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopAlbums { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> MusicalTrinity { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> ForgottenGems { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TimeCapsulePlaylist { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> FocusPlaylist { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> SundayMorningPlaylist { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> FibonacciArtists { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> AbandonedArtists { get; }

    public IObservable<IReadOnlyList<ChartPoint>> HabitsByHour { get; }
    public IObservable<IReadOnlyList<InsightStat>> LibraryInsights { get; }
    public IObservable<IReadOnlyList<HealthIssue>> LibraryHealth { get; }

    public GeneralStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var dbChangeSignal = _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Select(_ => true);

        var snapshotStream = Observable.CombineLatest(dbChangeSignal, _dateFilter, (_, f) => f)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(filter => Observable.FromAsync(() => Task.Run(() =>
            {
                using var bgRealm = _realmF.GetRealmInstance();
                return CalculateGeneralSnapshot(bgRealm, filter);
            })))
            .Switch().Publish().RefCount();

        TotalListeningTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        IntrovertExtrovertScore = snapshotStream.Select(s => s.IntroExtro).ObserveOn(RxSchedulers.UI);
        SkipToCompletionRatio = snapshotStream.Select(s => s.SkipRatio).ObserveOn(RxSchedulers.UI);
        CenterOfGravityYear = snapshotStream.Select(s => s.CenterYear).ObserveOn(RxSchedulers.UI);
        ParetoPrinciple = snapshotStream.Select(s => s.Pareto).ObserveOn(RxSchedulers.UI);
        PrimaryDecade = snapshotStream.Select(s => s.PrimaryDecade).ObserveOn(RxSchedulers.UI);

        TopSongs = snapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        TopArtists = snapshotStream.Select(s => s.TopArtists).ObserveOn(RxSchedulers.UI);
        TopAlbums = snapshotStream.Select(s => s.TopAlbums).ObserveOn(RxSchedulers.UI);
        MusicalTrinity = snapshotStream.Select(s => s.Trinity).ObserveOn(RxSchedulers.UI);
        ForgottenGems = snapshotStream.Select(s => s.ForgottenGems).ObserveOn(RxSchedulers.UI);
        TimeCapsulePlaylist = snapshotStream.Select(s => s.TimeCapsule).ObserveOn(RxSchedulers.UI);
        FocusPlaylist = snapshotStream.Select(s => s.FocusPlaylist).ObserveOn(RxSchedulers.UI);
        SundayMorningPlaylist = snapshotStream.Select(s => s.SundayMorning).ObserveOn(RxSchedulers.UI);
        FibonacciArtists = snapshotStream.Select(s => s.Fibonacci).ObserveOn(RxSchedulers.UI);
        AbandonedArtists = snapshotStream.Select(s => s.Abandoned).ObserveOn(RxSchedulers.UI);

        HabitsByHour = snapshotStream.Select(s => s.Hourly).ObserveOn(RxSchedulers.UI);
        LibraryInsights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
        LibraryHealth = snapshotStream.Select(s => s.Health).ObserveOn(RxSchedulers.UI);
    }

    private GeneralSnapshot CalculateGeneralSnapshot(Realm bgRealm, DateRangeFilter filter)
    {
        var cutoff = GetCutoffDate(filter);
        var allEvents = bgRealm.All<DimmerPlayEvent>().ToList();
        var periodEvents = cutoff == DateTimeOffset.MinValue ? allEvents : allEvents.Where(e => e.EventDate >= cutoff).ToList();
        var allSongs = bgRealm.All<SongModel>().ToList();

        // 1. Core Texts
        double totalSecs = periodEvents.Sum(e => Math.Max(0, e.PositionInSeconds));
        var totalTime = new TextStat("Total Time", $"{(int)TimeSpan.FromSeconds(totalSecs).TotalDays}d {TimeSpan.FromSeconds(totalSecs).Hours}h");

        double skips = periodEvents.Count(e => e.PlayType == 5);
        double completes = periodEvents.Count(e => e.WasPlayCompleted || e.PlayType == 3);
        var skipRatio = new TextStat("Skip/Complete Ratio", completes > 0 ? $"{skips / completes:F2}" : "0", $"Skips: {skips}, Completes: {completes}");

        var yearPlays = periodEvents.Select(e => bgRealm.Find<SongModel>(e.SongId)?.ReleaseYear).Where(y => y.HasValue).ToList();
        var centerYear = new TextStat("Center of Gravity", yearPlays.Count != 0 ? $"{Math.Round(yearPlays.Average(y => y!.Value))}" : "N/A");

        var decade = yearPlays.Select(y => (y!.Value / 10) * 10).GroupBy(d => d).OrderByDescending(g => g.Count()).FirstOrDefault();
        var primaryDecade = new TextStat("Primary Decade", decade != null ? $"{decade.Key}s" : "N/A");

        int introvert = allSongs.Count(s => s.DurationInSeconds > 300 || !s.HasLyrics || s.Tags.Any(t => t.Name == "Ambient"));
        int extrovert = allSongs.Count(s => s.DurationInSeconds < 180 || s.Tags.Any(t => t.Name == "Party" || t.Name == "Pop"));
        int totIE = introvert + extrovert;
        var introExtro = new TextStat("Introvert/Extrovert", totIE > 0 ? $"{((double)introvert / totIE) * 100:F0}% Introvert" : "N/A");

        var artistPlays = periodEvents.Where(e => e.PlayType == 0).GroupBy(e => bgRealm.Find<SongModel>(e.SongId)?.Artist?.Id).Where(g => g.Key != null).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        double paretoPct = artistPlays.Count != 0 ? (double)artistPlays.Take(Math.Max(1, (int)(artistPlays.Count * 0.2))).Sum() / artistPlays.Sum() * 100 : 0;
        var pareto = new TextStat("Pareto (80/20)", $"{paretoPct:F1}%", "Plays from top 20% of artists");

        // 2. Collections (Leaderboards)
        var topSongs = periodEvents.Where(e => e.PlayType == 0 && e.SongId.HasValue).GroupBy(e => e.SongId!.Value)
            .OrderByDescending(g => g.Count()).Take(15).Select((g, i) => {
                var s = bgRealm.Find<SongModel>(g.Key);
                return new LeaderboardItem($"#{i + 1}", s?.Title ?? "Unknown", $"{g.Count()} plays", s?.CoverImagePath ?? "");
            }).ToList();

        var topArtists = periodEvents.Where(e => e.PlayType == 0 && e.SongId.HasValue).GroupBy(e => bgRealm.Find<SongModel>(e.SongId.Value)?.Artist)
            .Where(g => g.Key != null).OrderByDescending(g => g.Count()).Take(15).Select((g, i) => new LeaderboardItem($"#{i + 1}", g.Key.Name, $"{g.Count()} plays", g.Key.ImagePath)).ToList();

        var topAlbums = periodEvents.Where(e => e.PlayType == 0 && e.SongId.HasValue).GroupBy(e => bgRealm.Find<SongModel>(e.SongId.Value)?.Album)
            .Where(g => g.Key != null).OrderByDescending(g => g.Count()).Take(15).Select((g, i) => new LeaderboardItem($"#{i + 1}", g.Key.Name, $"{g.Count()} plays", g.Key.ImagePath)).ToList();

        // 3. Power User Generated Playlists & Findings
        var timeCapsule = allEvents.Where(e => e.EventDate > DateTimeOffset.UtcNow.AddYears(-1).AddDays(-14) && e.EventDate < DateTimeOffset.UtcNow.AddYears(-1).AddDays(14))
            .Select(e => bgRealm.Find<SongModel>(e.SongId)).Where(s => s != null).Distinct().Take(30).Select(s => new LeaderboardItem("", s.Title, s.ArtistName, s.CoverImagePath)).ToList();

        var sundayMorn = allSongs.Where(s => s.Rating >= 4 && s.Tags.Any(t => t.Name == "Mellow" || t.Name == "Acoustic")).Take(30)
            .Select(s => new LeaderboardItem("", s.Title, s.ArtistName, s.CoverImagePath)).ToList();

        var focusList = allSongs.Where(s => s.DurationInSeconds > 240 && !s.HasLyrics && (!s.PlayHistory.Any() || s.PlayHistory.Max(p => p.DatePlayed) < DateTimeOffset.UtcNow.AddDays(-30))).Take(30)
            .Select(s => new LeaderboardItem("", s.Title, "Instrumental", s.CoverImagePath)).ToList();

        var forgotten = allSongs.Where(s => s.PlayHistory.Count > 15 && s.PlayHistory.Max(p => p.DatePlayed) < DateTimeOffset.UtcNow.AddMonths(-6)).OrderByDescending(s => s.PlayHistory.Count).Take(10)
            .Select((s, i) => new LeaderboardItem($"#{i + 1}", s.Title, $"Last played {s.PlayHistory.Max(p => p.DatePlayed):MMM yyyy}", s.CoverImagePath)).ToList();

        var abandoned = allEvents.GroupBy(e => bgRealm.Find<SongModel>(e.SongId)?.Artist).Where(g => g.Key != null && g.Count() < 5 && g.Min(e => e.DatePlayed) < DateTimeOffset.UtcNow.AddDays(-90)).Take(10)
            .Select(g => new LeaderboardItem("", g.Key.Name, $"Only {g.Count()} plays ever", g.Key.ImagePath)).ToList();

        var trinity = artistPlays.Take(3).Select((_, i) => new LeaderboardItem($"Pillar {i + 1}", topArtists.ElementAtOrDefault(i)?.Name ?? "", "")).ToList();

        // Fibonacci
        var firstPlayDate = allEvents.Min(e => e.DatePlayed);
        var fibs = new HashSet<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377 };
        var fibArtists = allEvents.GroupBy(e => bgRealm.Find<SongModel>(e.SongId)?.Artist).Where(g => g.Key != null)
            .Select(g => new { Art = g.Key, Day = (int)(g.Min(e => e.DatePlayed) - firstPlayDate).TotalDays })
            .Where(x => fibs.Contains(x.Day)).Select(x => new LeaderboardItem("", x.Art.Name, $"Discovered Day {x.Day}")).ToList();

        // 4. Charts
        var hourly = periodEvents.GroupBy(e => e.DatePlayed.ToLocalTime().Hour).Select(g => new ChartPoint($"{g.Key}:00", g.Count())).ToList();

        // 5. Library Health
        var health = new List<HealthIssue>();
        health.AddRange(allSongs.GroupBy(s => $"{s.Title.ToLower()}|{s.ArtistName.ToLower()}").Where(g => g.Count() > 1).Select(g => new HealthIssue("Duplicate", $"Song '{g.First().Title}' exists {g.Count()} times.", 2)));
        health.AddRange(bgRealm.All<AlbumModel>().AsEnumerable().Where(a => a.TrackTotal > 0 && a.SongsInAlbum.Count() != a.TrackTotal).Select(a => new HealthIssue("Incomplete Album", $"'{a.Name}' missing tracks.", 1)));
        health.AddRange(allSongs.Where(s => s.Rating > 0 && !s.PlayHistory.Any(p => p.WasPlayCompleted)).Take(10).Select(s => new HealthIssue("Rated but Unfinished", $"'{s.Title}' has stars but wasn't finished.", 0)));
        health.AddRange(allSongs.Where(s => !s.IsFileExists).Take(10).Select(s => new HealthIssue("Missing File", $"'{s.Title}' file not found.", 3)));

        // 6. Insights
        var insights = new List<InsightStat>
        {
            new("Adventurousness", $"{(double)abandoned.Count / Math.Max(1, topArtists.Count) * 100:F1}% of artists played this month are brand new.", "🌍"),
            new("Library Churn", $"You added {allSongs.Count(s => s.DateCreated > DateTimeOffset.UtcNow.AddDays(-30))} new songs in the last 30 days.", "🔄")
        };

        return new GeneralSnapshot(totalTime, introExtro, skipRatio, centerYear, pareto, primaryDecade,
            topSongs, topArtists, topAlbums, trinity, forgotten, timeCapsule, focusList, sundayMorn, fibArtists, abandoned,
            hourly, insights, health);
    }

    private DateTimeOffset GetCutoffDate(DateRangeFilter filter) => filter switch { DateRangeFilter.Today => DateTimeOffset.UtcNow.Date, DateRangeFilter.Last7Days => DateTimeOffset.UtcNow.AddDays(-7), DateRangeFilter.Last30Days => DateTimeOffset.UtcNow.AddDays(-30), DateRangeFilter.Last90Days => DateTimeOffset.UtcNow.AddDays(-90), DateRangeFilter.Last365Days => DateTimeOffset.UtcNow.AddYears(-1), _ => DateTimeOffset.MinValue };
    private record GeneralSnapshot(TextStat TotalTime, TextStat IntroExtro, TextStat SkipRatio, TextStat CenterYear, TextStat Pareto, TextStat PrimaryDecade, IReadOnlyList<LeaderboardItem> TopSongs, IReadOnlyList<LeaderboardItem> TopArtists, IReadOnlyList<LeaderboardItem> TopAlbums, IReadOnlyList<LeaderboardItem> Trinity, IReadOnlyList<LeaderboardItem> ForgottenGems, IReadOnlyList<LeaderboardItem> TimeCapsule, IReadOnlyList<LeaderboardItem> FocusPlaylist, IReadOnlyList<LeaderboardItem> SundayMorning, IReadOnlyList<LeaderboardItem> Fibonacci, IReadOnlyList<LeaderboardItem> Abandoned, IReadOnlyList<ChartPoint> Hourly, IReadOnlyList<InsightStat> Insights, IReadOnlyList<HealthIssue> Health);
}