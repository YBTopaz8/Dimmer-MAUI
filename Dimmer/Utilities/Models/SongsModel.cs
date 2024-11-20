namespace Dimmer_MAUI.Utilities.Services.Models;
public partial class SongsModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Title { get; set; }
    public string? ArtistName { get; set; }
    public string? AlbumName { get; set; }
    public string? Genre { get; set; }
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
    public ObjectId? UserID { get; set; }
    public string? UnSyncLyrics { get; set; }
    public bool IsPlaying { get; set; }
    public bool IsFavorite { get; set; }
    public string Achievement { get; set; }
    public IList<PlayDateAndIsPlayCompletedModel> DatesPlayedAndWasPlayCompleted { get;}
    public ObjectId UserId { get; set; } 
    public SongsModel() { }
    
    public SongsModel(SongModelView modelView)
    {        
        Id = modelView.Id;
        Title = modelView.Title;
        FilePath = modelView.FilePath;
        ArtistName = modelView.ArtistName;
        Genre = modelView.GenreName;
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
        AlbumName = modelView.AlbumName;        
        UserID = modelView.UserID;
        UnSyncLyrics = modelView.UnSyncLyrics;
        IsPlaying = modelView.IsPlaying;        
        IsFavorite = modelView.IsFavorite;
        Achievement = modelView.Achievement;
        DatesPlayedAndWasPlayCompleted = modelView.DatesPlayedAndWasPlayCompleted != null ? modelView.DatesPlayedAndWasPlayCompleted
        .Select(x => new PlayDateAndIsPlayCompletedModel
        {
            DatePlayed = x.DatePlayed,
            WasPlayCompleted = x.WasPlayCompleted
        }).ToList()
    : new List<PlayDateAndIsPlayCompletedModel>(); // Initialize an empty list if null

    }
}

public partial class PlayDateAndIsPlayCompletedModel : RealmObject
{
    public DateTimeOffset DatePlayed { get; set; }

    public bool WasPlayCompleted { get; set; }

    public PlayDateAndIsPlayCompletedModel(PlayDateAndIsPlayCompletedModelView modelView)
    {
        DatePlayed = modelView.DatePlayed;
        WasPlayCompleted = modelView.WasPlayCompleted;
    }
    public PlayDateAndIsPlayCompletedModel()
    {

    }
}
public partial class PlayDateAndIsPlayCompletedModelView : ObservableObject
{
    [ObservableProperty]
    DateTimeOffset datePlayed;
    [ObservableProperty]
    bool wasPlayCompleted;

    public PlayDateAndIsPlayCompletedModelView(PlayDateAndIsPlayCompletedModel model)
    {
        DatePlayed = model.DatePlayed;
        WasPlayCompleted = model.WasPlayCompleted;
    }
    public PlayDateAndIsPlayCompletedModelView()
    {

    }
}

public partial class PlayDateAndCompletionStateSongLink : RealmObject
{
    public ObjectId Id { get; set; }
    public ObjectId SongId { get; set; }
    public ObjectId PlayDateAndSongCompStat { get; set; }
}
public partial class SongModelView : ObservableObject
{

    public SongModelView()
    {

    }
    public SongModelView(SongsModel _model)
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
            CoverImagePath = _model.CoverImagePath is null ? null: _model.CoverImagePath;
            DateAdded = _model.DateAdded;            
            ArtistName = _model.ArtistName;
            Achievement = _model.Achievement;
            AlbumName = _model.AlbumName;
            GenreName = _model.Genre;
            UserID = _model.UserID;
            UnSyncLyrics = _model.UnSyncLyrics;
            IsPlaying = _model.IsPlaying;
            IsFavorite = _model.IsFavorite;
            HasLyrics = _model.HasLyrics;
            HasSyncedLyrics = _model.HasSyncedLyrics;
            DatesPlayedAndWasPlayCompleted = _model.DatesPlayedAndWasPlayCompleted != null
            ? _model.DatesPlayedAndWasPlayCompleted
        .Select(x => new PlayDateAndIsPlayCompletedModelView
        {
            DatePlayed = x.DatePlayed,
            WasPlayCompleted = x.WasPlayCompleted
        }).ToObservableCollection()
    : new ObservableCollection<PlayDateAndIsPlayCompletedModelView>(); // Initialize an empty list if null

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
    string? genreName;
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
    int rating = 0;
    
    [ObservableProperty]
    bool hasLyrics;
    [ObservableProperty]
    bool hasSyncedLyrics = false;
    [ObservableProperty]
    bool isInstrumental = false;
    [ObservableProperty]
    string? coverImagePath=null;
    [ObservableProperty]
    ObservableCollection<PlayDateAndIsPlayCompletedModelView> datesPlayedAndWasPlayCompleted;
    public DateTimeOffset DateAdded { get; set; }
    
    public ObjectId? UserID { get; set; }
    //public IList<LyricPhraseModel>? SynchronizedLyrics { get; }
    [ObservableProperty]
    string? unSyncLyrics;
    [ObservableProperty]
    bool isPlaying;

    [ObservableProperty]
    bool isCurrentPlayingHighlight;
    [ObservableProperty]
    bool isFavorite;
    [ObservableProperty]
    string achievement;
    [ObservableProperty]
    ObservableCollection<LyricPhraseModel> syncLyrics;

    // Override Equals to compare based on ObjectId
    public override bool Equals(object? obj)
    {
        if (obj is SongModelView other)
        {
            return this.Id == other.Id;
        }
        return false;
    }

    // Override GetHashCode to use ObjectId's hash code
    public override int GetHashCode()
    {
        return Id.GetHashCode();
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