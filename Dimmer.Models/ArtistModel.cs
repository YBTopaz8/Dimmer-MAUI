namespace Dimmer.Models;

public class ArtistModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public required string Name { get; set; }
    public string ImagePath { get; set; }
    public IList<AlbumModel> Albums { get; }
    public IList<SongsModel> Songs { get; }

    public ArtistModel()
    {
        Albums = new List<AlbumModel>();
        Songs = new List<SongsModel>();
    }
}