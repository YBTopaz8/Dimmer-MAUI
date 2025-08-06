﻿namespace Dimmer.Data.ModelView;
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

}
