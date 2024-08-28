namespace Dimmer.Utilities.Services.Models;
public class SongsModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Title { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }

    public string FilePath { get; set; }

    public double DurationInSeconds { get; set; }
    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string FileFormat { get; set; }
    public long FileSize { get; set; }
    public int? BitRate { get; set; }
    public double? SampleRate { get; set; }
    public int Rating { get; set; } = 0;
    public bool HasLyrics { get; set; }
    public bool HasSyncedLyrics { get; set; }
    public string? CoverImagePath { get; set; }

    public DateTimeOffset DateAdded { get; set; }
    public DateTimeOffset DateEdited { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
    public int SkipCount { get; set; }

    public ObjectId? ArtistID { get; set; }
    public ObjectId? AlbumID { get; set; }
    public ObjectId? GenreID { get; set; }
    public ObjectId? UserID { get; set; }
    public string? UnSyncLyrics { get; set; }
    public bool IsPlaying { get; set; }
    public int PlayCount { get; set; }
    public bool IsFavorite { get; set; }

    public SongsModel() { }

    public SongsModel(SongsModelView modelView)
    {
        Id = modelView.Id;
        Title = modelView.Title;
        FilePath = modelView.FilePath;
        ArtistName = modelView.ArtistName;
        DurationInSeconds = modelView.DurationInSeconds;
        ReleaseYear = modelView.ReleaseYear;
        TrackNumber = modelView.TrackNumber;
        FileFormat = modelView.FileFormat;
        FileSize = modelView.FileSize;
        BitRate = modelView.BitRate;
        SampleRate = modelView.SampleRate;
        Rating = modelView.Rating;
        HasLyrics = modelView.HasLyrics;
        HasSyncedLyrics = modelView.HasSyncedLyrics;
        CoverImagePath = modelView.CoverImagePath;
        DateAdded = modelView.DateAdded;
        DateEdited = modelView.DateEdited;
        LastPlayed = modelView.LastPlayed;
        SkipCount = modelView.SkipCount;
        ArtistID = modelView.ArtistID;
        AlbumID = modelView.AlbumID;
        AlbumName = modelView.AlbumName;
        GenreID = modelView.GenreID;
        UserID = modelView.UserID;
        UnSyncLyrics = modelView.UnSyncLyrics;
        IsPlaying = modelView.IsPlaying;
        PlayCount = modelView.PlayCount;
        IsFavorite = modelView.IsFavorite;
    }
}

public partial class SongsModelView : ObservableObject
{

    public SongsModelView()
    {

    }
    public SongsModelView(SongsModel _model)
    {
        if (_model is not null)
        {
            Id = _model.Id;
            Title = _model.Title;
            FilePath = _model.FilePath;
            DurationInSeconds = _model.DurationInSeconds;
            ReleaseYear = _model.ReleaseYear;
            TrackNumber = _model.TrackNumber;
            FileFormat = _model.FileFormat;
            FileSize = _model.FileSize;
            BitRate = _model.BitRate;
            SampleRate = _model.SampleRate;
            Rating = _model.Rating;
            HasLyrics = _model.HasLyrics;
            CoverImagePath = _model.CoverImagePath;
            DateAdded = _model.DateAdded;
            DateEdited = _model.DateEdited;
            LastPlayed = _model.LastPlayed;
            SkipCount = _model.SkipCount;
            ArtistID = _model.ArtistID;
            ArtistName = _model.ArtistName;
            AlbumID = _model.AlbumID;
            AlbumName = _model.AlbumName;
            GenreID = _model.GenreID;
            UserID = _model.UserID;
            UnSyncLyrics = _model.UnSyncLyrics;
            IsPlaying = _model.IsPlaying;
            PlayCount = _model.PlayCount;
            IsFavorite = _model.IsFavorite;
            HasLyrics = _model.HasLyrics;
            HasSyncedLyrics = _model.HasSyncedLyrics;
        }
        else
        {
            _model = new() { Title = string.Empty, FilePath = string.Empty };
        }
    }
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [ObservableProperty]
    string title;
    [ObservableProperty]
    string filePath;
    [ObservableProperty]
    string? artistName;
    [ObservableProperty]
    string? albumName;
    [ObservableProperty]
    double durationInSeconds;
    [ObservableProperty]
    int? releaseYear;
    [ObservableProperty]
    int? trackNumber;
    [ObservableProperty]
    string fileFormat;
    [ObservableProperty]
    long fileSize;
    [ObservableProperty]
    int? bitRate;
    [ObservableProperty]
    double? sampleRate;
    [ObservableProperty]
    int rating;
    
    [ObservableProperty]
    bool hasLyrics;
    [ObservableProperty]
    bool hasSyncedLyrics = false;

    [ObservableProperty]
    string? coverImagePath;
    public DateTimeOffset DateAdded { get; set; }
    [ObservableProperty]
    DateTimeOffset dateEdited;
    public DateTimeOffset LastPlayed { get; set; }
    public DateTime CreationTime { get; set; }
    public int SkipCount { get; set; }

    public ObjectId? ArtistID { get; set; }
    public ObjectId? AlbumID { get; set; }
    public ObjectId? GenreID { get; set; }
    public ObjectId? UserID { get; set; }
    //public IList<LyricPhraseModel>? SynchronizedLyrics { get; }
    [ObservableProperty]
    string? unSyncLyrics;

    //bool _isPlaying;
    [ObservableProperty]
    int playCount;

    [ObservableProperty]
    bool isPlaying;
    [ObservableProperty]
    bool isFavorite;

}

