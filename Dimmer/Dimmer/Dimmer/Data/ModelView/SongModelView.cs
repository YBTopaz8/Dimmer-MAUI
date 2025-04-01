using CommunityToolkit.Maui.Core.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Dimmer.Database.ModelView;
public partial class SongModelView : ObservableObject
{

    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; }
    [ObservableProperty]
    public partial string? Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? FilePath { get; set; }
    [ObservableProperty]
    public partial string? ArtistName { get; set; }
    [ObservableProperty]
    public partial string? AlbumName { get; set; }
    [ObservableProperty]
    public partial string? GenreName { get; set; }
    [ObservableProperty]
    public partial double DurationInSeconds { get; set; }
    [ObservableProperty]
    public partial string? DurationInSecondsText { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial bool IsDeleted { get; set; }
    [ObservableProperty]
    public partial int TrackNumber { get; set; }
    [ObservableProperty]
    public partial string? FileFormat { get; set; }
    [ObservableProperty]
    public partial long FileSize { get; set; }
    [ObservableProperty]
    public partial int? BitRate { get; set; }
    [ObservableProperty]
    public partial double SampleRate { get; set; } = 0;
    [ObservableProperty]
    public partial int Rating { get; set; } = 0;

    //[ObservableProperty]
    //public partial bool HasLyrics {get;set;}

    public bool HasLyrics { get; set; }
    public bool HasSyncedLyrics { get; set; } = false;
    public bool IsInstrumental { get; set; } = false;
    [ObservableProperty]
    [Display(AutoGenerateField = true)]
    public partial string? CoverImagePath { get; set; } = "musicnoteslider.png";
    [ObservableProperty]
    public partial string? UnSyncLyrics { get; set; } = string.Empty;
    public bool IsPlaying { get; set; }

    //[ObservableProperty]
    //public partial bool IsCurrentPlayingHighlight {get;set;}
    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight { get; set; } = false;
    [ObservableProperty]
    public partial bool IsFavorite { get; set; }
    [ObservableProperty]
    public partial bool HasCoverImage { get; set; }
    [ObservableProperty]
    public partial bool IsPlayCompleted { get; set; }
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial string? Achievement { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? SongWiki { get; set; } = string.Empty;

    public bool IsPlayedFromOutsideApp { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SyncLyrics { get; set; } = Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();

    
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [ObservableProperty]
    public partial int NumberOfTimesPlayed { get; set; }
    [ObservableProperty]
    public partial int NumberOfTimesPlayedCompletely { get; set; }

}
