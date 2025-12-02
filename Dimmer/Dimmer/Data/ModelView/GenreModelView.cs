namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public partial class GenreModelView : ObservableObject
{
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial string Name { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    [ObservableProperty]
    public partial bool IsNew { get; set; }

    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    /// <summary>
    /// The total number of times any song in this genre has been played to completion.
    /// </summary>
    [ObservableProperty]
    public partial int TotalCompletedPlays { get; set; }

    /// <summary>
    /// The average ListenThroughRate of all songs in this genre.
    /// </summary>
    [ObservableProperty]
    public partial double AverageSongListenThroughRate { get; set; }

    /// <summary>
    /// A score representing user affinity for this genre, based on total plays and average LTR.
    /// </summary>
    [ObservableProperty]
    public partial double AffinityScore { get; set; }

    /// <summary>
    /// The overall rank of this genre in the library, based on its AffinityScore.
    /// </summary>
    [ObservableProperty]
    public partial int OverallRank { get; set; }

}
