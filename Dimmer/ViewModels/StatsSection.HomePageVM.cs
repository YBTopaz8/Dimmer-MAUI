namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    [ObservableProperty]
    public partial TimeSpan SelectedTimeSpanForStats { get; private set; }= TimeSpan.FromDays(7);

    
    [ObservableProperty]
    public partial ObservableCollection<SingleSongStatistics>? TopTenPlayedSongs { get; set; } = new();

    [RelayCommand]
    void ShowGeneralTopXSongs()
    {

        TopTenPlayedSongs = SongsMgtService.AllSongs
            .Select(s => new SingleSongStatistics
            {
                Song = s,
                //PlayCount = s.DatesPlayedAndWasPlayCompleted.Count()
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
    void ShowTopTenSongsForSpecificDay(DateTimeOffset? selectedDay)
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
        //PlayCount = s.DatesPlayedAndWasPlayCompleted
        //    .Count(d => d.DatePlayed.Date == selectedDay.Value.Date && d.WasPlayCompleted == true)
    })
    .OrderByDescending(s => s.PlayCount)
    .Take(15)
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
    public partial SingleSongStatistics SongPickedForStats { get; set; }
    [ObservableProperty]
    int numberOfTimesPlayed;
    [ObservableProperty]
    string? mostPlayedDay;
    [ObservableProperty]
    bool isChartVisible;
    [RelayCommand]
    void ShowSingleSongStats(SongModelView? song)
    {
        IsChartVisible = false;
        if (song == null)
            return;

        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = song;

        LoadDailyStats(song);
        
        UpdateMostPlayedDay(song);
        UpdateNumberOfTimesPlayed(song);

    }

    public void LoadWeeklyStats(SongModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        if (song is null)
        {
            return;
        }
        startDate ??= DateTimeOffset.UtcNow.Date.AddMonths(-6).Date; // Default to 6 months ago for a longer range
        endDate ??= DateTimeOffset.UtcNow.Date.Date; // Default to today

        SongPickedForStats.WeeklyStats = [];

        // Ensure start date is aligned to the beginning of the week (e.g., Monday)
        DateTimeOffset currentStartDate = startDate.Value;
        if (currentStartDate.DayOfWeek != DayOfWeek.Monday)
        {
            int daysToMonday = ((int)DayOfWeek.Monday - (int)currentStartDate.DayOfWeek + 7) % 7;
            currentStartDate = currentStartDate.AddDays(daysToMonday);
        }

        // Loop through each week in the specified range
        while (currentStartDate <= endDate.Value)
        {
            DateTimeOffset weekEndDate = currentStartDate.AddDays(6);

            // Adjust the week end date if it exceeds the end date
            if (weekEndDate > endDate.Value)
            {
                weekEndDate = endDate.Value;
            }
            WeeklyStats weekStat = new WeeklyStats(song, currentStartDate, weekEndDate);
            if (weekStat.Count>0)
            {
                SongPickedForStats.WeeklyStats.Add(weekStat);
            }
            currentStartDate = currentStartDate.AddDays(7);
        }
    }



    public void LoadDailyStats(SongModelView song, DateTimeOffset? specificDate = null)
    {
        if (song is null)
            return;
        specificDate ??= DateTimeOffset.UtcNow.Date.Date;

        SongPickedForStats.DailyStats = new DailyStats(song, specificDate.Value.Date);

        UpdateMostPlayedDay(song);
        UpdateNumberOfTimesPlayed(song);
    }

    public void LoadMonthlyStats(SongModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {

        if (song is null)
            return;
        startDate ??= new DateTimeOffset(DateTimeOffset.UtcNow.Date.Year, DateTimeOffset.UtcNow.Date.Month, 1, 0, 0, 0, TimeSpan.Zero);
        endDate ??= new DateTimeOffset(DateTimeOffset.UtcNow.Date.Year, DateTimeOffset.UtcNow.Date.Month, 1, 0, 0, 0, TimeSpan.Zero)
                        .AddMonths(1).AddDays(-1); // End of the current month

        SongPickedForStats.MonthlyStats = [];


        DateTimeOffset currentMonth = new DateTimeOffset(startDate.Value.Year, startDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastMonth = new DateTimeOffset(endDate.Value.Year, endDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero);

        while (currentMonth <= lastMonth)
        {

            DateTimeOffset monthEndDate = currentMonth.AddMonths(1).AddDays(-1);

            
            SongPickedForStats.MonthlyStats.Add(new MonthlyStats(song, currentMonth, monthEndDate));

            
            currentMonth = currentMonth.AddMonths(1);
        }
    }


    public void LoadYearlyStats(SongModelView song, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {

        if (song is null)
            return;
        startDate ??= new DateTimeOffset(DateTimeOffset.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        endDate ??= new DateTimeOffset(DateTimeOffset.Now.Year, 12, 31, 23, 59, 59, TimeSpan.Zero);

        SongPickedForStats.YearlyStats = [];


        DateTimeOffset currentYear = new DateTimeOffset(startDate.Value.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset lastYear = new DateTimeOffset(endDate.Value.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);

        while (currentYear <= lastYear)
        {

            DateTimeOffset yearEndDate = new DateTimeOffset(currentYear.Year, 12, 31, 23, 59, 59, TimeSpan.Zero);

            // Create and add each yearly stat entry
            SongPickedForStats.YearlyStats.Add(new YearlyStats(song, currentYear, yearEndDate));

            // Move to the start of the next year
            currentYear = currentYear.AddYears(1);
        }
    }

    private void UpdateMostPlayedDay(SongModelView song)
    {
        if (song is null)
            return;
        //var mostPlayedDay = song.DatesPlayedAndWasPlayCompleted?
        //    .GroupBy(entry => entry.DatePlayed.DayOfWeek)
        //    .OrderByDescending(group => group.Count())
        //    .FirstOrDefault();

        //MostPlayedDay = mostPlayedDay?.Key.ToString() ?? "Never Played Yet";
    }

    private void UpdateNumberOfTimesPlayed(SongModelView song)
    {
        if (song is null)
            return;
        //NumberOfTimesPlayed = song.DatesPlayedAndWasPlayCompleted?
        //    .Count() ?? 0;
    }

    [ObservableProperty]
    int currentNowPlayingStatsViewIndex=0;
    void RefreshStatView()
    {

        UpdateMostPlayedDay(SongPickedForStats.Song!);
        UpdateNumberOfTimesPlayed(SongPickedForStats.Song);

        switch (CurrentNowPlayingStatsViewIndex)
        {
            case 0:
                LoadDailyStats(MySelectedSong!);
                break;
            case 1:
                LoadWeeklyStats(MySelectedSong!);
                break;
            case 2:
                LoadMonthlyStats(MySelectedSong!);
                break;
            case 3:
                LoadYearlyStats(MySelectedSong!);
                break;
            default:
                break;
        }
    }





    

    [RelayCommand]
    async Task NavigateToSingleSongStatsPage(SongModelView song)
    {
        SongPickedForStats.Song = song;
#if ANDROID
        //await Shell.Current.GoToAsync(nameof(SingleSongStatsPageM));
#elif WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongStatsPageD));
#endif
    }
    [ObservableProperty]
    ObservableCollection<string> statsFilters = ["Daily","Weekly", "Monthly", "Yearly"];

}

public partial class SingleSongStatistics : ObservableObject
{
    [ObservableProperty]
    SongModelView? song;
    [ObservableProperty]
    ObservableCollection<WeeklyStats> weeklyStats;
    [ObservableProperty]
    DailyStats dailyStats;
    [ObservableProperty]
    ObservableCollection<MonthlyStats> monthlyStats;
    [ObservableProperty]
    ObservableCollection<YearlyStats> yearlyStats;
    [ObservableProperty]
    int playCount;
    [ObservableProperty]
    DateTimeOffset playDateTime;
}

public partial class YearlyStats : ObservableObject
{
    [ObservableProperty]
    MonthlyStats? monthlies;

    [ObservableProperty]
    string month;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public YearlyStats(SongModelView model, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {

        startDate ??= DateTimeOffset.UtcNow.Date.Date;
        endDate ??= DateTimeOffset.UtcNow.Date.Date;
        //var yearlyPlays = model.DatesPlayedAndWasPlayCompleted?
        //    .Where(entry => entry.DatePlayed.Date >= startDate && entry.DatePlayed.Date <= endDate)
        //    .ToList() ?? new List<PlayDateAndCompletionStateSongLinkView>();

        //Month = startDate.Value.Year.ToString();
        //Count = yearlyPlays.Count;
        TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
    }

}


public partial class MonthlyStats : ObservableObject
{
    [ObservableProperty]
    WeeklyStats? weeklies;

    [ObservableProperty]
    string month;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public MonthlyStats(SongModelView model, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        startDate ??= DateTimeOffset.UtcNow.Date.Date;
        //endDate ??= DateTimeOffset.UtcNow.Date.Date;
        //{
        //    var monthlyPlays = model.DatesPlayedAndWasPlayCompleted?
        //        .Where(entry => entry.DatePlayed.Date >= startDate.Value.Date && entry.DatePlayed.Date <= endDate.Value.Date)
        //        .ToList() ?? new List<PlayDateAndCompletionStateSongLinkView>();

        //    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startDate.Value.Month);
        //    Count = monthlyPlays.Count;
        //    TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
        //}
    }

}
public partial class DailyStats : ObservableObject
{
    [ObservableProperty]
    List<PlayDataLink>? playDates;

    [ObservableProperty]
    double totalPlayTime;

    [ObservableProperty]
    ObservableCollection<DataForChart> colforStats;

    public DailyStats(SongModelView model, DateTimeOffset? specificDate = null)
    {
        return;
        
        specificDate ??= DateTimeOffset.UtcNow.Date.Date;

        //// Filter play dates for the specific date
        //PlayDates = new ObservableCollection<PlayDateAndCompletionStateSongLinkView>(
        //    model.DatesPlayedAndWasPlayCompleted?
        //        .Where(entry => entry.DatePlayed.Date == specificDate.Value.Date) ??
        //        Enumerable.Empty<PlayDateAndCompletionStateSongLinkView>()
        //);

        // Calculate total play time in minutes
        //TotalPlayTime = PlayDates.Count * model.DurationInSeconds / 60;

        // Initialize ColforStats collection
        ColforStats = [];

        // Populate ColforStats initially
        RefreshColforStats();
    }

    public void RefreshColforStats()
    {
        // Clear any existing items
        ColforStats.Clear();

        // Count completed and incomplete plays
        int completedCount = PlayDates.Count(entry => entry.WasPlayCompleted);
        int incompleteCount = PlayDates.Count(entry => !entry.WasPlayCompleted);
        // Add data for completed plays
        ColforStats.Add(new DataForChart
        {
            LabelForNumberOfCompletedPlays = $"Completed Plays",
            TheCount = completedCount
        });

        // Add data for incomplete plays
        ColforStats.Add(new DataForChart
        {
            LabelForNumberOfCompletedPlays = $"Incomplete Plays",
            TheCount = incompleteCount
        });
        
    }

    
}

public class DataForChart
{
    public string? LabelForNumberOfCompletedPlays { get; set; } // Label, e.g., "Completed Plays" or "Incomplete Plays"
    public int TheCount { get; set; }                          // Count of plays
}
public partial class WeeklyStats : ObservableObject
{
    [ObservableProperty]
    DateTimeOffset? datePlayed;

    [ObservableProperty]
    string week;

    [ObservableProperty]
    int count;

    [ObservableProperty]
    double totalPlayTime;

    public WeeklyStats(SongModelView model, DateTimeOffset? startDate=null, DateTimeOffset? endDate = null)
    {
        startDate ??= DateTimeOffset.UtcNow.Date.Date;
        endDate ??= DateTimeOffset.UtcNow.Date.Date.AddDays(1);
        //var weeklyPlays = model.DatesPlayedAndWasPlayCompleted?
            //.Where(entry => entry.DatePlayed.Date >= startDate.Value.Date && entry.DatePlayed.Date <= endDate.Value.Date)
            //.ToList() ?? new List<PlayDateAndCompletionStateSongLinkView>();

        DatePlayed = startDate;
        Week = $"Week of {startDate:MMMM dd}";
        //Count = weeklyPlays.Count;
        TotalPlayTime = Count * model.DurationInSeconds / 60; // Convert seconds to minutes
    }

}


