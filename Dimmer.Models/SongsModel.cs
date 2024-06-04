namespace Dimmer.Models;
public class SongsModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public required string Title { get; set; }
    public required string FilePath { get; set; }

    public double DurationInSeconds { get; set; }
    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string FileFormat { get; set; }
    public long FileSize { get; set; }
    public int? BitRate { get; set; }
    public double SampleRate { get; set; }
    public int Rating { get; set; } = 0;
    public bool HasLyrics { get; set; }
    public byte[]? CoverImage { get; set; }

    public DateTimeOffset DateAdded { get; set; }
    public DateTimeOffset DateEdited { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
    public int SkipCount { get; set; }

    public ArtistModel? Artist { get; set; }
    public AlbumModel? Album { get; set; }
    public GenreModel? Genre { get; set; }
    public UserModel? User { get; set; }
    public string? UnSyncLyrics { get; set; }
    public bool IsPlaying { get; set; }
    public int PlayCount { get; set; }
    public bool IsFavorite { get; set; }

    public SongsModel() { }

    public SongsModel(SongsModelView modelView)
    {
        Title = modelView.Title;
        FilePath = modelView.FilePath;
        DurationInSeconds = modelView.DurationInSeconds;
        ReleaseYear = modelView.ReleaseYear;
        TrackNumber = modelView.TrackNumber;
        FileFormat = modelView.FileFormat;
        FileSize = modelView.FileSize;
        BitRate = modelView.BitRate;
        SampleRate = modelView.SampleRate;
        Rating = modelView.Rating;
        HasLyrics = modelView.HasLyrics;
        CoverImage = modelView.CoverImage;
        DateAdded = modelView.DateAdded;
        DateEdited = modelView.DateEdited;
        LastPlayed = modelView.LastPlayed;
        SkipCount = modelView.SkipCount;
        Artist = modelView.Artist;
        Album = modelView.Album;
        Genre = modelView.Genre;
        User = modelView.User;
        UnSyncLyrics = modelView.UnSyncLyrics;
        IsPlaying = modelView.IsPlaying;
        PlayCount = modelView.PlayCount;
        IsFavorite = modelView.IsFavorite;
    }
}

public class SongsModelView : INotifyPropertyChanged
{

    public SongsModelView()
    {
        
    }
    public SongsModelView(SongsModel _model)
    {
        if(_model is not null)
        {
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
            CoverImage = _model.CoverImage;
            DateAdded = _model.DateAdded;
            DateEdited = _model.DateEdited;
            LastPlayed = _model.LastPlayed;
            SkipCount = _model.SkipCount;
            Artist = _model.Artist;
            Album = _model.Album;
            Genre = _model.Genre;
            User = _model.User;
            UnSyncLyrics = _model.UnSyncLyrics;
            IsPlaying = _model.IsPlaying;
            PlayCount = _model.PlayCount;
            IsFavorite = _model.IsFavorite;
        }
        else
        {
            _model = new() { Title = string.Empty, FilePath = string.Empty };
        }
    }
    public string Title { get; set; }
    public string FilePath { get; set; }

    public double DurationInSeconds { get; set; }
    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string FileFormat { get; set; }
    public long FileSize { get; set; }
    public int? BitRate { get; set; }
    public double SampleRate { get; set; }
    public int Rating { get; set; } = 0;
    public bool HasLyrics { get; set; }
    public byte[]? CoverImage { get; set; }

    public DateTimeOffset DateAdded { get; set; }
    public DateTimeOffset DateEdited { get; set; }
    public DateTimeOffset LastPlayed { get; set; }
    public int SkipCount { get; set; }

    public ArtistModel? Artist { get; set; }
    public AlbumModel? Album { get; set; }
    public GenreModel? Genre { get; set; }
    public UserModel? User { get; set; }
    //public IList<LyricPhraseModel>? SynchronizedLyrics { get; }
    public string? UnSyncLyrics { get; set; }

    //bool _isPlaying;

    public bool IsPlaying { get; set; }// = false;
    public int PlayCount { get; set; }
    bool isFavorite { get; set; }
    public bool IsFavorite
    {
        get => isFavorite;
        set
        {
            if (isFavorite != value)
            {
                isFavorite = value;
            }
            OnPropertyChanged(nameof(IsFavorite));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

public class LyricPhraseModel
{
    public int TimeStampMs { get; set; }
    public string Text { get; set; }

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