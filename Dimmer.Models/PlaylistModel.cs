

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

public partial class PlaylistModelView : ObservableObject
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<ObjectId> SongsIDs { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }


    public PlaylistModelView()
    {
        
    }

    public PlaylistModelView(PlaylistModel model)
    {
        Id = model.Id;
        Name = model.Name;
        SongsIDs = new List<ObjectId>(model.SongsIDs);
        //SongsIDs = model.SongsIDs;
        DateCreated = model.DateCreated;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;

    }
}