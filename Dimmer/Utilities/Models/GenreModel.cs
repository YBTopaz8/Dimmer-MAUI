namespace Dimmer_MAUI.Utilities.Models;
public class GenreModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }


    public GenreModel()
    {

    }

    public GenreModel(GenreModelView modelView)
    {
        Id = modelView.Id;
        Name = modelView.Name;
    }

    
}

public class AlbumArtistGenreSongLink : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId SongId { get; set; }
    public ObjectId AlbumId { get; set; }
    public ObjectId ArtistId { get; set; }
    public ObjectId GenreId { get; set; }
}

public partial class GenreModelView : ObservableObject
{
    public ObjectId Id { get; set; }

    [ObservableProperty]
    string name;
    public GenreModelView(GenreModel model)
    {
        Id = model.Id;
        name = model.Name;
    }
    public GenreModelView()
    {

    }
}