namespace Dimmer.DimmerLive.Models;
[ParseClassName("_User")]
public class UserModelOnline : ParseUser
{
    [ParseFieldName("profileImagePath")]
    public string? ProfileImagePath // Use nullable if the field might not exist or be null
    {
        get => GetProperty<string?>();
        set => SetProperty(value);
    }

    private const string UserDeviceSessionsKey = "userDeviceSessions"; // Define key as a constant

    [ParseFieldName(UserDeviceSessionsKey)] // Conventionally plural for relations
    public ParseRelation<UserDeviceSession> UserDeviceSessions
    {
        // The GetRelation<T>() method is used to access relation fields.
        // You don't typically provide a setter for ParseRelation properties.
        get => GetRelation<UserDeviceSession>(UserDeviceSessionsKey);
    }

    [ParseFieldName("profileImageFile")]
    public ParseFile? ProfileImageFile 
    { 
        get=> GetProperty<ParseFile?>();

        set => SetProperty(value);
    }

    public UserModelOnline() : base() { }
    public UserModelOnline(ParseUser plainUser) : this() // Calls the base parameterless constructor
    {
        if (plainUser == null)
            return;

        // Copy all fields from plainUser to this instance.
        // The ParseObject indexer (this[key]) and plainUser[key] handle type conversions
        // and know about Parse-specific types.
        foreach (var key in plainUser.Keys)
        {
            this[key] = plainUser[key];
        }

        // If the plainUser has an ACL, you might want to copy it as well.
        // Create a new ACL instance to avoid modifying the original plainUser's ACL.
        if (plainUser.ACL != null)
        {
            this.ACL = new ParseACL(plainUser);
        }
    }

}
