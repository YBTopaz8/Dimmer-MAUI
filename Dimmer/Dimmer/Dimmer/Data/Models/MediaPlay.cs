namespace Dimmer.Data.Models;
public class MediaPlay
{
    public string? SongId { get; set; }
    public required string Name { get; set; }
    public string? Author { get; set; }
    public required string URL { get; set; }
    public Stream? Stream { get; set; }
    public string? ImagePath { get; set; }
    
    public byte[]? ImageBytes { get; set; }

    public long DurationInMs { get; set; }
}
