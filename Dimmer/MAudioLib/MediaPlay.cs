namespace Dimmer_MAUI.MAudioLib;
public class MediaPlay
{
    public string SongId {  get; set; }=string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string URL { get; set; } = string.Empty;
    public Stream? Stream { get; set; }
    public string ImagePath { get; set; }
    /// <summary>
    /// Get/Set Album Cover in Byte[] to put on Notification 
    /// </summary>
    public byte[]? ImageBytes { get; set; }
    /// <summary>
    /// Get/Set the VALUE of the Duration, solely used for the Notification bar to show Progress.
    /// </summary>
    public long DurationInMs { get; set; } = 0;
}