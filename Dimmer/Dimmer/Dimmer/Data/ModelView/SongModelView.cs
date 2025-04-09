﻿namespace Dimmer.Database.ModelView;
public partial class SongModelView : ObservableObject
{
    [ObservableProperty]
    public partial string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    [ObservableProperty]
    public partial string Title { get; set; } = "Unknown Title";
    [ObservableProperty]
    public partial string ArtistName { get; set; } = "Unknown Artist Name";
    [ObservableProperty]
    public partial string AlbumName { get; set; } = "Unknown Album Name";
    [ObservableProperty]
    public partial string Genre { get; set; } = "Unknown Genre";
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;
    [ObservableProperty]
    public partial double DurationInSeconds { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial int? TrackNumber { get; set; }
    [ObservableProperty]
    public partial string FileFormat { get; set; } = string.Empty;
    [ObservableProperty]
    public partial long FileSize { get; set; }
    [ObservableProperty]
    public partial int? BitRate { get; set; }
    [ObservableProperty]
    public partial int Rating { get; set; } = 0;
    [ObservableProperty]
    public partial bool HasLyrics { get; set; }
    [ObservableProperty]
    public partial bool HasSyncedLyrics { get; set; }
    [ObservableProperty]
    public partial string SyncLyrics { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string CoverImagePath { get; set; } = "musicnoteslider.png";
    [ObservableProperty]
    public partial string UnSyncLyrics { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight { get; set; } =false;
    [ObservableProperty]
    public partial bool IsFavorite { get; set; }
    [ObservableProperty]
    public partial string Achievement { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    [ObservableProperty]
    public partial string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    [ObservableProperty]
    public partial string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    [ObservableProperty]
    public partial string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    [ObservableProperty]
    public partial string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    [ObservableProperty]
    public partial string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    


}
