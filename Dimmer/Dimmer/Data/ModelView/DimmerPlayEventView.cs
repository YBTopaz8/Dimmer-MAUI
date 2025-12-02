namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public class DimmerPlayEventView
{
    public ObjectId Id { get; set; } // Or public string Id if you prefer for display
    public bool IsNewOrModified { get; set; } // If needed by UI
    public string? SongName { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string? CoverImagePath { get; set; }
    public bool IsFav { get; set; }

    public ObjectId? SongId { get; set; } 
    /// <summary>
                                           /// Indicates the type of play action performed.
                                           /// Possible VALID values for <see cref="PlayType" />:
                                           /// <list type="bullet"><item>
                                           /// <term>0</term><description>Play</description></item><item><term>1</term><description>Pause</description></item><item><term>2</term><description>Resume</description></item><item><term>3</term><description>Completed</description></item><item><term>4</term><description>Seeked</description></item><item><term>5</term><description>Skipped</description></item><item><term>6</term><description>Restarted</description></item><item><term>7</term><description>SeekRestarted</description></item><item><term>8</term><description>CustomRepeat</description></item><item><term>9</term><description>Previous</description></item></list>
                                           /// </summary>
                                           /// <value>
                                           /// The type of the play.
                                           /// </value>
    public int PlayType { get; set; }
    public string? PlayTypeStr { get; set; }
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public bool WasPlayCompleted { get; set; }
    public double PositionInSeconds { get; set; }
    public DateTimeOffset? EventDate { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    public DimmerPlayEventView()
    {
        
    }
    public DimmerPlayEventView(DimmerPlayEvent dbEvt)
    {
        Id = dbEvt.Id;
        SongName = dbEvt.SongName;
        SongId = dbEvt.SongId;
        PlayType = dbEvt.PlayType;
        PlayTypeStr = dbEvt.PlayTypeStr;
        DatePlayed = dbEvt.DatePlayed;
        DateFinished = dbEvt.DateFinished;
        WasPlayCompleted = dbEvt.WasPlayCompleted;
        PositionInSeconds = dbEvt.PositionInSeconds;
        EventDate = dbEvt.EventDate;
        DeviceName = dbEvt.DeviceName;
        DeviceFormFactor = dbEvt.DeviceFormFactor;
        DeviceModel = dbEvt.DeviceModel;
        DeviceManufacturer = dbEvt.DeviceManufacturer;
        DeviceVersion = dbEvt.DeviceVersion;
        
    }

}
public partial class PlayEventGroup : ObservableCollection<DimmerPlayEventView>
{
    public string Name { get; private set; }

    public PlayEventGroup(string name, IEnumerable<DimmerPlayEventView> events) : base(events)
    {
        Name = name;
    }

    public override string ToString() => Name;
}