namespace Dimmer_MAUI.Utilities.Models;

public class PlaybackInfo
{
    public double CurrentPercentagePlayed { get; set; }
    public double CurrentTimeInSeconds { get; set; }
}



public partial class DimmData : ObservableObject
{
    [ObservableProperty]
    public partial string? TimeKey { get; set; }
    [ObservableProperty]
    public partial string? SongId { get; set; }
    [ObservableProperty]
    public partial double? DoubleKey { get; set; }
    [ObservableProperty]
    public partial DateTime Date { get; set; }
    [ObservableProperty]
    public partial double DimmCount { get; set; }
    [ObservableProperty]
    public partial string? PlayEventDescription { get; set; }
    [ObservableProperty]
    public partial int PlayEventCode { get; set; }

    [ObservableProperty]
    public partial int SeekCount { get; set; }
    [ObservableProperty]
    public partial string? PeakSessionStartDate { get; set; }
    [ObservableProperty]
    public partial int ConsecutivePlays { get; set; }

    [ObservableProperty]
    public partial int? Hour { get; set; }
    [ObservableProperty]
    public partial string? Month { get; set; }
    [ObservableProperty]
    public partial string? Year { get; set; }
    [ObservableProperty]
    public partial string? ArtistName { get; set; }
    [ObservableProperty]
    public partial string? AlbumName { get; set; }
    [ObservableProperty]
    public partial string? GenreName { get; set; }

    [ObservableProperty]
    public partial int WeekNumber { get; set; }
    [ObservableProperty]
    public partial string? SongTitle { get; set; }
    [ObservableProperty]
    public partial int RankChange { get; set; }
    [ObservableProperty]
    public partial int PreviousMonthDimms { get; set; }
    [ObservableProperty]
    public partial int CurrentMonthDimms { get; set; }
    [ObservableProperty]
    public partial int MaxStreak { get; set; }
    [ObservableProperty]
    public partial string? AverageEndedDate { get; set; }
    [ObservableProperty]
    public partial TimeSpan MaxGap { get; set; }
    [ObservableProperty]
    public partial TimeSpan OngoingGap { get; set; }
    [ObservableProperty]
    public partial int UniqueWeeks { get; set; }
    [ObservableProperty]
    public partial int StreakLength { get; set; }
    [ObservableProperty]
    public partial DateTime StartDate { get; set; }
    [ObservableProperty]
    public partial DateTime DateStarted { get; set; }
    [ObservableProperty]
    public partial DateTime DateFinished { get; set; }
    [ObservableProperty]
    public partial DateTime FirstDimmDate { get; set; }
    [ObservableProperty]
    public partial double TotalListeningHours { get; set; }
    [ObservableProperty]
    public partial double LifeTimeHours { get; set; }
    [ObservableProperty]
    public partial double DurationInSecond { get; set; }
    [ObservableProperty]
    public partial double SeekToEndRatio { get; set; }
    [ObservableProperty]
    public partial DateTime EndDate { get; set; }
    [ObservableProperty]
    public partial int RankLost { get; set; }
    /// <summary>
    /// Indicates the type of play action performed.    
    /// Possible VALID values for <see cref="PlayType"/>:
    /// <list type="bullet">
    /// <item><term>0</term><description>Play</description></item>
    /// <item><term>1</term><description>Pause</description></item>
    /// <item><term>2</term><description>Resume</description></item>
    /// <item><term>3</term><description>Completed</description></item>
    /// <item><term>4</term><description>Seeked</description></item>
    /// <item><term>5</term><description>Skipped</description></item>
    /// <item><term>6</term><description>Restarted</description></item>
    /// <item><term>7</term><description>SeekRestarted</description></item>
    /// <item><term>8</term><description>CustomRepeat</description></item>
    /// <item><term>9</term><description>Previous</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty]
    public partial int PlayType { get; set; }
    [ObservableProperty]
    public partial int IntValue { get; set; }
    [ObservableProperty]
    public partial double DoubleValue { get; set; }
    [ObservableProperty]
    public partial string? Description { get; set; }
    [ObservableProperty]
    public partial DayOfWeek DayOfWeekk { get; set; }
    public DimmData()
    {

        switch (PlayType)
        {
            case 0:
                PlayEventDescription= "Play";
                break;
            case 1:
                PlayEventDescription = "Pause";
                break;
            case 2:
                PlayEventDescription = "Resume";
                break;
            case 3:
                PlayEventDescription = "Completed";
                break;
            case 4:
                PlayEventDescription = "Seeked";
                break;
                case 5:
                PlayEventDescription = "Skipped";
                break;
            default:
                break;
        }
    }

    public DimmData(PlayDataLink linkk)
    {
        DateStarted = linkk.EventDate;
        DateFinished = linkk.EventDate;
        PlayEventCode = linkk.PlayType;
        SongId = linkk.SongId;

    }

}
public partial class PlaybackStats : ObservableObject
{
    [ObservableProperty]
    public partial string? SongId { get; set; }
    [ObservableProperty]
    public partial string? SongTitle { get; set; }
    [ObservableProperty]
    public partial string? SongArtist { get; set; }
    [ObservableProperty]
    public partial string? SongAlbum { get; set; }
    [ObservableProperty]
    public partial string? SongGenre { get; set; }
    [ObservableProperty]
    public partial int TotalCompletedPlays { get; set; } = 0;
    [ObservableProperty]
    public partial double TotalCompletedHours { get; set; } = 0.0;
    [ObservableProperty]
    public partial List<DateTime>? CompletedPlayTimes { get; set; }
    [ObservableProperty]
    public partial int TotalSkips { get; set; } = 0;
    [ObservableProperty]
    public partial List<DateTime>? SkipTimes { get; set; }
    [ObservableProperty]
    public partial int TotalPlays { get; set; } = 0;
    [ObservableProperty]
    public partial double TotalPlayHours { get; set; } = 0.0;
}
