

namespace Dimmer_MAUI.Utilities.Models;
public partial class SongModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(SongModel));
    public BaseEmbedded? Instance { get; set; }
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
    public bool IsFileExists { get; set; }
    public SongModel(SongModelView model)
    {
        Instance = new(model.Instance);
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
        SyncLyrics = model.SyncLyrics is null ? null : model.SyncLyrics.Select(x => new LyricPhraseModel().TimeStampText).ToList().ToString();
    }
    public SongModel()
    {
        Instance = new BaseEmbedded();

    }
}


public partial class SongModelView : ObservableObject
{

    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(SongModelView));
    [ObservableProperty]
    BaseEmbeddedView instance = new();
    [ObservableProperty]
    string? title;
    [ObservableProperty]
    string? filePath;
    [ObservableProperty]
    string? artistName;
    [ObservableProperty]
    string? albumName;
    [ObservableProperty]
    string? genreName;
    [ObservableProperty]
    double durationInSeconds;
    [ObservableProperty]
    int? releaseYear;

    [ObservableProperty]
    bool isDeleted;
    [ObservableProperty]
    int? trackNumber;
    [ObservableProperty]
    string? fileFormat;
    [ObservableProperty]
    long fileSize;
    [ObservableProperty]
    int? bitRate;
    [ObservableProperty]
    double? sampleRate;
    [ObservableProperty]
    int rating = 0;

    [ObservableProperty]
    bool hasLyrics;
    [ObservableProperty]
    bool hasSyncedLyrics = false;
    [ObservableProperty]
    bool isInstrumental = false;
    [ObservableProperty]
    string? coverImagePath = null;
    [ObservableProperty]
    string? unSyncLyrics;
    [ObservableProperty]
    bool isPlaying;

    [ObservableProperty]
    bool isCurrentPlayingHighlight;
    [ObservableProperty]
    bool isFavorite;
    [ObservableProperty]
    bool isPlayCompleted;
    [ObservableProperty]
    bool isFileExists;
    [ObservableProperty]
    string? achievement;
    [ObservableProperty]
    ObservableCollection<LyricPhraseModel>? syncLyrics;

    public SongModelView(SongModel model)
    {
        if (model is not null)
        {
            Instance = new(model.Instance);
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
            CoverImagePath = model.CoverImagePath is null ? null : model.CoverImagePath;
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

public partial class PlayDateAndCompletionStateSongLinkView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(PlayDateAndCompletionStateSongLinkView));

    [ObservableProperty]
    BaseEmbeddedView instance = new();
    [ObservableProperty]
    string? songId;
    [ObservableProperty]
    DateTimeOffset datePlayed;
    [ObservableProperty]
    DateTimeOffset dateFinished;
    [ObservableProperty]
    bool wasPlayCompleted;
    public PlayDateAndCompletionStateSongLinkView()
    {
        
    }
    public PlayDateAndCompletionStateSongLinkView(PlayDateAndCompletionStateSongLink model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
    }
}

public partial class PlayDateAndCompletionStateSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(PlayDateAndCompletionStateSongLink));
    public BaseEmbedded? Instance { get; set; } // = new ();
    public string? SongId { get; set; }
    /// <summary>
    /// Indicates the type of play action performed.    
    /// Possible VALID values:
    /// <list type="bullet">
    /// <item><term>0</term><description>Play</description></item>
    /// <item><term>1</term><description>Pause</description></item>
    /// <item><term>2</term><description>Completed</description></item>
    /// </list>
    /// </summary>
    public int PlayType { get; set; } = 0; 
    public DateTimeOffset DatePlayed { get; set; }
    public DateTimeOffset DateFinished { get; set; }
    public bool WasPlayCompleted { get; set; }
        
    public PlayDateAndCompletionStateSongLink()
    {
        Instance = new BaseEmbedded();
    }
        
    public PlayDateAndCompletionStateSongLink(PlayDateAndCompletionStateSongLinkView model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
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


public class BaseEmbedded : EmbeddedObject
{    
    public string? UserIDOnline { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o"); 
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;    
    
    public BaseEmbedded()
    {
    }
    public BaseEmbedded(BaseEmbeddedView model)
    {
        UserIDOnline = model.UserIDOnline;
        DeviceName = model.DeviceName;
        DeviceModel = model.DeviceModel;
        DeviceManufacturer = model.DeviceFormFactor;
        DeviceVersion = model.DeviceVersion;
        DeviceManufacturer = model.DeviceManufacturer;
        LastDateUpdated = model.LastDateUpdated;
        DateCreated = model.DateCreated;
    }


}

public partial class BaseEmbeddedView : ObservableObject
{
    [ObservableProperty]
    string? userIDOnline;
    [ObservableProperty]
    bool isDeleted;
    [ObservableProperty]
    DateTimeOffset? lastDateUpdated;
    public string? DateCreated { get; set; }
    [ObservableProperty]
    string? deviceName = DeviceInfo.Current.Name;
    [ObservableProperty]
    string? deviceFormFactor = DeviceInfo.Current.Idiom.ToString();
    [ObservableProperty]
    string? deviceModel;
    [ObservableProperty]
    string? deviceManufacturer;
    [ObservableProperty]
    string? deviceVersion;
    
    public BaseEmbeddedView()
    {
        DateCreated = DateTime.UtcNow.ToString("o"); // ISO 8601 format

    }
    public BaseEmbeddedView(BaseEmbedded model)
    {
        UserIDOnline = model.UserIDOnline is null ? string.Empty : model.UserIDOnline;
        DeviceName = model.DeviceName is null ? DeviceInfo.Current.Name : model.DeviceName;
        DeviceModel = model.DeviceModel is null ? DeviceInfo.Current.Model : model.DeviceModel;
        DeviceManufacturer = model.DeviceFormFactor is null ? DeviceInfo.Current.Idiom.ToString() : model.DeviceFormFactor;
        DeviceVersion = model.DeviceVersion is null ? DeviceInfo.Current.VersionString : model.DeviceVersion;
        DeviceManufacturer = model.DeviceManufacturer is null ? DeviceInfo.Current.Manufacturer : model.DeviceManufacturer;
        LastDateUpdated = model.LastDateUpdated;
        DateCreated = model.DateCreated;
    }
    private string GenerateRandomString(int length = 10)
    {
        Random random = Random.Shared;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[length];

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(stringChars);
    }

    public enum ActionType
    {
        Add = 0,
        Update = 1,
        Delete = 2
    }

    public enum TargetType
    {
        Song = 0,
        Artist = 1,
        Album = 2,
        Playlist = 3,
        Genre = 4,
        User = 5,
    }
}



