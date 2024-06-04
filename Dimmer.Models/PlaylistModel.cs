
namespace Dimmer.Models;
public class PlaylistModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<ObjectId> SongsID { get; } 
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }

    public PlaylistModelDup Detach()
    {
        return new PlaylistModelDup
        {
            Id = this.Id,
            Name = this.Name,
            SongsID = SongsID,
            DateCreated = this.DateCreated,
            TotalDuration = this.TotalDuration,
            TotalSize = this.TotalSize
        };
    }

}

public class PlaylistModelDup
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string Name { get; set; }
    public IList<ObjectId> SongsID { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }
}