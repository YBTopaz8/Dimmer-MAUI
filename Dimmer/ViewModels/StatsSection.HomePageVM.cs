namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> topTenPlayedSongs = new();

    [RelayCommand]
    void ShowGeneralTopXSongs()
    {
        // Get today's date
        var today = DateTime.Today;
        var lastWeek = today.AddDays(-7); // Get the date 7 days ago (last week)

        // Get the date 7 days ago
        //TopTenPlayedSongs = DisplayedSongs
        //.Select(s => new SingleSongStatistics
        //{
        //    Song = s,
        //    PlayCount = s.DatesPlayed.Count()  // Count all dates without a range
        //})
        //.OrderByDescending(s => s.PlayCount)
        //.ToObservableCollection();

        TopTenPlayedSongs = DisplayedSongs
            .Select(s => new SingleSongStatistics
            {
                Song = s,
                PlayCount = s.DatesPlayedAndWasPlayCompleted.Count()
                //PlayCount = s.DatesPlayed.Count(d => d.Date >= lastWeek && d.Date <= today),
            })
            .OrderByDescending(s => s.PlayCount)
            .Take(20)
            .ToObservableCollection();
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
        // Count only dates where the song was fully played (WasPlayCompleted is true)
        PlayCount = s.DatesPlayedAndWasPlayCompleted
            .Count(d => d.DatePlayed.Date == selectedDay.Value.Date && d.WasPlayCompleted == true)
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
    SingleSongStatistics songPickedForStats;
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
        if (song == null)
        {
            return;
        }

        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = song;

        if (song.DatesPlayedAndWasPlayCompleted != null && song.DatesPlayedAndWasPlayCompleted.Count > 0)
        {
            // Filter only fully played entries (where WasPlayCompleted is true)
            var mostPlayedDay = song.DatesPlayedAndWasPlayCompleted
                 // Only take entries with WasPlayCompleted == true                
                .GroupBy(entry => entry.DatePlayed.DayOfWeek)
                .OrderByDescending(group => group.Count())
                .FirstOrDefault();

            if (mostPlayedDay != null)
            {
                MostPlayedDay = mostPlayedDay.Key.ToString();
                // PlotPieSeries(song); // Assuming you want to plot something
            }
            else
            {
                MostPlayedDay = "Never Played Yet";
                IsChartVisible = false;
            }
        }
        else
        {
            IsChartVisible = false;
            MostPlayedDay = "Never Played Yet";
        }

        if (SongPickedForStats.Song.DatesPlayedAndWasPlayCompleted is not null)
        {
            // Count only fully played entries
            NumberOfTimesPlayed = SongPickedForStats.Song.DatesPlayedAndWasPlayCompleted
                .Count();
        }
        else
        {
            NumberOfTimesPlayed = 0;
        }

        return;
    }

    [ObservableProperty]
    ObservableCollection<DateTimeOffset> dialyWalkThrough;
    //private void PlotPieSeries(SongsModelView? song)
    //{
    //    IsChartVisible = true;
    //    var today = DateTime.Today;
    //    var lastWeek = today.AddDays(-6);
    //    int[] dayOfWeekCountsArray;
    //    List<string> dayNamesList;
    //    AllLoadingsBeforePlotting(song, today, lastWeek, out dayOfWeekCountsArray, out dayNamesList);

    //    int _index = 0;

    //    MyPieSeries = dayOfWeekCountsArray.AsPieSeries((value, series) =>
    //    {
    //        // Get the name of the day
    //        var dayName = dayNamesList[_index++];
    //        series.Name = dayName;
    //        var dayOfWeek = Enum.Parse<DayOfWeek>(dayName);
    //        series.DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle;
    //        series.Fill = new SolidColorPaint(GetColorForDay(dayOfWeek));

    //        series.DataLabelsSize = 14;
    //        series.DataLabelsPaint = new SolidColorPaint(SKColors.Black);
    //        series.DataLabelsFormatter = point =>
    //            series.Name + ": " + point.Coordinate.PrimaryValue + ((point.Coordinate.PrimaryValue > 1) ? " Plays" : " Play");
    //        series.ToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue}";

    //    });

    //    MyPieSeriesTitle = new LabelVisual
    //    {
    //        Text = $"From {lastWeek.ToShortDateString()} to {today.ToShortDateString()}",
    //        TextSize = 15,
    //        Padding = new LiveChartsCore.Drawing.Padding(15),
    //        Paint = new SolidColorPaint(SKColors.White)
    //    };
    //}

    private void AllLoadingsBeforePlotting(
      SongsModelView? song,
      DateTime today,
      DateTime lastWeek,
      out int[] dayOfWeekCountsArray,
      out List<string> dayNamesList)
    {
        today = DateTime.Today;
        lastWeek = today.AddDays(-7);

        // Filter by the last week and only take entries with `true` (fully played)
        var filteredDates = song.DatesPlayedAndWasPlayCompleted
            .Where(entry => entry.WasPlayCompleted == true && entry.DatePlayed.Date >= lastWeek && entry.DatePlayed.Date <= today)
            .Select(entry => entry.DatePlayed)
            .ToList();

        // Group by DayOfWeek and count
        var filteredDayCounts = filteredDates
            .GroupBy(date => date.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.Count());

        // Create a list of DatePlayCount objects
        var datePlayCounts = filteredDayCounts.Select(d => new DatePlayCount
        {
            DatePlayed = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(d.Key),
            Count = d.Value
        }).ToList();

        SongDatePlayCounts = new ObservableCollection<DatePlayCount>(datePlayCounts);

        // Fill the output arrays for day of week counts and day names
        dayOfWeekCountsArray = filteredDayCounts
            .Select(kvp => kvp.Value)
            .ToArray();

        dayNamesList = filteredDayCounts
            .Select(kvp => kvp.Key.ToString())
            .ToList();

        // Total number of times the song was played during the week
        NumberOfTimesPlayed = filteredDayCounts.Values.Sum();

        // Find the most played day
        var mostPlayedDay = filteredDayCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        if (mostPlayedDay.Key != null)
        {
            MostPlayedDay = mostPlayedDay.Key.ToString();
        }
        else
        {
            MostPlayedDay = "None";
        }
    }



    private void PlotLineSeries(SongsModelView? song)
    {
        var today = DateTime.Today;
        var lastWeek = today.AddDays(-6);
        int[] dayOfWeekCountsArray;
        List<string> dayNamesList;
        AllLoadingsBeforePlotting(song, today, lastWeek, out dayOfWeekCountsArray, out dayNamesList);

        return;
        //var lines = new LineSeries<int>
        //{
        //    Values = dayOfWeekCountsArray,
        //    YToolTipLabelFormatter = (linePoint) =>
        //    {
        //        return $"Played {linePoint.Coordinate.PrimaryValue} times";
        //    },
        //    Stroke = new SolidColorPaint(SKColors.DarkSlateBlue) { StrokeThickness = 2 },

        //    Fill = null
        //};

        //MySeries = new ISeries[]
        //{
        //lines
        //};

        //var orderedDaysOfWeek = new List<DayOfWeek>
        //{
        //    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        //    DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        //};

        //XAxes =
        //[
        //new Axis
        //{
        //    Labels = orderedDaysOfWeek.Select(day => day.ToString()).ToArray()
        //}
        //];

        //MyPieSeriesTitle = new LabelVisual
        //{
        //    Text = $"{song.Title} Play count from {lastWeek.ToShortDateString()} to {today.ToShortDateString()}",
        //    TextSize = 25,
        //    Padding = new LiveChartsCore.Drawing.Padding(15),
        //    Paint = new SolidColorPaint(SKColors.White)
        //};
    }

    [RelayCommand]
    async Task NavigateToSingleSongStatsPage(SongsModelView song)
    {
        SongPickedForStats.Song = song;
#if ANDROID
        //await Shell.Current.GoToAsync(nameof(SingleSongStatsPageM));
#elif WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageD));
#endif
    }

    [ObservableProperty]
    ObservableCollection<DatePlayCount> songDatePlayCounts;

    //    [ObservableProperty]
    //    ISeries[] mySeries;
    //    [ObservableProperty]
    //    IEnumerable<ISeries> myPieSeries;
    //    [ObservableProperty]
    //    Axis[] xAxes =
    //    { new Axis
    //        {
    //            Name = "Days of the Week",
    //            Labels = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" },
    //            MinLimit = 0,  // Start at 0 plays
    //        }
    //    };

    //    [ObservableProperty]
    //    Axis[] yAxes =
    //    {
    //        new Axis
    //        {
    //            CrosshairSnapEnabled = true,
    //            Name = "Times Played",
    //            //Labeler = value => value.ToString("N"),

    //             MinStep = 1,
    //             MinLimit = 0,
    //            LabelsPaint = new SolidColorPaint(SKColors.White, 3),
    //        }
    //    };
    //    private SKColor GetColorForDay(DayOfWeek dayOfWeek)
    //    {
    //        return dayOfWeek switch
    //        {
    //            DayOfWeek.Monday => SKColor.Parse("#AEDFF7"),    // Soft Light Blue
    //            DayOfWeek.Tuesday => SKColor.Parse("#A8E6CF"),   // Soft Mint Green
    //            DayOfWeek.Wednesday => SKColor.Parse("#D3E4CD"), // Soft Pale Green
    //            DayOfWeek.Thursday => SKColor.Parse("#FFD3B6"),  // Soft Peach
    //            DayOfWeek.Friday => SKColor.Parse("#FFAAA5"),    // Soft Coral
    //            DayOfWeek.Saturday => SKColor.Parse("#FF8B94"),  // Soft Pink
    //            DayOfWeek.Sunday => SKColor.Parse("#B5EAD7"),    // Soft Teal
    //            _ => SKColors.Gray // Default color
    //        };
    //    }
    //    [ObservableProperty]
    //    LabelVisual myPieSeriesTitle;


}

public partial class SingleSongStatistics : ObservableObject
{
    [ObservableProperty]
    SongsModelView? song;
    [ObservableProperty]
    int playCount;
}
public partial class DatePlayCount : ObservableObject
{
    [ObservableProperty]
    string? datePlayed;
    [ObservableProperty]
    int count;
}