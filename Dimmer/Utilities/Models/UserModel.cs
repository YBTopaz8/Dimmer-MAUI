namespace Dimmer.Utilities.Services.Models;

public class UserModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
}