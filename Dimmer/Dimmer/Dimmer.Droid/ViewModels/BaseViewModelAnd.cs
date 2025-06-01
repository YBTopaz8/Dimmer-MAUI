
//using System.Reactive.Linq;

using System.ComponentModel;

using Dimmer.Data.Models;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Extensions;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    // _subs is inherited from BaseViewModel as _subsManager and should be used for subscriptions here too
    // private readonly SubscriptionManager _subsLocal = new(); // Use _subsManager from base

    [ObservableProperty]
    private ObservableCollection<SongModelView>? _displayedSongs; // Backing field

    [ObservableProperty]
    private DXCollectionView? _songLyricsCV; // Nullable, ensure it's set from XAML
    private readonly PlayListMgtFlow playlistsMgtFlow;

    private readonly IAppInitializerService appInitializerService;
    // Removed local _stateService and _mapper as they are protected in BaseViewModel

    public BaseViewModelAnd(IMapper mapper, IAppInitializerService appInitializerService, IDimmerLiveStateService dimmerLiveStateService, AlbumsMgtFlow albumsMgtFlow,
       IDimmerAudioService _audioService, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager,
IRepository<SongModel> songRepository, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, ILogger<BaseViewModelWin> logger, ISettingsWindowManager settingsWindwow) : base(mapper, appInitializerService, dimmerLiveStateService, _audioService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subsManager, lyricsMgtFlow, folderMgtService, songRepository, logger)
    {
        // _mapper and _stateService are accessible via base class protected fields.
        // _subs (passed as subsManager) is managed by BaseViewModel as _subsManager.
        this.playlistsMgtFlow=playlistsMgtFlow;

        // Populate DisplayedSongs by subscribing to the state (can be done once)
        _subsManager.Add( // Use the inherited _subsManager
            _stateService.AllCurrentSongs

                .Subscribe(songList =>
                {
                    DisplayedSongs = _mapper.Map<ObservableCollection<SongModelView>>(songList);
                    _logger.LogDebug("BaseViewModelAnd: DisplayedSongs updated with {Count} songs.", DisplayedSongs?.Count ?? 0);
                }, ex => _logger.LogError(ex, "Error updating DisplayedSongs from AllCurrentSongs state in BaseViewModelAnd."))
        );

        SubscribeToLyricIndexChangesForAndroid(); // Android-specific lyric scrolling

        _logger.LogInformation("BaseViewModelAnd initialized.");
    }

    private void SubscribeToLyricIndexChangesForAndroid()
    {
        _subsManager.Add( // Use the inherited _subsManager from BaseViewModel
      Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
          handler => this.PropertyChanged += handler, // Subscribe to the PropertyChanged event
          handler => this.PropertyChanged -= handler) // Unsubscribe
      .Where(evtArgs => evtArgs.EventArgs.PropertyName == nameof(this.ActiveCurrentLyricPhrase)) // Filter for changes to the specific property
      .Select(_ => this.ActiveCurrentLyricPhrase) // Get the new value of the property
                                                  // Ensure execution on the UI thread for UI updates
      .Subscribe(activePhrase =>
      {
          if (activePhrase == null || SongLyricsCV == null)
              return;
          _logger.LogTrace("BaseViewModelAnd: Scrolling lyrics to: {LyricText}", activePhrase.Text);

          // DXCollectionView specific scrolling logic
          // FindItemHandle might need the item itself, not just its properties,
          // ensure LyricPhraseModelView has proper equality or items are reference equal.
          var itemHandle = SongLyricsCV.FindItemHandle(activePhrase);
          if (itemHandle != DXCollectionView.InvalidItemHandle)
          {
              // GetItemVisibleIndex might not be what you want if the item isn't currently visible.
              // ScrollToItem might be more direct if available and suitable.
              // Forcing it into view:
              SongLyricsCV.ScrollTo(itemHandle, DXScrollToPosition.MakeVisible);
          }
          else
          {
              _logger.LogWarning("BaseViewModelAnd: Lyric item handle not found in SongLyricsCV for phrase: {LyricText}", activePhrase.Text);
          }
      }, ex => _logger.LogError(ex, "Error in Android Lyric Scroll subscription."))
        );
    }

    public async Task AddMusicFolderViaPickerAsync(string? selectedFolder = null)
    {

        _logger.LogInformation("SelectSongFromFolderAndroid: Requesting storage permission.");
        var status = await Permissions.RequestAsync<CheckPermissions>();

        if (status == PermissionStatus.Granted)
        {
            _logger.LogInformation("SelectSongFromFolderAndroid: Storage permission granted.");
            string? selectedFolderPath = "/storage/emulated/0/Music/TestFolder"; // Placeholder

            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                _logger.LogInformation("Folder selected: {FolderPath}. Adding to preferences and triggering scan.", selectedFolderPath);
                // The FolderManagementService should handle adding to settings and triggering the scan.
                // We just need to tell it the folder was selected by the user.

                await _folderMgtService.AddFolderToWatchListAndScanAsync(selectedFolderPath); // This method in IFolderMgtService will:
                                                                                              // 1. Add to ISettingsService
                                                                                              // 2. Restart IFolderMonitorService
                                                                                              // 3. Call ILibraryScannerService.ScanSpecificPathsAsync for this new path
            }
            else
            {
                _logger.LogInformation("No folder selected by user.");
            }
        }
        else
        {
            _logger.LogWarning("Storage permission denied for adding music folder.");
            // TODO: Show message to user explaining why permission is needed.
        }

    }

    public void LoadAndPlaySongTapped(SongModelView? songToPlay) // Make songToPlay nullable
    {
        if (songToPlay == null)
        {
            _logger.LogWarning("LoadAndPlaySongTapped: songToPlay is null.");
            return;
        }

        var songToPlayModel = songToPlay.ToModel(_mapper);
        if (songToPlayModel == null)
        {
            _logger.LogWarning("LoadAndPlaySongTapped: Could not map songToPlay '{SongTitle}' to SongModel.", songToPlay.Title);
            return;
        }

        _logger.LogInformation("LoadAndPlaySongTapped: Requesting to play '{SongTitle}'.", songToPlay.Title);

        // Determine the context for playback.
        // Is it part of the currently displayed list (DisplayedSongs)?
        // Is there a specific playlist active in the global state (_stateService.CurrentPlaylist)?

        var activePlaylistFromState = _stateService.CurrentPlaylist.FirstAsync().Wait(); // Blocking call!
        PlaylistModel? activePlaylistModel = null;
        if (activePlaylistFromState != null)
        {
            // Assuming your PlaylistModel is directly usable or map if needed.
            // This assumes ActivePlaylistModel is the actual model, not a view model.
            activePlaylistModel = activePlaylistFromState;
        }


        if (activePlaylistModel != null && activePlaylistModel.SongsInPlaylist.Any(s => s.Id == songToPlayModel.Id))
        {
            _logger.LogDebug("Playing '{SongTitle}' from active playlist context '{PlaylistName}'.", songToPlay.Title, activePlaylistModel.PlaylistName);
            int startIndex = activePlaylistModel.SongsInPlaylist.ToList().FindIndex(s => s.Id == songToPlayModel.Id);
            playlistsMgtFlow.PlayPlaylist(activePlaylistModel, Math.Max(0, startIndex));
        }
        else if (DisplayedSongs != null && DisplayedSongs.Any(svm => svm.Id == songToPlay.Id))
        {
            _logger.LogDebug("Playing '{SongTitle}' from current 'DisplayedSongs' list.", songToPlay.Title);
            var songListModels = DisplayedSongs.Select(svm => svm.ToModel(_mapper)).Where(sm => sm != null).ToList()!;
            int startIndex = DisplayedSongs.IndexOf(songToPlay);
            playlistsMgtFlow.PlayGenericSongList(songListModels, Math.Max(0, startIndex), "Current Displayed List");
        }
        else
        {

        }
    }

    public async Task InitializeApp()
    {
        await appInitializerService.InitializeApplicationAsync();
    }
}