
namespace Dimmer.Models;

public class ArtistModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string? Name { get; set; }
    public string? ImagePath { get; set; }
    public IList<ObjectId>? AlbumsIDs { get; }
    public IList<ObjectId>? SongsIDs { get; }

    public ArtistModel()
    {
    }

    public ArtistModel(ArtistModelView modelView)
    {
        Id = modelView.Id;
        Name = modelView.Name;
        ImagePath = modelView.ImagePath;
        //Albums = modelView.Albums;//.ToList();
        SongsIDs = modelView.SongsIDs;//modelView.Songs.Select(s => new SongsModel(s)).ToList();
        
    }
}

// ViewModel for ArtistModel
public partial class ArtistModelView : ObservableObject
{
    public ObjectId Id { get; set;}

    [ObservableProperty]
    string name;
    public string ImagePath { get; set; }

    [ObservableProperty]
    ObservableCollection<ObjectId> songsIDs;


    public ArtistModelView()
    {
    }

    public ArtistModelView(ArtistModel model)
    {
        Id = model.Id;
        Name = model.Name;
        ImagePath = model.ImagePath;
        SongsIDs = new ObservableCollection<ObjectId>(model.SongsIDs ?? new List<ObjectId>());

    }
}