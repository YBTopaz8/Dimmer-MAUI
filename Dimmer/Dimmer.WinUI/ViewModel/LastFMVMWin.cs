using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.WinUI.ViewModel;

public partial class LastFMVMWin :LastFMViewModel
{
    public LastFMVMWin(ILastfmService lastfmService, ILogger<LastFMViewModel> logger, IRealmFactory realmFact) : base(lastfmService, logger, realmFact)
    {

    }

    public async override Task LoadUserLastFMDataAsync(LastFMUserView? user)
    {
        await base.LoadUserLastFMDataAsync(user);

        await GeneratePremiumInsights(user, CollectionOfUserRecentTracks);
    }
    #region --- Pano Scrobbler Premium Visualizations & Utilities ---

    [ObservableProperty] public partial double LastFmMilestoneProgress { get; set; }

    // Charts
    [ObservableProperty] public partial ISeries[]? WeeklyScrobblesChart { get; set; }
    [ObservableProperty]
    public partial ICartesianAxis[]? ChartXAxes { get; set; }
    [ObservableProperty] public partial ISeries[]? TimeOfDayChart { get; set; }


    [ObservableProperty] public partial string FavoriteListeningTime { get; set; } = "...";
    [ObservableProperty] public partial string WeeklyScrobbleCountText { get; set; } = "0";

    private Task GeneratePremiumInsights(LastFMUserView user, IEnumerable<Hqub.Lastfm.Entities.Track>? recentTracks)
    {
        return Task.Run(() =>
        {
            if (recentTracks == null || !recentTracks.Any()) return;

            // 1. CALCULATE MILESTONES (Nearest 10k, 50k, etc.)
            long currentScrobbles = user.Playcount;
            long milestoneStep = currentScrobbles switch
            {
                < 1000 => 1000,
                < 10000 => 5000,
                < 50000 => 10000,
                _ => 25000
            };

            long nextMilestone = ((currentScrobbles / milestoneStep) + 1) * milestoneStep;
            long previousMilestone = nextMilestone - milestoneStep;
            long scrobblesLeft = nextMilestone - currentScrobbles;

            RxSchedulers.UI.ScheduleTo(() =>
            {
                LastFmMilestoneText = $"{scrobblesLeft:N0} scrobbles until {nextMilestone:N0}!";
                LastFmMilestoneProgress = (double)(currentScrobbles - previousMilestone) / milestoneStep;
            });

            // Filter out "Now Playing" tracks which have a null Date
            var validTracks = recentTracks.Where(t => t.Date.HasValue).ToList();
            if (!validTracks.Any()) return;

            // 2. CALCULATE TIME OF DAY HABITS (Morning, Afternoon, Evening, Night)
            int morning = 0, afternoon = 0, evening = 0, night = 0;

            foreach (var track in validTracks)
            {
                int hour = track.Date!.Value.ToLocalTime().Hour;
                if (hour >= 6 && hour < 12) morning++;
                else if (hour >= 12 && hour < 18) afternoon++;
                else if (hour >= 18 && hour < 24) evening++;
                else night++;
            }

            var timeStats = new Dictionary<string, int>
            {
                { "Morning", morning }, { "Afternoon", afternoon }, { "Evening", evening }, { "Night", night }
            };
            var topTime = timeStats.OrderByDescending(x => x.Value).First();

            // 3. CALCULATE WEEKLY ACTIVITY (Last 7 Days)
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            var dailyScrobbles = new double[7];
            int totalWeekly = 0;

            for (int i = 0; i < 7; i++)
            {
                var targetDate = last7Days[i];
                int count = validTracks.Count(t => t.Date!.Value.ToLocalTime().Date == targetDate);
                dailyScrobbles[i] = count;
                totalWeekly += count;
            }

            var dayLabels = last7Days.Select(d => d.ToString("ddd")).ToArray();

            // 4. BUILD LIVECHARTS DATA ON MAIN THREAD
            RxSchedulers.UI.ScheduleTo(() =>
            {
                FavoriteListeningTime = $"You are an {topTime.Key} listener.";
                WeeklyScrobbleCountText = $"+{totalWeekly} Scrobbles this week";

                // Weekly Bar Chart
                WeeklyScrobblesChart = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = dailyScrobbles,
                        Name = "Scrobbles",
                        Fill = new SolidColorPaint(new SKColor(123, 104, 238)), // DarkSlateBlue
                        //TooltipLabelFormatter = (chartPoint) => $"{chartPoint.PrimaryValue} Scrobbles",
                        Rx = 6, Ry = 6 // Rounded bars
                    }
                };

                ChartXAxes = new ICartesianAxis[]
                {
                    new Axis
                    {
                        Labels = dayLabels,
                        LabelsPaint = new SolidColorPaint(new SKColor(180, 180, 180)),
                        TextSize = 12
                    }
                };

                // Time of Day Doughnut Chart
                TimeOfDayChart = new ISeries[]
                {
                    new PieSeries<int> { Values = new[] { morning }, Name = "Morning", Fill = new SolidColorPaint(SKColors.Gold) },
                    new PieSeries<int> { Values = new[] { afternoon }, Name = "Afternoon", Fill = new SolidColorPaint(SKColors.DarkOrange) },
                    new PieSeries<int> { Values = new[] { evening }, Name = "Evening", Fill = new SolidColorPaint(SKColors.DarkSlateBlue) },
                    new PieSeries<int> { Values = new[] { night }, Name = "Night", Fill = new SolidColorPaint(SKColors.MidnightBlue) }
                };
            });
        });
    }

    #endregion
}
