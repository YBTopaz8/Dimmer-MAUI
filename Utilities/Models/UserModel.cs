namespace Dimmer_MAUI.Utilities.Models;

public class UserModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
}