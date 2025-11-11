namespace Dimmer.Data.ModelView;

public class AudioOutputDevice
{

    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? ProductName { get; set; }
    public bool? IsSource { get; set; }


    public string? Name { get; set; }
    public bool IsDefaultDevice { get; set; }
    public bool IsDefaultCommunicationsDevice { get; set; }
    public bool IsMuted { get; set; }
    public string? State { get; set; }
    public string? IconString { get; set; }
    public bool IsPlaybackDevice { get; set; }
    public double Volume { get; set; }
}