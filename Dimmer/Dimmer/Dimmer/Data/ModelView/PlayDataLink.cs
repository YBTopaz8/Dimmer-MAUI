namespace Dimmer.Data.ModelView;

public partial class PlayDataLink : ObservableObject
{
    [ObservableProperty]
    public required partial string Id { get; set; }

    [ObservableProperty]
    public partial string? SongId { get; set; }
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
    /// <item><term>8</term><description>SeekRestarted</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty]
    public partial int PlayType { get; set; } = 0;

    [ObservableProperty]
    public partial DateTime DateStarted { get; set; }

    [ObservableProperty]
    public partial DateTime DateFinished { get; set; }

    [ObservableProperty]
    public partial bool WasPlayCompleted { get; set; }

    [ObservableProperty]
    public partial double PositionInSeconds { get; set; }
    [ObservableProperty]
    public partial DateTime EventDate { get; set; }


}