namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> topTenPlayedSongs = new();

    [RelayCommand]
    void ShowGeneralTopXSongs()
    {
        Debug.WriteLine("Calledtrrt");
        // Get today's date
        var today = DateTime.Today;
        // Get the date 7 days ago
        TopTenPlayedSongs = DisplayedSongs
    .Select(s => new SingleSongStatistics
    {
        Song = s,
        PlayCount = s.DatesPlayed.Count()  // Count all dates without a range
    })
    .OrderByDescending(s => s.PlayCount)
    
    .ToObservableCollection();

        //TopTenPlayedSongs = DisplayedSongs
        //    .Select(s => new SingleSongStatistics
        //    {
        //        Song = s,
        //        PlayCount = s.DatesPlayed.Count(d => d.Date >= lastWeek && d.Date <= today),
        //    })
        //    .OrderByDescending(s => s.PlayCount)
        //    .Take(20)
        //    .ToObservableCollection();
        if (IsPlaying && CurrentQueue != 2)
        {
            ShowSingleSongStats(TemporarilyPickedSong);
        }
        else
        {
            ShowSingleSongStats(TopTenPlayedSongs.FirstOrDefault()?.Song);
        }
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
    [ObservableProperty]
    bool isChartVisible;
    [RelayCommand]
    void ShowSingleSongStats(SongsModelView? song)
    {
        IsChartVisible = false;
        MyPieSeries = null;
        MyPieSeriesTitle = null;
        if (song == null)
        {
            return;
        }

        SongPickedForStats = song;
        
        if (song.DatesPlayed != null && song.DatesPlayed.Count > 0)
        {

            var mostPlayedDay = song.DatesPlayed
                .GroupBy(d => d.DayOfWeek)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();


            MostPlayedDay = mostPlayedDay.Key.ToString();
            PlotPieSeries(song);
        }
        else
        {
            IsChartVisible = false;

            MostPlayedDay = "Never Played Yet";
        }
        if (SongPickedForStats.DatesPlayed is not null)
        {
            NumberOfTimesPlayed = SongPickedForStats.DatesPlayed.Count;
        }
        return;
        PlotLineSeries(song);
    }
    [ObservableProperty]
    ObservableCollection<DateTimeOffset> dialyWalkThrough;
    private void PlotPieSeries(SongsModelView? song)
    {
        IsChartVisible = true;
        var today = DateTime.Today;
        var lastWeek = today.AddDays(-6);
        int[] dayOfWeekCountsArray;
        List<string> dayNamesList;
        AllLoadingsBeforePlotting(song, today, lastWeek, out dayOfWeekCountsArray, out dayNamesList);

        int _index = 0;

        MyPieSeries = dayOfWeekCountsArray.AsPieSeries((value, series) =>
        {
            // Get the name of the day
            var dayName = dayNamesList[_index++];
            series.Name = dayName;
            var dayOfWeek = Enum.Parse<DayOfWeek>(dayName);
            series.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle;
            series.Fill = new SolidColorPaint(GetColorForDay(dayOfWeek));

            series.DataLabelsSize = 14;
            series.DataLabelsPaint = new SolidColorPaint(SKColors.Black);
            series.DataLabelsFormatter = point =>
                series.Name + ": " + point.Coordinate.PrimaryValue + ((point.Coordinate.PrimaryValue > 1) ? " Plays" : " Play");
            series.ToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue}";

        });

        MyPieSeriesTitle = new LabelVisual
        {
            Text = $"From {lastWeek.ToShortDateString()} to {today.ToShortDateString()}",
            TextSize = 15,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.White)
        };
    }

    private void AllLoadingsBeforePlotting(
    SongsModelView? song,
    DateTime today,
    DateTime lastWeek,
    out int[] dayOfWeekCountsArray,
    out List<string> dayNamesList)
    {
        today = DateTime.Today;
        lastWeek = today.AddDays(-7);

        var filteredDates = song.DatesPlayed
            .Where(date => date.Date >= lastWeek && date.Date <= today)
            .ToList();

        var filteredDayCounts = filteredDates
            .GroupBy(date => date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        var datePlayCounts = filteredDayCounts.Select(d => new DatePlayCount
        {
            DatePlayed = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(d.Key),
            Count = d.Value
        }).ToList();

        SongDatePlayCounts = new ObservableCollection<DatePlayCount>(datePlayCounts);

        dayOfWeekCountsArray = filteredDayCounts
            .Select(kvp => kvp.Value)
            .ToArray();

        dayNamesList = filteredDayCounts
            .Select(kvp => kvp.Key.ToString())
            .ToList();

        NumberOfTimesPlayed = filteredDayCounts.Values.Sum();
        var mostPlayedDay = filteredDayCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        MostPlayedDay = mostPlayedDay.Key.ToString();
    }

    private void PlotLineSeries(SongsModelView? song)
    {
        var today = DateTime.Today;
        var lastWeek = today.AddDays(-6);
        int[] dayOfWeekCountsArray;
        List<string> dayNamesList;
        AllLoadingsBeforePlotting(song, today, lastWeek, out dayOfWeekCountsArray, out dayNamesList);

        return;
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

        MyPieSeriesTitle = new LabelVisual
        {
            Text = $"{song.Title} Play count from {lastWeek.ToShortDateString()} to {today.ToShortDateString()}",
            TextSize = 25,
            Padding = new LiveChartsCore.Drawing.Padding(15),
            Paint = new SolidColorPaint(SKColors.White)
        };
    }

    [RelayCommand]
    async Task NavigateToSingleSongStatsPage(SongsModelView song)
    {
        SongPickedForStats = song;
#if ANDROID
        //await Shell.Current.GoToAsync(nameof(SingleSongStatsPageM));
#elif WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageD));
#endif
    }

    [ObservableProperty]
    ISeries[] mySeries;
    [ObservableProperty]
    IEnumerable<ISeries> myPieSeries;
    [ObservableProperty]
    ObservableCollection<DatePlayCount> songDatePlayCounts;
    [ObservableProperty]
    Axis[] xAxes =
    { new Axis
        {
            Name = "Days of the Week",
            Labels = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
            MinLimit = 0,  // Start at 0 plays
        }
    };

    [ObservableProperty]
    Axis[] yAxes =
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
    private SKColor GetColorForDay(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => SKColor.Parse("#AEDFF7"),    // Soft Light Blue
            DayOfWeek.Tuesday => SKColor.Parse("#A8E6CF"),   // Soft Mint Green
            DayOfWeek.Wednesday => SKColor.Parse("#D3E4CD"), // Soft Pale Green
            DayOfWeek.Thursday => SKColor.Parse("#FFD3B6"),  // Soft Peach
            DayOfWeek.Friday => SKColor.Parse("#FFAAA5"),    // Soft Coral
            DayOfWeek.Saturday => SKColor.Parse("#FF8B94"),  // Soft Pink
            DayOfWeek.Sunday => SKColor.Parse("#B5EAD7"),    // Soft Teal
            _ => SKColors.Gray // Default color
        };
    }
    [ObservableProperty]
    LabelVisual myPieSeriesTitle;


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
