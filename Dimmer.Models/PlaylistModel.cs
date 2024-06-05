
namespace Dimmer.Models;
public class PlaylistModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<ObjectId> SongsIDs { get; } 
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }

    public PlaylistModel()
    {
        
    }
    public PlaylistModel(PlaylistModelView model)
    {
        Id = model.Id;
        Name = model.Name;
        SongsIDs = model.SongsIDs;
        DateCreated = model.DateCreated;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
    }

}

public class PlaylistModelView : INotifyPropertyChanged
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<ObjectId> SongsIDs { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public PlaylistModelView()
    {
        
    }

    public PlaylistModelView(PlaylistModel model)
    {
        Id = model.Id;
        Name = model.Name;
        SongsIDs = model.SongsIDs;
        DateCreated = model.DateCreated;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;

    }
}