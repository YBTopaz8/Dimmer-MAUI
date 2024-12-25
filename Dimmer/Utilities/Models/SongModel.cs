﻿namespace Dimmer_MAUI.Utilities.Models;
public partial class SongModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(SongModel));
    public string? Title { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string? Genre { get; set; }
    public string? FilePath { get; set; }
    public double DurationInSeconds { get; set; }
    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string? FileFormat { get; set; }
    public long FileSize { get; set; }
    public int? BitRate { get; set; }
    public double? SampleRate { get; set; }
    public int Rating { get; set; } = 0;
    public bool HasLyrics { get; set; }
    public bool HasSyncedLyrics { get; set; }
    public string? SyncLyrics { get; set; }
    public string? CoverImagePath { get; set; }
    public string? UnSyncLyrics { get; set; }
    public bool IsPlaying { get; set; }
    public bool IsFavorite { get; set; }
    public string? Achievement { get; set; }
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
        LocalDeviceId = model.LocalDeviceId;
        Title = model.Title;
        FilePath = model.FilePath;
        ArtistName = model.ArtistName;
        Genre = model.GenreName;
        DurationInSeconds = model.DurationInSeconds;
        ReleaseYear = model.ReleaseYear;
        TrackNumber = model.TrackNumber;
        FileFormat = model.FileFormat;
        FileSize = model.FileSize;
        BitRate = model.BitRate;
        SampleRate = model.SampleRate;
        Rating = model.Rating;
        HasLyrics = model.HasLyrics;
        HasSyncedLyrics = model.HasSyncedLyrics;
        CoverImagePath = model.CoverImagePath;
        AlbumName = model.AlbumName;        
        UnSyncLyrics = model.UnSyncLyrics;
        IsPlaying = model.IsPlaying;        
        IsFavorite = model.IsFavorite;
        Achievement = model.Achievement;
        IsFileExists = model.IsFileExists;
        SyncLyrics = model.SyncLyrics?.Select(x => new LyricPhraseModel().TimeStampText).ToList().ToString();
    }
    public SongModel()
    {
        

    }
}


public partial class SongModelView : ObservableObject
{

    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(SongModelView));


    [ObservableProperty]
    public partial string? Title {get;set;}
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
    public partial int? ReleaseYear {get;set;}

    [ObservableProperty]
    public partial bool IsDeleted {get;set;}
    [ObservableProperty]
    public partial int? TrackNumber {get;set;}
    [ObservableProperty]
    public partial string? FileFormat {get;set;}
    [ObservableProperty]
    public partial long FileSize {get;set;}
    [ObservableProperty]
    public partial int? BitRate {get;set;}
    [ObservableProperty]
    public partial double? SampleRate {get;set;}
    [ObservableProperty]
    public partial int Rating {get;set;} = 0;

    [ObservableProperty]
    public partial bool HasLyrics {get;set;}
    [ObservableProperty]
    public partial bool HasSyncedLyrics {get;set;} = false;
    [ObservableProperty]
    public partial bool IsInstrumental {get;set;} = false;
    [ObservableProperty]
    public partial string? CoverImagePath { get; set; } = null;
    [ObservableProperty]
    public partial string? UnSyncLyrics {get;set;}
    [ObservableProperty]
    public partial bool IsPlaying {get;set;}

    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight {get;set;}
    [ObservableProperty]
    public partial bool IsFavorite {get;set;}
    [ObservableProperty]
    public partial bool IsPlayCompleted {get;set;}
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial string? Achievement {get;set;}
    [ObservableProperty]
    public partial string? SongWiki {get;set;}

    public bool IsPlayedFromOutsideApp { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SyncLyrics { get; set; } = new();


    [ObservableProperty]
    public partial ObservableCollection<PlayDataLink> PlayData { get; set; } = new();
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
            ReleaseYear = model.ReleaseYear;
            TrackNumber = model.TrackNumber;
            FileFormat = model.FileFormat;
            FileSize = model.FileSize;
            BitRate = model.BitRate;
            SampleRate = model.SampleRate;
            Rating = model.Rating;
            HasLyrics = model.HasLyrics;
            CoverImagePath = model.CoverImagePath;
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
            
        }
       

    }

    public SongModelView(SongModel model, ObservableCollection<PlayDateAndCompletionStateSongLink>? AllPlayLinks = null) : this(model)
    {
        if (AllPlayLinks is not null)
        {
            PlayData = AllPlayLinks.Select(x=> new PlayDataLink() 
            {

                LocalDeviceId = x.LocalDeviceId!,                
                SongId = x.SongId,
                DateFinished= x.DateFinished.LocalDateTime,
                DateStarted= x.DatePlayed.LocalDateTime,
                PlayType= x.PlayType,
                WasPlayCompleted= x.WasPlayCompleted,
                PositionInSeconds= x.PositionInSeconds,
                
            }).ToObservableCollection();
            NumberOfTimesPlayed = PlayData.Count;
            NumberOfTimesPlayedCompletely = PlayData.Count(p => p.WasPlayCompleted);
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

    // Override GetHashCode to use string's hash code
    public override int GetHashCode()
    {
        return LocalDeviceId!.GetHashCode();
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
    public partial DateTime EventDate { get; internal set; }=DateTime.UtcNow;

    public PlayDataLink()
    {
        
    }
    public PlayDataLink(PlayDateAndCompletionStateSongLink model)
    {        
        LocalDeviceId = model.LocalDeviceId!;
        SongId = model.SongId;
        DateStarted = model.DatePlayed.LocalDateTime;        
        DateFinished = model.DateFinished.LocalDateTime;
        WasPlayCompleted = model.WasPlayCompleted;
        PositionInSeconds = model.PositionInSeconds;
        PlayType = model.PlayType;
        
        
            EventDate = model.EventDate!.Value.LocalDateTime;
        
    }
}

public partial class PlayDateAndCompletionStateSongLink : RealmObject
{
    [PrimaryKey]
    public string LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(PlayDateAndCompletionStateSongLink));
    
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
    /// <item><term>8</term><description>SeekRestarted</description></item>
    /// </list>
    /// </summary>
    public int PlayType { get; set; } = 0; 
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public bool WasPlayCompleted { get; set; }
    public double PositionInSeconds { get; set; }
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public DateTimeOffset? EventDate { get; set; } = DateTimeOffset.UtcNow;
    public PlayDateAndCompletionStateSongLink()
    {
        
    }

}

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
    PlayCountAsc,
    PlayCountDesc,
    NumberOfTimesPlayedAsc,
    NumberOfTimesPlayedDesc,
    NumberOfTimesPlayedCompletelyAsc,
    NumberOfTimesPlayedCompletelyDesc,
    RatingAsc,
    RatingDesc,
}
