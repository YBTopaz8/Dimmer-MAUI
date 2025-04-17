using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dimmer.Data;
using Dimmer.Orchestration;
using Dimmer.Services;
using Dimmer.WinUI.Utils.StaticUtils.TaskBarSection;
using Microsoft.UI.Xaml.Controls;

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

            // You no longer need to subscribe manually to CurrentSong or Volume here—
            // BaseViewModel already does that. If you want Taskbar updates, hook into Position:
            _ = SongsMgtFlow.Position
                .Subscribe(pos =>
                {
                    // update a Taskbar progress ring at 0–100%
                    var perc = (int)(100 * pos / (SongsMgtFlow.CurrentlyPlayingSong?.DurationInSeconds ?? 1));
                    WindowsIntegration.SetTaskbarProgress(
                        PlatUtils.GetWindowHandle(),
                        completed: (ulong)perc,
                        total: 100);
                });
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
