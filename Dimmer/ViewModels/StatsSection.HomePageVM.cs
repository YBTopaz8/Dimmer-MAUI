using LiveChartsCore;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> topTenPlayedSongs = new();

    [RelayCommand]
    void ShowGeneralTopTenSongs()
    {
        // Get today's date
        var today = DateTime.Today;
        // Get the date 7 days ago
        var lastWeek = today.AddDays(-6);
        TopTenPlayedSongs = SongsMgtService.AllSongs
            .Select(s => new SingleSongStatistics
            {
                Song = s,
                PlayCount = s.DatesPlayed.Count(d => d.Date >= lastWeek && d.Date <= today),
            })
            .OrderByDescending(s => s.PlayCount)
            .Take(10)
            .ToObservableCollection();
        ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);
    }

    [RelayCommand]
    void ShowTopTenSongsForSpecificDay(DateTime? selectedDay)
    {
        if (selectedDay == null)
        {
            return;
        }
        TopTenPlayedSongs = SongsMgtService.AllSongs
        .Select(s => new SingleSongStatistics
        {
            Song = s,
            PlayCount = s.DatesPlayed.Count(d => d.Date == selectedDay.Value.Date), // Filter by specific date
        })
        .OrderByDescending(s => s.PlayCount)
        .Take(10)
        .ToObservableCollection();
        ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);

    }

    Dictionary<string, int> dayCounts = new Dictionary<string, int>
    {
        { "Monday", 0 },
        { "Tuesday", 0 },
        { "Wednesday", 0 },
        { "Thursday", 0 },
        { "Friday", 0 },
        { "Saturday", 0 },
        { "Sunday", 0 }
    };

    [ObservableProperty]
    SongsModelView songPickedForStats;
    [ObservableProperty]
    int numberOfTimesPlayed;
    [ObservableProperty]
    string? mostPlayedDay;
    [RelayCommand]
    void ShowSingleSongStats(SongsModelView? song)
    {
        if (song == null)
        {
            return;
        }

        SongPickedForStats = song;

        if (song.DatesPlayed == null || song.DatesPlayed.Count < 1)
        {
            song = TemporarilyPickedSong;
        }

        var today = DateTime.Today;
        var lastWeek = today.AddDays(-6);

        var filteredDates = song.DatesPlayed
            .Where(date => date.Date >= lastWeek && date.Date <= today)
            .ToList();

        var dateCounts = new Dictionary<DateTime, int>();
        for (var date = lastWeek; date <= today; date = date.AddDays(1))
        {
            dateCounts[date] = 0;
        }

        foreach (var date in filteredDates)
        {
            dateCounts[date.Date]++;
        }

        var datePlayCounts = dateCounts.Select(d => new DatePlayCount
        {
            DatePlayed = d.Key.ToString("dddd"), 
            Count = d.Value
        }).ToList();

        NumberOfTimesPlayed = datePlayCounts.Sum(d => d.Count);

        SongDatePlayCounts = new ObservableCollection<DatePlayCount>(datePlayCounts);


        DayOfWeek? mostPlayedDay = null;
        int maxPlays = -1;

        var dayOfWeekCountsArray = Enum.GetValues(typeof(DayOfWeek))
            .Cast<DayOfWeek>() 
            .Where(day => day != DayOfWeek.Sunday) 
            .Concat(new[] { DayOfWeek.Sunday }) 
            .Select(day =>
            {
                var count = dateCounts.Where(d => d.Key.DayOfWeek == day).Sum(d => d.Value);

                if (count > maxPlays)
                {
                    maxPlays = count;
                    mostPlayedDay = day;
                }

                return count;
            })
            .ToArray(); 

        MostPlayedDay = mostPlayedDay?.ToString();


        var lines = new LineSeries<int>
        {
            Values = dayOfWeekCountsArray,
            YToolTipLabelFormatter = (linePoint) =>
            {
                return $"Played {linePoint.Coordinate.PrimaryValue} times";
            },
            Stroke = new SolidColorPaint(SKColors.DarkSlateBlue) { StrokeThickness = 2 },

            Fill = null
        };
        
        MySeries = new ISeries[]
        {
        lines
        };

        var orderedDaysOfWeek = new List<DayOfWeek>
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        XAxes =
        [
        new Axis
        {
            Labels = orderedDaysOfWeek.Select(day => day.ToString()).ToArray()
        }
        ];

        Title = new LabelVisual
        {
            Text = $"{song.Title} Play count from {lastWeek.ToShortDateString()} to {today.ToShortDateString()}",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.White)
        };
    }



    private SKColor GetColorForDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => SKColors.Red,
            DayOfWeek.Tuesday => SKColors.Blue,
            DayOfWeek.Wednesday => SKColors.Green,
            DayOfWeek.Thursday => SKColors.Yellow,
            DayOfWeek.Friday => SKColors.Purple,
            DayOfWeek.Saturday => SKColors.Orange,
            DayOfWeek.Sunday => SKColors.Cyan,
            _ => SKColors.Gray // Default color
        };
    }
    [RelayCommand]
    async Task NavigateToSingleSongStatsPage(SongsModelView song)
    {
        SongPickedForStats = song;
#if ANDROID
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageM));
#elif WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageD));
#endif
    }

    [ObservableProperty]
    public ISeries[] mySeries;
    [ObservableProperty]
    public ObservableCollection<DatePlayCount> songDatePlayCounts;
    [ObservableProperty]
    public Axis[] xAxes =
    { new Axis
        {
            Name = "Days of the Week",
            Labels = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
            MinLimit = 0,  // Start at 0 plays
        }
    };

    [ObservableProperty]
    public Axis[] yAxes =
    {
        new Axis
        {
            CrosshairSnapEnabled = true,
            Name = "Times Played",
            //Labeler = value => value.ToString("N"),
             
             MinStep = 1,
             MinLimit = 0,
            LabelsPaint = new SolidColorPaint(SKColors.White, 3),
        }
    };

    [ObservableProperty]
    public LabelVisual title;


}

public class SingleSongStatistics
{
    public SongsModelView Song { get; set; }
    public int PlayCount { get; set; }
}
public class DatePlayCount
{
    public string DatePlayed { get; set; }
    public int Count { get; set; }
}
