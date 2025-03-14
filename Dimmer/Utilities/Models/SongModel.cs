using ATL;
using System.ComponentModel.DataAnnotations;

namespace Dimmer_MAUI.Utilities.Models;
public partial class SongModel : RealmObject
{

    [PrimaryKey]
    public string? LocalDeviceId { get; set; } //Otherwise called SongId by PlayDataLink
    public string Title { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumName { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public double DurationInSeconds { get; set; }
    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string FileFormat { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? BitRate { get; set; }    
    public int Rating { get; set; } = 0;
    public bool HasLyrics { get; set; }
    public bool HasSyncedLyrics { get; set; }

    public string SyncLyrics { get; set; }=string.Empty;
    public string CoverImagePath { get; set; } = "musicnoteslider.png";
    public string UnSyncLyrics { get; set; } = string.Empty;
    public bool IsPlaying { get; set; }
    public bool IsFavorite { get; set; }
    public string Achievement { get; set; } = string.Empty;
    public bool IsFileExists { get; set; } = true;
    public DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;
    
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public string? UserIDOnline { get; set; } 
    public SongModel(SongModelView model)
    {
        LocalDeviceId = model.LocalDeviceId!;

        Title = string.IsNullOrEmpty(model.Title)? "Unknown Title" : model.Title;
        FilePath = string.IsNullOrEmpty(model.FilePath) ? "Unknown FilePath" : model.FilePath;
        
        ArtistName = string.IsNullOrEmpty(model.ArtistName) ? "Unknown Artist Name" : model.ArtistName;
        
        Genre = string.IsNullOrEmpty(model.GenreName) ? "Unknown Genre" : model.GenreName;
        
        DurationInSeconds = model.DurationInSeconds;
        ReleaseYear = model.ReleaseYear ?? 0;
        TrackNumber = model.TrackNumber;
        
        FileSize = model.FileSize;
        BitRate = model.BitRate ?? 0;   
        Rating = model.Rating;
        HasLyrics = model.HasLyrics;
        HasSyncedLyrics = model.HasSyncedLyrics;
        CoverImagePath = string.IsNullOrEmpty(model.CoverImagePath) ? "Unknown CoverImagePath" : model.CoverImagePath;
        
        AlbumName = string.IsNullOrEmpty(model.AlbumName) ? "Unknown AlbumName" : model.AlbumName;
              
        UnSyncLyrics = string.IsNullOrEmpty(model.UnSyncLyrics) ? "No Lyrics" : model.UnSyncLyrics;
        
        IsPlaying = model.IsPlaying;        
        IsFavorite = model.IsFavorite;
        Achievement = string.IsNullOrEmpty(model.Achievement) ? "No Achievements" : model.Achievement;
        
        IsFileExists = model.IsFileExists;
        var s = new Track();
        LyricsInfo lyrics = new LyricsInfo();
        if (model.SyncLyrics is not null && model.SyncLyrics.Count > 0)
        {
            lyrics.SynchronizedLyrics = [.. model.SyncLyrics.Select(x => new LyricsPhrase(x.TimeStampMs, x.Text))];
            SyncLyrics = lyrics.FormatSynchToLRC();
        }
    }
    public SongModel()
    {
        

    }
}


public partial class SongModelView : ObservableObject
{

    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; }
    [ObservableProperty]
    public partial string? Title {get;set;}=string.Empty;
   
    [ObservableProperty]
    public partial string? FilePath {get;set;}
    [ObservableProperty]
    public partial string? ArtistName {get;set;}
    [ObservableProperty]
    public partial string? AlbumName {get;set;}
    [ObservableProperty]
    public partial string? GenreName {get;set;}
    [ObservableProperty]
    public partial double DurationInSeconds {get;set;}
    [ObservableProperty]
    public partial string? DurationInSecondsText {get;set;}
    [ObservableProperty]
    public partial int? ReleaseYear {get;set;}
    [ObservableProperty]
    public partial bool IsDeleted {get;set;}
    [ObservableProperty]
    public partial int TrackNumber {get;set;}
    [ObservableProperty]
    public partial string? FileFormat {get;set;}
    [ObservableProperty]
    public partial long FileSize {get;set;}
    [ObservableProperty]
    public partial int? BitRate {get;set;}
    [ObservableProperty]
    public partial double SampleRate { get; set; } = 0;
    [ObservableProperty]
    public partial int Rating {get;set;} = 0;

    //[ObservableProperty]
    //public partial bool HasLyrics {get;set;}
    
    public bool HasLyrics {get;set;}
    public bool HasSyncedLyrics {get;set;} = false;    
    public bool IsInstrumental {get;set;} = false;
    [ObservableProperty]
    [Display(AutoGenerateField = true)]
    public partial string? CoverImagePath { get; set; } = "musicnoteslider.png";
    [ObservableProperty]
    public partial string? UnSyncLyrics { get; set; } = string.Empty;
    public bool IsPlaying {get;set;}

    //[ObservableProperty]
    //public partial bool IsCurrentPlayingHighlight {get;set;}
    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight { get; set; } = false;
    [ObservableProperty]
    public partial bool IsFavorite {get;set;}
    [ObservableProperty]
    public partial bool HasCoverImage {get;set;}
    [ObservableProperty]
    public partial bool IsPlayCompleted {get;set;}
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial string? Achievement {get;set;} = string.Empty;
    [ObservableProperty]
    public partial string? SongWiki {get;set;} = string.Empty;

    public bool IsPlayedFromOutsideApp { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SyncLyrics { get; set; } = Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();

    [ObservableProperty]
    public partial List<PlayDataLink> PlayData { get; set; } = new();
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [ObservableProperty]
    public partial int NumberOfTimesPlayed { get; set; }
    [ObservableProperty]
    public partial int NumberOfTimesPlayedCompletely { get; set; }
    public SongModelView(SongModel model) 
    {
        
        if (model is not null)
        {
            LocalDeviceId = model.LocalDeviceId;
            
            Title = model.Title;
            FilePath = model.FilePath;
            DurationInSeconds = model.DurationInSeconds;
            DurationInSecondsText = TimeSpan.FromSeconds(model.DurationInSeconds).ToString(@"mm\:ss");
            ReleaseYear = model.ReleaseYear;

            TrackNumber = model.TrackNumber ?? 0; // Default to 0 if null

            FileFormat = model.FileFormat;
            FileSize = model.FileSize;
            BitRate = model.BitRate;            
            Rating = model.Rating;
            HasLyrics = model.HasLyrics;
            if (!string.IsNullOrEmpty(model.CoverImagePath))
            {
                CoverImagePath = model.CoverImagePath;
                HasCoverImage = true;
            }
            ArtistName = model.ArtistName;
            Achievement = model.Achievement;
            AlbumName = model.AlbumName;
            GenreName = model.Genre;
            UnSyncLyrics = model.UnSyncLyrics;
            IsPlaying = model.IsPlaying;
            IsFavorite = model.IsFavorite;
            HasLyrics = model.HasLyrics;
            HasSyncedLyrics = model.HasSyncedLyrics;
            IsFileExists = model.IsFileExists;
            
            Track s = new Track
            {
                Lyrics = new LyricsInfo()
            };
            if (model.SyncLyrics is not null)
            {
                s.Lyrics.ParseLRC(model.SyncLyrics);
                IList<LyricsPhrase>? syncLyr = s.Lyrics.SynchronizedLyrics;
                SyncLyrics = syncLyr.Select(x => new LyricPhraseModel(x) ).ToObservableCollection();
            }
            
        }

        else
        {

        }
    }

   
    public SongModelView()
    {
        
    }

    
        // Override Equals to compare based on string
        public override bool Equals(object? obj)
        {
        if (obj is SongModelView other)
        {
            return this.LocalDeviceId == other.LocalDeviceId;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LocalDeviceId);
    }

}

public partial class PlayDataLink : ObservableObject
{
    [ObservableProperty]
    public required partial string LocalDeviceId { get; set; }

    [ObservableProperty]
    public partial string? SongId { get; set; }
    /// <summary>
    /// Indicates the type of play action performed.    
    /// Possible VALID values for <see cref="PlayType"/>:
    /// <list type="bullet">
    /// <item><term>0</term><description>Play</description></item>
    /// <item><term>1</term><description>Pause</description></item>
    /// <item><term>2</term><description>Resume</description></item>
    /// <item><term>3</term><description>Completed</description></item>
    /// <item><term>4</term><description>Seeked</description></item>
    /// <item><term>5</term><description>Skipped</description></item>
    /// <item><term>6</term><description>Restarted</description></item>
    /// <item><term>7</term><description>SeekRestarted</description></item>
    /// <item><term>8</term><description>SeekRestarted</description></item>
    /// </list>
    /// </summary>
    [ObservableProperty]
    public partial int PlayType { get; set; } = 0;

    [ObservableProperty]
    public partial DateTime DateStarted { get; set; }

    [ObservableProperty]
    public partial DateTime DateFinished { get; set; }

    [ObservableProperty]
    public partial bool WasPlayCompleted { get; set; }

    [ObservableProperty]
    public partial double PositionInSeconds { get; set; }
    [ObservableProperty]
    public partial DateTime EventDate { get; set; }

    public PlayDataLink()
    {
        
    }
    public PlayDataLink(PlayDateAndCompletionStateSongLink model)
    {        
        LocalDeviceId = model.LocalDeviceId!;
        SongId = model.SongId;
        DateStarted = model.DatePlayed.LocalDateTime;        
        //DateFinished = model.EventDate.LocalDateTime;
        WasPlayCompleted = model.WasPlayCompleted;
        PositionInSeconds = model.PositionInSeconds;
        PlayType = model.PlayType;
        
        EventDate = model.EventDate!.Value.LocalDateTime;

    }
    public override int GetHashCode()
    {
        return HashCode.Combine(LocalDeviceId);
    }

}

public partial class PlayDateAndCompletionStateSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; }
    public string? SongId { get; set; }
    /// <summary>
    /// Indicates the type of play action performed.    
    /// Possible VALID values for <see cref="PlayType"/>:
    /// <list type="bullet">
    /// <item><term>0</term><description>Play</description></item>
    /// <item><term>1</term><description>Pause</description></item>
    /// <item><term>2</term><description>Resume</description></item>
    /// <item><term>3</term><description>Completed</description></item>
    /// <item><term>4</term><description>Seeked</description></item>
    /// <item><term>5</term><description>Skipped</description></item>
    /// <item><term>6</term><description>Restarted</description></item>
    /// <item><term>7</term><description>SeekRestarted</description></item>
    /// <item><term>8</term><description>CustomRepeat</description></item>
    /// <item><term>9</term><description>Previous</description></item>
    /// </list>
    /// </summary>
    public int PlayType { get; set; } = 0; 
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public bool WasPlayCompleted { get; set; }
    public double PositionInSeconds { get; set; }
    public DateTimeOffset? EventDate { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public PlayDateAndCompletionStateSongLink()
    {
        
    }

}

// Assuming you have this enum somewhere
public enum SortingEnum
{
    TitleAsc,
    TitleDesc,
    ArtistNameAsc,
    ArtistNameDesc,
    DateAddedAsc,
    DateAddedDesc,
    DurationAsc,
    DurationDesc,
    YearAsc,
    YearDesc,
    RatingAsc,
    RatingDesc,
    NumberOfTimesPlayedAsc,
    NumberOfTimesPlayedDesc,
    MostSkippedAsc,
    MostSkippedDesc,
    MostPlayedCompletelyAsc,
    MostPlayedCompletelyDesc,
    MostPlayedIncompletelyAsc,
    MostPlayedIncompletelyDesc,
    HasLyrics,
    HasNoLyrics
    // ... other sorting modes ...
}