using Dimmer.Services;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;

namespace Dimmer.WinUI.ViewModel
{
    public partial class BaseViewModelWin : BaseViewModel, IDisposable
    {
        [ObservableProperty]
        private int _currentQueue;

        [ObservableProperty]
        private ObservableCollection<SongModelView>? _displayedSongs;

        [ObservableProperty]
        private CollectionView? _songLyricsCV;

        [ObservableProperty]
        private List<SongModelView>? _filteredSongs;

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
            LoadViewModel();
        }

        private void LoadViewModel()
        {
            // Initialize displayed songs to the full master list
            if (MasterSongs != null)
                DisplayedSongs = new ObservableCollection<SongModelView>(MasterSongs);

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
}
