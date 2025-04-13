using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;

namespace Dimmer.WinUI.ViewModel;
public partial class BaseViewModelWin : BaseViewModel
{
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }
    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }

    public BaseViewModelWin(IMapper mapper, SongsMgtFlow songsMgtFlow, IDimmerAudioService dimmerAudioService)
        : base(mapper, songsMgtFlow, dimmerAudioService)
    {
        LoadViewModel();
    }

    public void LoadViewModel()
    {
        if (base.MasterSongs is not null)
        {
            DisplayedSongs = [.. MasterSongs];
        }

        BaseAppFlow.CurrentSong
            .DistinctUntilChanged()
            .Subscribe(song =>
            {
                if (song is null)
                    return;
                if (string.IsNullOrWhiteSpace(song.Title) || song.Title == "Unknown Title")
                {
                    return;
                }
                VolumeLevel = songsMgtFlow.VolumeLevel;

            });
    }
    public static void SetTaskbarProgress(double position)
    {
        WindowsIntegration.SetTaskbarProgress(PlatUtils.GetWindowHandle(), completed: 50, total: 100);
    }
}
