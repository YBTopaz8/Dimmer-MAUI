namespace Dimmer.Data.Models;

public partial class TagModel : RealmObject, IRealmObjectWithObjectId
{
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public string Name { get; set; } = "Unknown Tag";
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    [Backlink(nameof(SongModel.Tags))]
    public IQueryable<SongModel>? Songs { get; }

    [Backlink(nameof(ArtistModel.Tags))]
    public IQueryable<ArtistModel>? Artists { get; }

    [Backlink(nameof(AlbumModel.Tags))]
    public IQueryable<AlbumModel>? Albums { get; }

    [Backlink(nameof(UserModel.Tags))]
    public IQueryable<UserModel>? Users { get; }

    public bool IsNew { get; set; }

}