
//using System.Reactive.Linq;

using Android.Text;

using AndroidX.Lifecycle;

using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;

using DevExpress.Maui.Controls;

using Dimmer.Data.Models;
using Dimmer.DimmerLive;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.StatsUtils;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

using System.ComponentModel;
using System.Runtime.CompilerServices;

using SwipeItem = DevExpress.Maui.CollectionView.SwipeItem;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    public LoginViewModel LoginViewModel => _loginViewModel;
    private readonly LoginViewModel _loginViewModel;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerAudioService audioService;

    // _subs is inherited from BaseViewModel as _subsManager and should be used for subscriptions here too
    // private readonly SubscriptionManager _subsLocal = new(); // Use _subsManager from base
    private readonly IMapper mapper;
    private readonly IFolderPicker folderPicker;
    private readonly IAnimationService animService;
    private readonly IDimmerStateService stateService;
    private readonly ISettingsService settingsService;
    private readonly SubscriptionManager subsManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    private readonly LyricsMgtFlow lyricsMgtFlow;
    private readonly IFolderMgtService folderMgtService;
    private readonly BaseViewModel baseVM;
    public BaseViewModel BaseVM => baseVM; // Expose BaseViewModel reference if needed


    [ObservableProperty]
    public partial DXCollectionView? SongLyricsCV { get; set; } // Nullable, ensure it's set from XAML

    // Removed local stateService and mapper as they are protected in BaseViewModel



    [ObservableProperty]
    public partial SafeKeyboardAreaView MySafeKeyboardAreaView { get; set; }


    [ObservableProperty]
    public partial int NowPlayingQueueItemSpan { get; set; }


    [ObservableProperty]
    public partial int NowPlayingTabIndex { get; set; }

    [ObservableProperty]
    public partial bool NowPlayingUI { get; set; }

    partial void OnNowPlayingTabIndexChanged(int oldValue, int newValue)
    {
        switch (newValue)
        {
            case 0: IsNowPlayingQueue =false;
                IsNowAllSongsQueue=true;
                NowPlayingUI =false;
               
                break;
            case 1:
               
                IsNowPlayingQueue =false;

                IsNowAllSongsQueue=false;
                NowPlayingUI=true;
                break;
            case 2:
                break;
            default:
                break;
        }
    }
    [ObservableProperty]
    public partial bool IsNowPlayingQueue { get; set; }
    [ObservableProperty]
    public partial bool IsNowAllSongsQueue { get; set; }
    partial void OnNowPlayingQueueItemSpanChanged(int oldValue, int newValue)
    {
        // Handle any additional logic when NowPlayingQueueItemSpan changes, if needed.
        _logger.LogInformation("NowPlayingQueueItemSpan changed from {OldValue} to {NewValue}", oldValue, newValue);
    }


    bool isAppBooting = false;

    public void FiniInit()
    {
        if (isAppBooting)
        {
            isAppBooting = false;
        }
    }




    [ObservableProperty] public partial Page CurrentUserPage { get; set; }

    [ObservableProperty] public partial ObservableCollection<AnimationSetting>? PageAnimations { get; set; }
    public void GetAllAnimations()
    {
        PageAnimations = animService.GetAvailableAnimations().ToObservableCollection();
    }
    public async Task SavePage(Page PageToSave, int duration, bool IsEnter)
    {

        // (Add null checks here for safety)

        // STEP 3: Call our public API to save the settings.
        // This is the key method call you were asking about!
        // It takes the page type and the four chosen AnimationSetting objects.
        AnimationManager.SetPageAnimations(
            PageToSave.GetType(),
            null,
            null,
            null,
            null
        );

        await Shell.Current.DisplayAlert("Success", "Settings saved!", "OK");
    }
    public async Task AddMusicFolderViaPickerAsync(string? selectedFolder = null)
    {

        _logger.LogInformation("SelectSongFromFolderAndroid: Requesting storage permission.");
        var status = await Permissions.RequestAsync<CheckPermissions>();

        if (status == PermissionStatus.Granted)
        {
            var res = await folderPicker.PickAsync(CancellationToken.None);

            if (res is not null)
            {


                string? selectedFolderPath = res?.Folder?.Path;



                if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    _logger.LogInformation("Folder selected: {FolderPath}. Adding to preferences and triggering scan.", selectedFolderPath);
                    // The FolderManagementService should handle adding to settings and triggering the scan.
                    // We just need to tell it the folder was selected by the user.

                  await  AddMusicFolderByPassingToService(selectedFolderPath);
                }
                else
                {
                    _logger.LogInformation("No folder selected by user.");
                }


            }

        }
        else
        {
            _logger.LogWarning("Storage permission denied for adding music folder.");
            // TODO: Show message to user explaining why permission is needed.
        }

    }

    internal void ViewArtistDetails(ArtistModelView? s)
    {
        ViewArtistDetails(s);
    }

    private bool _isExpanded;

    public BaseViewModelAnd(IDimmerAudioService AudioService, ILogger<BaseViewModelAnd> logger, 
        IMapper mapper, IDimmerStateService dimmerStateService, MusicDataService musicDataService,
        IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, 
        ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, 
        ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> SongRepo, 
        IDuplicateFinderService duplicateFinderService, ILastfmService LastfmService, IRepository<ArtistModel> artistRepo, 
        IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService) : base(mapper, dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, SongRepo, duplicateFinderService, LastfmService, artistRepo, albumModel, genreModel, dialogueService, logger)
    {
       
        
        // mapper and stateService are accessible via base class protected fields.
        // _subs (passed as subsManager) is managed by BaseViewModel as _subsManager.

        this._logger = new LoggerFactory().CreateLogger<BaseViewModelAnd>();
        isAppBooting=true;
        this._logger.LogInformation("BaseViewModelAnd initialized.");
        audioService=AudioService;
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            // This is how we link the native sheet's state to our UI's appearance.
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(IsMiniPlayerVisible));
                OnPropertyChanged(nameof(IsFullPlayerVisible));
            }
        }
    }

    // These properties will drive the visibility of our layouts in XAML.
    public bool IsMiniPlayerVisible => !_isExpanded;
    public bool IsFullPlayerVisible => _isExpanded;

    // --- Communication Bridge ---
    // This event is for the ViewModel to tell the native UI what to do.
    public event EventHandler<bool> RequestSheetStateChange;

    // Call this from your MAUI UI (e.g., a tap gesture) to expand the sheet.
    public void TriggerExpand() => RequestSheetStateChange?.Invoke(this, true);

    // Call this from your MAUI UI (e.g., a "down arrow" button) to collapse.
    public void TriggerCollapse() => RequestSheetStateChange?.Invoke(this, false);


    [RelayCommand]
    void PlayClicked()
    {
        TriggerExpand();
    }

    [RelayCommand]
    async Task SkipNextClicked()
    {
        await Shell.Current.DisplayAlert("SkipNext Clicked", "SkipNext button was clicked!", "OK");
    }
    [ObservableProperty]
    public partial DXCollectionView? SongsColView { get; set; }
    [ObservableProperty]
    public partial DXCollectionView SongsColViewNPQ { get; set; } // Nullable, ensure it's set from XAML
    [RelayCommand]
    void ScrollToSong()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            int itemHandle = SongsColView.FindItemHandle(CurrentPlayingSongView);
            SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
        });
    }
    [RelayCommand]
    void ScrollToSongNowPlayingQueue()
    {
        if(PlaybackQueueColView is null)
        {
            return;
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            int itemHandle = PlaybackQueueColView.FindItemHandle(CurrentPlayingSongView);
            PlaybackQueueColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
        });
    }


    public void LoadTheCurrentColView(DXCollectionView colView)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (colView is not null)
            {
                SongsColView = colView;
                // Optionally, you can also set the current item to scroll to it.
                int itemHandle = SongsColView.FindItemHandle(CurrentPlayingSongView);
                SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
            }
        });
    }

    #region INotifyPropertyChanged
    public new event PropertyChangedEventHandler PropertyChanged;
    protected new bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {

        this.Dispose();
    }

  
    #endregion


    public async Task InitializeDimmerLiveData()
    {
        _loginViewModel.Username=UserLocal.Username;
        await _loginViewModel.InitializeAsync();
    }
    protected override async Task ProcessSongChangeAsync(SongModelView value)
    {
        // 1. Let the base class do all of its work first.
        await base.ProcessSongChangeAsync(value);


        if (value.IsCurrentPlayingHighlight)
        {

            _logger.LogInformation($"Song changed and highlighted in ViewModel B: {value.Title}");
            var itemHandle = SongsColView.FindItemHandle(value);

            MainThread.BeginInvokeOnMainThread(() =>
            {

                PlaybackQueueColView?.ScrollTo(itemHandle, DXScrollToPosition.MakeVisible);

            });
        }
    }

    internal void ScrollColViewToStart(SongModelView? songModelView=null)
    {
        if (songModelView is not null)
        {
            songModelView = CurrentPlayingSongView ;
        }
        int itemHandle = SongsColView.FindItemHandle(songModelView);
        SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
    }

    public async Task ProcessAndMoveToViewSong(SongModelView? selectedSec)
    {
        if (selectedSec is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong=CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongsColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong=selectedSec;
        }
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
    }


    [ObservableProperty]
    public partial BottomSheet QuickPanelBtmSht { get; set; } 

    [ObservableProperty]
    public partial DXExpander MainViewExp { get; set; }
    [ObservableProperty]
    public partial DXCollectionView? PlaybackQueueColView { get; internal set; }


    partial void OnSongsColViewChanged(DXCollectionView? oldValue, DXCollectionView? newValue)
    {
    
        if(newValue is not null)
        {
            newValue.GroupCollapsed += SongsColView_GroupCollapsed;
            newValue.DragItem +=SongsColView_DragItem;
            newValue.DragItemOver += SongsColView_DragItemOver;
            newValue.DropItem += SongsColView_DropItem;
            newValue.FilteringUIFormShowing += SongsColView_FilteringUIFormShowing;
            newValue.PullToRefresh +=SongsColView_PullToRefresh;
            newValue.Scrolled += SongsColView_Scrolled;
            newValue.ValidateAndSave +=SongsColView_ValidateAndSave;
            newValue.SwipeItemShowing +=SongsColView_SwipeItemShowing;
            newValue.SelectionChanged += SongsColView_SelectionChanged;
        }
    }

    private async void SongsColView_SelectionChanged(object? sender, CollectionViewSelectionChangedEventArgs e)
    {
      DXCollectionView send=sender as DXCollectionView;


        // --- Multi-Select Mode ---
        // If we're in a multi-select state, we just update the selection list.
        if (IsInMultiSelectMode)
        {
            // AddedItems contains what the user just tapped on.
            foreach (var item in e.AddedItems.Cast<SongModelView>())
            {
                //ToggleMultiSelectItemCommand.Execute(item);
            }
            // RemovedItems contains what the user just UN-tapped.
            foreach (var item in e.RemovedItems.Cast<SongModelView>())
            {
                //ToggleMultiSelectItemCommand.Execute(item);
            }
            return; // Don't play the song in multi-select mode.
        }

        // --- Single-Select Mode (Standard Playback) ---
        var songToPlay = e.AddedItems.FirstOrDefault() as SongModelView;
        if (songToPlay == null)
            return;

        // This is the most important call. It tells the VM to start playback
        // with this song as the starting point, and the current search results
        // (_searchResults in your VM) as the context for the new queue.
        await PlaySong(songToPlay, CurrentPage.AllSongs
            );

        Debug.WriteLine(send.SelectedItems.GetType());

     // Deselect the item visually so it can be tapped again.
     //send.SelectedItems.Cast<SongModelView>().DeselectItem(songToPlay);
    }

    private void SongsColView_SwipeItemShowing(object? sender, SwipeItemShowingEventArgs e)
    {
        DXCollectionView send = sender as DXCollectionView;
        var w = e.SwipeItem as SwipeItemBase;
        var song = e.Item as SongModelView;
        var swipee = e.RowHandle;

        var addEndAction = new SwipeContainerItem()
        {
            Caption = "Add to End",
            BackgroundColor = Colors.DarkGreen,
            Command = AddToQueueEndCommand, // Assuming you have this command
            CommandParameter = new List<SongModelView> { song } // Pass a list
        };

        // 2. Add to Next in Queue
        var addNextAction = new SwipeContainerItem()
        {
            Caption = "Play Next",
            BackgroundColor = Colors.RoyalBlue,
            Command = AddToNextCommand,
            CommandParameter = new List<SongModelView> { song }
        };

        // --- END SWIPE (usually for neutral/destructive actions) ---

        // 3. Edit Metadata
        var editAction = new SwipeContainerItem()
        {
            Caption = "Edit",
            BackgroundColor = Colors.Orange,
            Command = send.Commands.ShowDetailEditForm, // A new command that navigates to an edit page
            CommandParameter = song
        };

        // 4. Find More Like This (Powerful Discovery!)
        var findSimilarAction = new SwipeContainerItem()
        {
            Caption = "More Like This",
            BackgroundColor = Colors.Purple,
            // We can call a method directly or use a command.
            // This is a "power method" that constructs a TQL query.
            Command = new RelayCommand(() =>
            {
                // Creates a search for songs of the same genre and similar BPM.
                string similarQuery = $"genre:\"{song.GenreName}\" and bpm:{song.BPM - 10}-{song.BPM + 10}";
                SearchSongSB_TextChangedCommand.Execute(similarQuery);
            })
        };
    }

    private void SongsColView_ValidateAndSave(object? sender, ValidateItemEventArgs e)
    {
        var ee = e.Item as SongModelView;
        var s = e.Context;
        
        // The e.Item is the SongModelView with the *new, edited* values.
        var editedSong = e.Item as SongModelView;
        if (editedSong == null)
        {
            e.IsValid = false;
            return;
        }

        // --- Perform Validation ---
        if (string.IsNullOrWhiteSpace(editedSong.Title))
        {
            e.IsValid = false;
            return;
        }

        if (editedSong.ReleaseYear is < 1000 or > 3000)
        {
            e.IsValid = false;
            return;
        }

        // --- If Valid, Save the Changes ---
        e.IsValid = true;
        e.ForceUpdateItemsSource(); // Tell DX to commit the change visually.

        // Now, call your ViewModel's persistence logic.
        if (ApplyNewSongEditsCommand.CanExecute(editedSong))
        {
             ApplyNewSongEditsCommand.ExecuteAsync(editedSong);
        }

        //Debug.WriteLine(s.GetType().Name);
        //DataChangeType DTType = e.DataChangeType;
        //switch (DTType)
        //{
        //    case DataChangeType.Add:
        //        break;
        //    case DataChangeType.Edit:
        //        break;
        //    case DataChangeType.Delete:
        //        break;
        //    default:
        //        break;
        //}
        //var ss = e.SourceIndex;
        //e.ForceUpdateItemsSource();

        // can be used to call save song
        //base.UpdateSongArtist

    }

    private void SongsColView_Scrolled(object? sender, DXCollectionViewScrolledEventArgs e)
    {
        var ee = e.ViewportSize;
        var aw = e.Delta;
        var ss = e.ExtentSize;
        var sx = e.FirstVisibleItemHandle;
        var sy = e.LastVisibleItemHandle;
        var pos = e.FirstVisibleItemIndex;
        var lastVisibleItemIndex = e.LastVisibleItemIndex;
        var offset = e.Offset;

    }

    private void SongsColView_PullToRefresh(object? sender, EventArgs e)
    {
        
    }

    private void SongsColView_FilteringUIFormShowing(object? sender, FilteringUIFormShowingEventArgs e)
    {
        var s = e.ViewModel;
        var ss = e.Form;
        
    }


    private void SongsColView_DragItemOver(object? sender, DropItemEventArgs e)
    {
        DXCollectionView send = sender as DXCollectionView;
        var draggedItem = e.DragItem as SongModelView;
        var draggedItemHandle = e.ItemHandle;
        var Cancel = e.Cancel;

        var dropItemHandle = e.DropItemHandle;  
        var dragItemposInSource = send.GetItemSourceIndex(draggedItemHandle);
        var targetItem = e.DropItem as SongModelView;
        var dropItemposInSource = send.GetItemSourceIndex(dropItemHandle);


        if (draggedItem == null || targetItem == null || draggedItem.Id == targetItem.Id)
        {
            e.Cancel = true; // Don't allow dropping onto itself or invalid items
            return;
        }

        // IDEA: If you drag a song onto another song from a DIFFERENT artist,
        // we can interpret that as "Show me a playlist blending these two artists."
        if (draggedItem.ArtistName != targetItem.ArtistName)
        {
            // Provide visual feedback - maybe change the row color. This is harder in MVVM.
            // For now, we'll just allow the drop.
            e.Cancel = false; // Allow the drop
        }
        // IDEA: If you drag onto a song from the SAME artist,
        // we can interpret that as "Group these songs into an album."
        else if (draggedItem.ArtistName == targetItem.ArtistName)
        {
            e.Cancel = false; // Allow the drop
        }
        else
        {
            e.Cancel = true; // Disallow drops in other cases for clarity.
        }
    }

    private async void SongsColView_DropItem(object? sender, DropItemEventArgs e)
    {

        int DropItemHandle = e.DropItemHandle;
        int itemHandle = e.ItemHandle;
        SongModelView dropItem = e.DropItem as SongModelView;
        SongModelView dragItem = e.DragItem as SongModelView;
        var Cancel = e.Cancel;

    e.Cancel = true; // ALWAYS cancel the default DX behavior. We will handle the logic.

        // Get the dragged song(s) and the target song.
        var draggedSongs = IsInMultiSelectMode
            ? MultiSelectSongs.ToList()
            : new List<SongModelView> { e.DragItem as SongModelView };

        var targetSong = e.DropItem as SongModelView;

        if (!draggedSongs.Any() || targetSong == null)
            return;

        // --- Logic based on the DragItemOver checks ---

        // SCENARIO 1: Blend Artists
        if (draggedSongs.First().ArtistName != targetSong.ArtistName)
        {
            string choice = await Shell.Current.DisplayActionSheet(
                "Create Blend?",
                "Cancel",
                null,
                $"Create a playlist with {draggedSongs.First().ArtistName} and {targetSong.ArtistName}"
            );

            if (choice.StartsWith("Create"))
            {
                // Another "power method" call!
                string blendQuery = $"artist:\"{draggedSongs.First().ArtistName}\" or artist:\"{targetSong.ArtistName}\" shuffle";
                SearchSongSB_TextChangedCommand.Execute(blendQuery);
            }
        }
        // SCENARIO 2: Group into Album
        else if (draggedSongs.First().ArtistName == targetSong.ArtistName)
        {
            string choice = await Shell.Current.DisplayActionSheet(
                "Group Songs?",
                "Cancel",
                null,
                "Group selected songs into a new album"
            );
            if (choice.StartsWith("Group"))
            {
                // Combine the dragged songs and the target into one list to pass to the command.
                var allSongsToGroup = new List<SongModelView>(draggedSongs);
                if (!allSongsToGroup.Contains(targetSong))
                {
                    allSongsToGroup.Add(targetSong);
                }

                // Your existing powerful command does the rest!
                GroupSongsIntoAlbumCommand.Execute(allSongsToGroup);
            }
        }
    }

    private void SongsColView_DragItem(object? sender, DragItemEventArgs e)
    {
        var s = e.DragItem as SongModelView;
        var itemHandle = e.ItemHandle;
        var Cancel = e.Cancel;

    }

    private void SongsColView_GroupCollapsed(object? sender, DevExpress.Maui.CollectionView.ItemEventArgs e)
    {
        DXCollectionView cv = sender as DXCollectionView;
        var ee = e.ItemHandle;
        var item = cv.GetItem(ee);
    }

    [ObservableProperty]
    public partial Microsoft.Maui.Controls.View SelectedSongView { get; internal set; }

    [ObservableProperty]
    public partial bool IsInMultiSelectMode { get; internal set; }

    [ObservableProperty]
    public partial bool IsSongLongPressed { get; set; }

    public void HandleSongLongPress(Microsoft.Maui.Controls.View view)
    {
        if(SongsColView is null)
        {
            return;
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {


            if (SongsColView.SelectionMode !=SelectionMode.Multiple)
            {
                SongsColView.SelectionMode=SelectionMode.Multiple;
                HandleSongSongMultiSelect(view);
                return;
            }
            SelectedSongView=view;
            IsSongLongPressed=true;

            MultiSelectViewsOfSongs.Add(view);
            // You can now use 'collectionOfLongPressedViews' as needed.
        _logger.LogInformation("Song long-pressed, view captured.");
        });

    }
    public void HandleSongSongMultiSelect(Microsoft.Maui.Controls.View view)
    {
        SelectedSongView=view;
        var selectedSong = SongsColView.SelectedItem as SongModelView;
        IsSongLongPressed=true;

        if (!MultiSelectViewsOfSongs.Contains(view))
        {

            MultiSelectViewsOfSongs.Add(view);
            
            if(!MultiSelectSongs.Contains(selectedSong))
            {
                
            MultiSelectSongs.Add(selectedSong);
            }

        }
        // You can now use 'collectionOfLongPressedViews' as needed.
        _logger.LogInformation("Song multi-selected, view captured.");
        var collectionOfSelectedViews = new List<Microsoft.Maui.Controls.View>();
        collectionOfSelectedViews.Add(view);
    }
    [ObservableProperty]
    public partial ObservableCollection<Microsoft.Maui.Controls.View> MultiSelectViewsOfSongs { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> MultiSelectSongs { get; set; } = new();
    [ObservableProperty]
    public partial HomePage? MyHomePage { get; internal set; }
    partial void OnMyHomePageChanged(HomePage? oldValue, HomePage? newValue)
    {
        if (newValue is not null)
        {
            SongsColView =  newValue.SongsColView;
        }
        else
        {
            SongsColView =  null;

        }
    }

    protected override void HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        // STEP 1: Always a good practice to let the base class do its work first.
        // This will run the logic in A (setting IsPlaying, etc.).
        base.HandlePlaybackStateChange(args);


        PlayType? state = StatesMapper.Map(args.EventType);

        if (state == PlayType.Play)
        {
            // Do something that ONLY ViewModel B cares about.
            // For example, maybe B is the VM for a mini-player and needs to
            // trigger a specific animation.
            TriggerMiniPlayerGlowAnimation();
            _logger.LogInformation("Playback started, ViewModel B is reacting specifically.");
        }
        else if (state == PlayType.Pause)
        {
            // Stop the animation.
            StopMiniPlayerGlowAnimation();
        }
    }

    private void StopMiniPlayerGlowAnimation()
    {
        //throw new NotImplementedException();
    }

    private void TriggerMiniPlayerGlowAnimation()
    {
        //throw new NotImplementedException();
    }


    public async Task ShareSongViewClipboard(SongModelView song)
    {

        var byteData = await ShareCurrentPlayingAsStoryInCardLikeGradient(song, true);

        if (byteData.imgBytes != null)
        {
            string clipboardText = $"{song.Title} - {song.ArtistName}\nAlbum: {song.AlbumName}\n\nShared via Dimmer Music Player v{CurrentAppVersion}";

             await Clipboard.Default.SetTextAsync(clipboardText);

        }
    }
}