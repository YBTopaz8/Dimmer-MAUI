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
    public string Name { get; set; } = "Unknown Genre";
    [Backlink(nameof(SongModel.Genre))]
    public IQueryable<SongModel> Songs { get; }

    public bool IsNew { get; set; }

    public IList<UserNoteModel> UserNotes { get; }
    public GenreModel()
    {

    }



}