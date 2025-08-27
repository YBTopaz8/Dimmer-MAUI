namespace Dimmer.Data.Models;
public partial class GenreModel : RealmObject, IRealmObjectWithObjectId
{
    [PrimaryKey]
    public ObjectId Id { get; set; }
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    public string Name { get; set; }
    [Backlink(nameof(SongModel.Genre))]
    public IQueryable<SongModel> Songs { get; }

    public bool IsNew { get; set; }

    public IList<UserNoteModel> UserNotes { get; }

    /// <summary>
    /// The total number of times any song in this genre has been played to completion.
    /// </summary>
    [Indexed]
    public int TotalCompletedPlays { get; set; }

    /// <summary>
    /// The average ListenThroughRate of all songs in this genre.
    /// </summary>
    
    public double AverageSongListenThroughRate { get; set; }

    /// <summary>
    /// A score representing user affinity for this genre, based on total plays and average LTR.
    /// </summary>
    
    public double AffinityScore { get; set; }

    /// <summary>
    /// The overall rank of this genre in the library, based on its AffinityScore.
    /// </summary>
    [Indexed]
    public int OverallRank { get; set; }

    public GenreModel()
    {

    }



}