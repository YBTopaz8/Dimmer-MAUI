namespace Dimmer.Data.Models;
public partial class ArtistModel : RealmObject
{


    [PrimaryKey]
    public ObjectId Id { get; set; }
    public string? Name { get; set; } = "Unknown Artist";
    public string? Bio { get; set; }
    public string? ImagePath { get; set; } = "lyricist.png";
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [Backlink(nameof(SongModel.ArtistIds))]
    public IQueryable<SongModel> Songs { get; }

    public IList<UserNoteModel> UserNotes { get; }
    public ArtistModel()
    {
        
    }
}
