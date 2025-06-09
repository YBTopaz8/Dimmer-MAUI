namespace Dimmer.Data.Models;
public partial class ArtistModel : RealmObject, IRealmObjectWithObjectId
{

    public bool IsNew { get; set; }

    [PrimaryKey]
    public ObjectId Id { get; set; }
    public string? Name { get; set; } = "Unknown Artist";
    public string? Bio { get; set; }
    public string? ImagePath { get; set; } = "lyricist.png";
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    [Backlink(nameof(SongModel.ArtistIds))]
    public IQueryable<SongModel> Songs { get; }


    [Backlink(nameof(AlbumModel.ArtistIds))]
    public IQueryable<AlbumModel> Albums { get; }


    public IList<TagModel> Tags { get; }
    public IList<UserNoteModel> UserNotes { get; }
    public ArtistModel()
    {

    }
}
