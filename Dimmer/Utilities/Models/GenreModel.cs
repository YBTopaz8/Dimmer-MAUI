namespace Dimmer.Utilities.Services.Models;
public class GenreModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<AlbumModel> Albums { get; }
    public IList<SongsModel> Songs { get; }

    public GenreModel()
    {
        Albums = new List<AlbumModel>();
        Songs = new List<SongsModel>();
    }
}