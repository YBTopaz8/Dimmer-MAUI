namespace Dimmer.Data.Models;
public partial class ArtistModel : RealmObject, IRealmObjectWithObjectId
{

    public bool IsNew { get; set; }

    [PrimaryKey]
    public ObjectId Id { get; set; }
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    [Backlink(nameof(SongModel.ArtistToSong))]
    public IQueryable<SongModel> Songs { get; }

    [Backlink(nameof(AlbumModel.ArtistIds))]
    public IQueryable<AlbumModel> Albums { get; }



    public IList<TagModel> Tags { get; } = null!;
    public string? ImagePath { get; set; }
    public IList<UserNoteModel> UserNotes { get; } = null!;
    public ArtistModel()
    {

    }
}
