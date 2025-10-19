
namespace Dimmer.Data.Models;

public class FileProcessingResult
{
    public string FilePath { get; }
    public SongModelView? ProcessedSong { get; set; }
    public List<string> Errors { get; } = new List<string>();
    public bool Skipped { get; set; }
    public string SkipReason { get; set; } = string.Empty;
    public bool Success => ProcessedSong != null && Errors.Count == 0 && !Skipped;
    public FileProcessingResult(string filePath)
    {
        FilePath = filePath;
    }
}