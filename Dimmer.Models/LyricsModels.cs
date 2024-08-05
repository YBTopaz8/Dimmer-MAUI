namespace Dimmer.Models;

public class Content
{
    public int id { get; set; }
    public string name { get; set; }
    public string trackName { get; set; }
    public string artistName { get; set; }
    public string albumName { get; set; }
    public float duration { get; set; }
    public bool instrumental { get; set; }
    public string plainLyrics { get; set; }
    public string syncedLyrics { get; set; }
    public string linkToCoverImage { get; set; }
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
    int timeStampMs;
    [ObservableProperty]
    string text;

    // Constructor that accepts a LyricsInfo.LyricsPhrase object
    public LyricPhraseModel(LyricsInfo.LyricsPhrase phrase)
    {
        if (phrase != null)
        {
            TimeStampMs = phrase.TimestampMs;
            Text = phrase.Text;
        }
        else
        {
            // Initialize with default values if 'phrase' is null
            TimeStampMs = 0; // Default timestamp, adjust if necessary
            Text = ""; // Default text, could be "No lyrics available" etc.
        }
    }
}