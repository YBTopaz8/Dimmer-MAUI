namespace Dimmer.Data.Models;

public class FileProcessingResult
{
    public SongModel? ProcessedSong { get; set; }
    public List<string> Errors { get; } = new List<string>();
    public bool Skipped { get; set; }
    public string SkipReason { get; set; } = string.Empty;
    public bool Success => ProcessedSong != null && Errors.Count == 0 && !Skipped;
}