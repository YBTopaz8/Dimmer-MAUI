// NOTE: This file uses GlobalUsings.cs for common imports (Realms, MongoDB.Bson, etc.)
namespace Dimmer.Data.Models;

/// <summary>
/// Represents the canonical "Musical Work" or composition.
/// This is the abstract idea of a song, independent of any particular recording or rendition.
/// Multiple SongModels (renditions) can link to the same MusicalWork.
/// </summary>
[Dimmer.Utils.Preserve(AllMembers = true)]
public partial class MusicalWorkModel : RealmObject, IRealmObjectWithObjectId
{
    /// <summary>
    /// Unique identifier for this musical work
    /// </summary>
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }

    /// <summary>
    /// The canonical title of the work
    /// </summary>
    [Indexed]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The primary composer or songwriter
    /// </summary>
    public string? Composer { get; set; }

    /// <summary>
    /// The primary artist or performer commonly associated with this work
    /// </summary>
    public string? CanonicalArtist { get; set; }

    /// <summary>
    /// Optional description or notes about the work
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Year the work was originally composed or released
    /// </summary>
    public int? OriginalYear { get; set; }

    /// <summary>
    /// Genre classification for the work
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// When this work entry was created in the database
    /// </summary>
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this work entry was last modified
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// All renditions (song recordings) of this work.
    /// This is a backlink from SongModel.MusicalWork
    /// </summary>
    [Backlink(nameof(SongModel.MusicalWork))]
    public IQueryable<SongModel> Renditions { get; }

    /// <summary>
    /// Tags associated with this work
    /// </summary>
    public IList<TagModel> Tags { get; } = null!;

    /// <summary>
    /// User notes about this work
    /// </summary>
    public IList<UserNoteModel> UserNotes { get; } = null!;

    /// <summary>
    /// Whether this work is marked as favorite
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Aggregated play count across all renditions
    /// </summary>
    [Indexed]
    public int TotalPlayCount { get; set; }

    /// <summary>
    /// Aggregated completed play count across all renditions
    /// </summary>
    public int TotalPlayCompletedCount { get; set; }

    /// <summary>
    /// Most recent play date across all renditions
    /// </summary>
    [Indexed]
    public DateTimeOffset LastPlayed { get; set; }

    /// <summary>
    /// Popularity score aggregated from all renditions
    /// </summary>
    public double PopularityScore { get; set; }

    /// <summary>
    /// Number of renditions linked to this work
    /// </summary>
    [Indexed]
    public int RenditionCount { get; set; }

    public MusicalWorkModel()
    {
    }
}
