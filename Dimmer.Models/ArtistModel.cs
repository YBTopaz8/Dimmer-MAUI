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
public class ArtistModelView : INotifyPropertyChanged
{
    public ObjectId Id { get; set;} = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public string ImagePath { get; set; }
    public IList<ObjectId> SongsIDs { get; set; }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ArtistModelView()
    {
        //Albums = new List<AlbumModelView>();
        //Songs = new List<SongsModelView>();
    }

    public ArtistModelView(ArtistModel model)
    {
        Id = model.Id;
        Name = model.Name;
        ImagePath = model.ImagePath;
        //Albums = model.Albums.Select(a => new AlbumModelView(a)).ToList();
        SongsIDs = model.SongsIDs;// model.Songs.Select(s => new SongsModelView(s)).ToList();        
    }
}