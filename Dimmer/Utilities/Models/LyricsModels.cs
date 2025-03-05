﻿namespace Dimmer_MAUI.Utilities.Models;
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
    public partial double Opacity { get; set; } = 0.3;
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

    [ObservableProperty]
    public partial bool IsHighlighted { get; set; } = false;
    [ObservableProperty]
    public partial Color? HighlightColor { get; set; } = null; // Nullable, so no highlight has no color

    [ObservableProperty]
    public partial Color? TextColor { get; set; }  // Or a default from your theme

    [ObservableProperty]
    public partial Thickness Margin { get; set; } = new Thickness(0); // Default no margin.
    [ObservableProperty]
    public partial int DurationMs { get; set; } = 0;

    public int EndTimeMs => TimeStampMs + DurationMs; // No [ObservableProperty]

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;


    [ObservableProperty]
    public partial bool IsLyricSynced { get; set; }

    // Constructor that accepts a LyricsInfo.LyricsPhrase object
    public LyricPhraseModel(LyricsPhrase? phrase = null, int? nextPhraseTimestampMs = null)
    {

        if (phrase != null)
        {
        
            TimeStampText = string.Format("[{0:mm\\:ss\\:fff}]", TimeSpan.FromMilliseconds(phrase.TimestampMs));
            TimeStampMs = phrase.TimestampMs;
            Text = phrase.Text;
            DurationMs = (nextPhraseTimestampMs ?? phrase.TimestampMs) - phrase.TimestampMs; 

            if (!nextPhraseTimestampMs.HasValue)
            {
                DurationMs = 2000;  //Fall back
            }
            if (DurationMs < 0)
                DurationMs = 0;
        }
    }

    
}



