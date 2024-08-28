namespace Dimmer.Utilities.Services.Models;

public class AlbumModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public int? ReleaseYear { get; set; }
    //property for number of tracks
    public int NumberOfTracks { get; set; }
    public ArtistModel Artist { get; set; }
    public IList<SongsModel> Songs { get; }
    public string ImagePath { get; set; }
}