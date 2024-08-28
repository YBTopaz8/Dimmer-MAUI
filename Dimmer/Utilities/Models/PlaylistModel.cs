namespace Dimmer.Utilities.Services.Models;
public class PlaylistModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }
    public int TotalSongsCount { get; set; }
    public PlaylistModel()
    {
        
    }

    public PlaylistModel(PlaylistModelView model)
    {
        Id = model.Id;
        Name = model.Name;
        DateCreated = model.DateCreated;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
        TotalSongsCount = model.TotalSongsCount;
    }

}

public class PlaylistSongLink : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public ObjectId PlaylistId { get; set; }
    public ObjectId SongId { get; set; }
}

public partial class PlaylistModelView : ObservableObject
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [ObservableProperty]
    string name;
    public DateTimeOffset DateCreated { get; set; }
    [ObservableProperty]
    double totalDuration;
    [ObservableProperty]
    double totalSize;
    [ObservableProperty]
    int totalSongsCount;
    
    public PlaylistModelView()
    {
        
    }

    public PlaylistModelView(PlaylistModel model)
    {
        Id = model.Id;
        Name = model.Name;
        //SongsIDs = model.SongsIDs;
        DateCreated = model.DateCreated;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
        TotalSongsCount= model.TotalSongsCount;
    }
}