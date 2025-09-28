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
    // The actual song data
    public SongModelView Song { get; }

    // Is this the original or a duplicate? (For UI styling)
    public DuplicateStatus Status { get; }
    
    public DuplicateReason Reasons { get; }

    // What does the user want to do with this item?
    [ObservableProperty]
    public partial DuplicateAction Action { get; set; }

    public DuplicateItemViewModel(SongModelView song, DuplicateStatus status)
    {
        Song = song;
        Status = status;

        // Sensible defaults: Keep the original, mark duplicates for deletion.
        Action = status == DuplicateStatus.Original ? DuplicateAction.Keep : DuplicateAction.Delete;
    }
}