using System.Runtime.Serialization;

namespace Dimmer.Data.Models;

[Dimmer.Utils.Preserve(AllMembers = true)]
public partial class UserModel : RealmObject, IRealmObjectWithObjectId
{

    public IList<string> EarnedAchievementIds { get; }
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? UserPassword { get; set; }
    public string? UserProfileImage { get; set; }
    public string? UserBio { get; set; }
    public string? UserCountry { get; set; }
    public string? UserLanguage { get; set; }
    public string? UserTheme { get; set; }
    public string? UserIDOnline { get; set; }
    public string? SessionToken { get; set; } = string.Empty;
    public DateTimeOffset? UserDateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    public LastFMUser LastFMAccountInfo { get; set; }
    public bool IsNew { get; set; }
    public IList<TagModel> Tags { get; }



    public UserModel()
    {

    }


}

[Dimmer.Utils.Preserve(AllMembers = true)]
public partial class LastFMUser:EmbeddedObject
{
        #region Properties

        /// <summary>
        /// Gets the user name.
        /// </summary>
        [DataMember(Name = "name")]
        public string? Name { get;  set; }

        /// <summary>
        /// Gets or sets the real name.
        /// </summary>
        public string? RealName { get; set; }

        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets the age.
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// Gets or sets the gender.
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Gets or sets the total playcount.
        /// </summary>
        public int Playcount { get; set; }

        /// <summary>
        /// Gets or sets the number of playlists.
        /// </summary>
        public int Playlists { get; set; }

        /// <summary>
        /// Gets or sets the date of registration.
        /// </summary>
        public DateTimeOffset Registered { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets a list of images.
        /// </summary>
        public LastImage? Image { get; set; }

    [Dimmer.Utils.Preserve(AllMembers = true)]
    public partial class LastImage : EmbeddedObject
    {
        /// <summary>
        /// Gets or sets the image size.
        /// </summary>
        [DataMember(Name = "size")]
        public string? Size { get; set; }

        /// <summary>
        /// Gets or sets the image url.
        /// </summary>
        [DataMember(Name = "url")]
        public string? Url { get; set; }
    }

    #endregion



}