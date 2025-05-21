namespace Dimmer.Data.Models;
public partial class GenreModel : RealmObject
{
    [PrimaryKey]
    public string? Id { get; set; }
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public string Name { get; set; } = "Unknown Genre"; 
    [Backlink(nameof(SongModel.Genre))]
    public IQueryable<SongModel> Songs { get; }

    public IList<UserNoteModel> UserNotes { get; }
    public GenreModel()
    {

    }



}