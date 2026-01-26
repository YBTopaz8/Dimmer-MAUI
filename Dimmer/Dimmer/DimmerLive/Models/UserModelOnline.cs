namespace Dimmer.DimmerLive.Models;
[ParseClassName("_User")]
public class UserModelOnline : ParseUser
{
    private static readonly HashSet<string> _immutableUserKeys = new HashSet<string>
    {
        "sessionToken",
        "authData",
        // It's also good practice to exclude these as they are set via specific methods
        "username",
        "password",
        "email"
    };
    [ParseFieldName("profileImagePath")]
    public string? ProfileImagePath // Use nullable if the field might not exist or be null
    {
        get => GetProperty<string?>();
        set => SetProperty(value);
    }
    [ParseFieldName("isPremium")]
    public bool IsPremium
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    public bool IsAuthenticated { get; set; }
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

    [ParseFieldName("bio")]
    public string? Bio 
    { 
        get=> GetProperty<string?>();

        set => SetProperty(value);
    }
    
    public UserModelOnline() : base() { }
    public UserModelOnline(ParseUser plainUser) : this() // Calls the base parameterless constructor
    {
        if (plainUser == null)
            return;

        this.ObjectId = plainUser.ObjectId;

        foreach (var key in plainUser.Keys)
        {
            if (key == "sessionToken" || key == "objectId" || key == "username" || key == "email")
                continue;
             
            this[key] = plainUser[key];
        }
        this.ObjectId = plainUser.ObjectId;

        if (!string.IsNullOrEmpty(plainUser.Username))
            this.Username = plainUser.Username;
        if (!string.IsNullOrEmpty(plainUser.Email))
            this.Email = plainUser.Email;



        if (plainUser.ACL != null)
        {
            this.ACL = plainUser.ACL;
        }
    }

}
