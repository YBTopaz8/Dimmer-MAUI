namespace Dimmer.Data.Models;
public class LoadSongsResult
{
    public List<ArtistModel> Artists { get; set; }
    public List<AlbumModel> Albums { get; set; }
    public List<SongModel> Songs { get; set; }
    public List<GenreModel> Genres { get; set; }
}
public class FileProcessingResult
{
    public SongModel? ProcessedSong { get; set; }
    public List<string> Errors { get; } = new List<string>();
    public bool Skipped { get; set; }
    public string SkipReason { get; set; } = string.Empty;
    public bool Success => ProcessedSong != null && Errors.Count == 0 && !Skipped;
}