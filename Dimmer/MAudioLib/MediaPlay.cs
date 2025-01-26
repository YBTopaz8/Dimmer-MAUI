namespace Dimmer_MAUI.MAudioLib;
public class MediaPlay
{
    public string SongId {  get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string URL { get; set; }
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


public enum EqualizerPresetName
{
    Flat,
    Rock,
    Pop,
    Classical,
    Jazz,
    Dance,
    BassBoost,
    TrebleBoost
}
