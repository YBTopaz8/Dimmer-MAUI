namespace Dimmer_MAUI.Utilities.Services.Models;
public class Content
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? trackName { get; set; }
    public string? artistName { get; set; }
    public string? albumName { get; set; }
    public float duration { get; set; }
    public bool instrumental { get; set; }
    public string? plainLyrics { get; set; }
    public string? syncedLyrics { get; set; }
    public string? linkToCoverImage { get; set; }
}

public class LyristApiResponse
{
    public string lyrics { get; set; }
    public string title { get; set; }
    public string artist { get; set; }
    public string image { get; set; }
    public int id { get; set; }
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
    public LyricPhraseModel(ATL.LyricsInfo.LyricsPhrase? phrase=null)
    {
        
        if (phrase != null)
        {
            TimeStampText = string.Format("[{0:mm\\:ss\\:fff}]", TimeSpan.FromMilliseconds(phrase.TimestampMs));
            TimeStampMs = phrase.TimestampMs;
            Text = phrase.Text;
        }
        
    }
}