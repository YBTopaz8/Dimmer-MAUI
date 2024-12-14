namespace Dimmer_MAUI.Utilities.Models;
public class Content
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
    public string? LinkToCoverImage { get; set; }
    public List<string>? listOfLinksToCoverImages { get; set; }
    // Read-only property with logic
    public bool IsSynced => !string.IsNullOrEmpty(SyncedLyrics);
}

public class LyristApiResponse
{
    public string? Lyrics { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Image { get; set; }
    public int Id { get; set; }
}


public partial class LyricPhraseModel : ObservableObject
{
    [ObservableProperty]
    int timeStampMs = 0;
    [ObservableProperty]
    string? timeStampText;
    [ObservableProperty]
    string text = string.Empty;

    // Constructor that accepts a LyricsInfo.LyricsPhrase object
    public LyricPhraseModel(LyricsPhrase? phrase = null)
    {

        if (phrase != null)
        {
            TimeStampText = string.Format("[{0:mm\\:ss\\:fff}]", TimeSpan.FromMilliseconds(phrase.TimestampMs));
            TimeStampMs = phrase.TimestampMs;
            Text = phrase.Text;
        }

    }
}



