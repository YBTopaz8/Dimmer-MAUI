namespace Dimmer.Data.ModelView.LibSanityModels;


// DuplicateStatus.cs
public enum DuplicateStatus
{
    Original, // The one we think should be kept
    Duplicate // A candidate for deletion
}

// DuplicateAction.cs
public enum DuplicateAction
{
    Keep,
    Delete,
    Ignore // "Save aside"
}

[Flags]
public enum DuplicateReason
{
    None = 0,
    // Metadata Matches
    SameTitle = 1,
    SameArtist = 2,
    SameAlbum = 4,

    // File/Audio Property Matches
    SimilarDuration = 8,
    SameFileSize = 16,

    // Quality & Location Differences
    LowerBitrate = 32,
    DifferentFileFormat = 64,
    DifferentFolder = 128,

    // --- Placeholder for future advanced comparison ---
    IdenticalAudioFingerprint = 256
}

public partial class DuplicateItemViewModel : ObservableObject
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public SongModelView Song { get; }
    public DuplicateStatus Status { get; }
    public string Reasons { get; } // Make sure this is set

    [ObservableProperty]
    public partial DuplicateAction Action { get; set; }

    // Updated Constructor
    public DuplicateItemViewModel(SongModelView song, DuplicateStatus status, DuplicateReason reasons = DuplicateReason.None)
    {
        Song = song;
        Status = status;
        Reasons = reasons.ToString();

        Action = status == DuplicateStatus.Original ? DuplicateAction.Keep : DuplicateAction.Delete;
    }
}