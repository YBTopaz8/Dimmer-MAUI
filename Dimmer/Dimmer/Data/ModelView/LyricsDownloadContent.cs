

namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public class LyricsDownloadContent
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? TrackName { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public float Duration { get; set; }
    public bool Instrumental { get; set; }
    public string? PlainLyrics { get; set; }
    public string? SyncedLyrics { get; set; }
    public string? LinkToCoverImage { get; set; } = "e";
    public List<string>? ListOfLinksToCoverImages { get; set; }
    // Read-only property with logic
    public bool IsSynced => !string.IsNullOrEmpty(SyncedLyrics);
}

