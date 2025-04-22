using Dimmer.Services;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;

namespace Dimmer.WinUI.ViewModel;

public partial class BaseViewModelWin : BaseViewModel, IDisposable
{
    [ObservableProperty]
    public partial int CurrentQueue { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongs { get; set; }

    [ObservableProperty]
    public partial CollectionView? SongLyricsCV { get; set; }

    [ObservableProperty]
    public partial List<SongModelView>? FilteredSongs { get; set; }

    public BaseViewModelWin(
        IMapper mapper,
        AlbumsMgtFlow albumsMgtFlow,
        PlayListMgtFlow playlistsMgtFlow,
        SongsMgtFlow songsMgtFlow,
        IPlayerStateService stateService,
        ISettingsService settingsService,
        SubscriptionManager subs
    ) : base(mapper, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subs)
    {
        ResetDisplayedMasterList();

    }

    private void ResetDisplayedMasterList()
    {
        // Initialize displayed songs to the full master list
        if (MasterListOfSongs != null)
            DisplayedSongs = MasterListOfSongs.ToObservableCollection();
        
    }

    public static void SetTaskbarProgress(double position)
    {
        WindowsIntegration.SetTaskbarProgress(
            PlatUtils.GetWindowHandle(),
            completed: (ulong)position,
            total: 100);
    }

    public void Dispose()
    {
        // if you registered any additional subscriptions here, dispose them
    }
}
