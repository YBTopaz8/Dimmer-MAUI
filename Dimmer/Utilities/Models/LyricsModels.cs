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
    public string? LinkToCoverImage { get; set; } = "e";
    public List<string>? ListOfLinksToCoverImages { get; set; }
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
    public partial int TimeStampMs { get; set; } = 0;
    [ObservableProperty]
    public partial string? TimeStampText { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? Text { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int NowPlayingLyricsFontSize { get; set; } = 29;
    [ObservableProperty]
    public partial FontAttributes LyricsFontAttributes { get; set; } = FontAttributes.None;
    [ObservableProperty]
    public partial Color LyricsBGColor { get; set; } = Colors.Transparent;
    
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



