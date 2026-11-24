using AndroidX.Lifecycle;
using CommunityToolkit.Maui.Storage;
using DevExpress.Maui.CollectionView;
using Dimmer.Data;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Dimmer.Utilities.StatsUtils;
using DynamicData;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TextEdit = DevExpress.Maui.Editors.TextEdit;
namespace Dimmer.ViewModels;

public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{


    private readonly IDimmerAudioService audioService;
    private readonly IRepository<SongModel> songRepository;
    IFolderPicker fPicker;
    DimmerBackupService BackupService;

    private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        //throw new NotImplementedException();
    }



    [ObservableProperty]
    public partial int NowPlayingQueueItemSpan { get; set; }

    [ObservableProperty]
    public partial int HomePageIndex { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsTopBarVisible { get; set; } = true;

    partial void OnHomePageIndexChanged(int oldValue, int newValue)
    {

        switch (newValue)
        {
            case 0:
                IsTopBarVisible = true;
            break;
            case 1:
            case 2:
                IsTopBarVisible = false;
                break;
            default:
                break;
        }
    }




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



    [RelayCommand]
    public static async Task OpenFileInFolder(string filePath)
    {
        Uri uriF = new Uri(filePath);
        if (await Launcher.Default.CanOpenAsync(uriF))
        {

            await Launcher.Default.OpenAsync(new OpenFileRequest()
            {
                File = new ReadOnlyFile(filePath),
                Title = "Open with",

            });
        }
    }





    public async Task AddMusicFolderViaPickerAsync()
    {

        try
        {
            if (fPicker is null) return;
            // 2. Call it (No need to request permissions first, the picker handles the grant)
            //var selectedFolderPath = await fPicker.PickAsync();
            var pickerResult = await FolderPicker.Default.PickAsync();
            pickerResult.EnsureSuccess();
            if(pickerResult is not null && pickerResult.IsSuccessful)
            { 

                var folderName = pickerResult.Folder?.Name ?? "Unknown Folder";
                var folderPath = pickerResult.Folder?.Path ?? string.Empty;

                // Pass to your logic
                AddMusicFolderByPassingToService(folderPath);
            }
            else
            {
                _logger.LogInformation("No folder selected.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Native Picker Failed");

            // Native Alert
            MainApplication.CurrentActivity?.RunOnUiThread(() =>
            {
                var materialDialog = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(MainApplication.CurrentActivity)?
                    .SetTitle("Error")?
                    .SetMessage(ex.Message)?
                    .SetPositiveButton("OK", (s, e) => { })
                    .Show();


            });
        }
    }
    internal void ViewArtistDetails(ArtistModelView? s)
    {
        ViewArtistDetails(s);
    }


    public async Task InitializeDimmerLiveData()
    {
        //_loginViewModel.Username = CurrentUserLocal.Username;
        //await _loginViewModel.InitializeAsync();
    }



    [ObservableProperty]
    public partial bool IsInMultiSelectMode { get; internal set; }

    [ObservableProperty]
    public partial bool IsSongLongPressed { get; set; }


    [RelayCommand]
    public void AddArtistToTQL(string ArtistName)
    {
        string tqlClause = $"{CurrentTqlQuery} add artist:\"{ArtistName}\"";
        AddFilterToSearch(tqlClause);
    }
    [RelayCommand]
    public void RemoveArtistFromTQL(string artistName)
    {
        if (string.IsNullOrWhiteSpace(CurrentTqlQuery) || string.IsNullOrWhiteSpace(artistName))
            return;

        string query = CurrentTqlQuery;

        // Escape artist name safely
        string escapedArtist = Regex.Escape(artistName);

        // 1️⃣ Remove the last occurrence of artist:"X" (deepest/rightmost)
        // Use regex with rightmost match
        string pattern = $@"(?i)(.*)(artist:\s*{escapedArtist})(.*)";
        if (Regex.IsMatch(query, pattern))
        {
            // Keep everything except the matched artist term
            query = Regex.Replace(query, $@"(?i)(\s*[\(\)]*\s*)(artist:\s*{escapedArtist})", "", RegexOptions.RightToLeft, new TimeSpan(1));
        }

        // 2️⃣ Clean up logical operators left hanging (and/or)
        query = Regex.Replace(query, @"\s*(and|or)\s*(and|or)\s*", " ", RegexOptions.IgnoreCase);
        query = Regex.Replace(query, @"(^\s*(and|or)\s*)|(\s*(and|or)\s*$)", "", RegexOptions.IgnoreCase);

        // 3️⃣ Clean up empty parentheses and extra spaces
        query = Regex.Replace(query, @"\(\s*\)", "");
        query = Regex.Replace(query, @"\s{2,}", " ").Trim();

        // 4️⃣ Fix cases like 'include ()' or leftover operators before closing parenthesis
        query = Regex.Replace(query, @"include\s*\(\s*\)", "", RegexOptions.IgnoreCase);
        query = Regex.Replace(query, @"\(\s*(and|or)\s*\)", "", RegexOptions.IgnoreCase);

        // 5️⃣ Assign and search
        CurrentTqlQuery = query.Trim();
        SearchToTQL(query);
    }

    [RelayCommand]
    public void AddAlbumToTQL(string AlbumName)
    {
        string tqlClause = $"{CurrentTqlQuery} add album:\"{AlbumName}\"";
        AddFilterToSearch(tqlClause);
    }

    protected override async Task HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        // STEP 1: Always a good practice to let the base class do its work first.
        // This will run the logic in A (setting IsPlaying, etc.).
        await base.HandlePlaybackStateChange(args);


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

    [RelayCommand]
    public async Task DeleteFileFromSystemAsync(SongModelView? song)
    {
        if (song is null) return;
        bool confirm = await Shell.Current.DisplayAlertAsync("Confirm Delete", $"Are you sure you want to delete '{song.Title}' from your device? This action cannot be undone.", "Delete", "Cancel");
        if (confirm)
        {
            try
            {
                if (TaggingUtils.FileExists(song.FilePath))
                {
                    await RemoveFromQueue(song);
                    var songsToDelete = new List<SongModelView> { song };
                    await PerformFileOperationAsync(songsToDelete, string.Empty, FileOperation.Delete);
                    // Then, remove from the database.
                    await songRepository.DeleteAsync(song.Id);
                    // Optionally, you might want to refresh your UI or notify other components here.
                    SearchResultsHolder.Edit(updater =>
                    {
                        var indx = updater.IndexOf(song);
                        if(indx>0)
                            updater.Remove(song);

                    });
                }

                await Shell.Current.DisplayAlertAsync("Deleted", $"'{song.Title}' has been deleted from your device.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"Failed to delete '{song.Title}': {ex.Message}", "OK");
            }
        }
    }

  

    #region Binding Views Section

    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);





    protected override async Task ProcessSongChangeAsync(SongModelView value)
    {
        await base.ProcessSongChangeAsync(value);
        _currentSong.OnNext(value);
    }



    #endregion

    

    internal void ToggleOpenMediaUIOnNotificationTap(bool v)
    {
        var realm = RealmFactory.GetRealmInstance();
        var currentAppModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (currentAppModel != null)
        {
            realm.Write(() =>
            {
                currentAppModel.OpenMediaUIOnNotificationTap = v;
            });
            OpenMediaUIOnNotificationTap = v;
        }
    }

    [ObservableProperty]
    public partial bool IsBackUpDone { get; set; }
    public async Task<bool> StageRealmRestoreAsync(string pickedFilePathOrUri)
    {
        try
        {
            string targetDbPath;
            using (var realm = RealmFactory.GetRealmInstance())
                targetDbPath = realm.Config.DatabasePath;

            string stagedPath = targetDbPath + ".restore_pending";

            await using Stream? input = pickedFilePathOrUri.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
                ? TaggingUtils.PlatformGetStreamHook?.Invoke(pickedFilePathOrUri)
                : File.OpenRead(pickedFilePathOrUri);

            if (input == null) return false;

            await using (var output = File.Create(stagedPath))
                await input.CopyToAsync(output);

            // Validate it is a readable Realm database before accepting it
            var testConfig = new Realms.RealmConfiguration(stagedPath) { IsReadOnly = true };
            using var testRealm = Realms.Realm.GetInstance(testConfig);

            return true; // Tell UI: "Backup staged! Please restart the app to finish restoring."
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"StageRealmRestoreAsync failed: {ex}");
            return false;
        }
    }

    // 2. Call this ONCE at very early app startup BEFORE RealmFactory.GetRealmInstance() is ever called
    public static void ApplyPendingRealmRestoreIfAny(string defaultRealmPath)
    {
        string stagedPath = defaultRealmPath + ".restore_pending";
        if (!File.Exists(stagedPath)) return;

        // Remove Realm sidecar files (.lock, .note, .management) so the swapped DB opens cleanly
        foreach (var ext in new[] { ".lock", ".note" })
        {
            var sidecar = defaultRealmPath + ext;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
        var mgmtDir = defaultRealmPath + ".management";
        if (Directory.Exists(mgmtDir)) Directory.Delete(mgmtDir, recursive: true);

        File.Move(stagedPath, defaultRealmPath, overwrite: true);
    }

    [ObservableProperty]
    public partial bool IsNowPlayingBtmSheetOpened { get; set; }
    
    public override void UpdateIsSearchResultEmpty(bool isSearchResultEmpty)
    {
        if (SearchBarTextEdit is null) return;
        var newCount = SearchResults.Count;
        if (IsSearchResultEmpty)
        {
            SearchBarTextEdit.Prefix = "🔍";
            


        }
        else
        {


            SearchBarTextEdit.Prefix = string.Empty;


        }
    }
    TextEdit? SearchBarTextEdit;

    public BaseViewModelAnd(IDimmerStateService dimmerStateService,
        IFolderPicker picker,
    DimmerBackupService backupService
        , MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> songRepo, IDuplicateFinderService duplicateFinderService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> playlistRepo, IRealmFactory realmFact, IFolderMonitorService folderServ, ILibraryScannerService libScannerService, IRepository<DimmerPlayEvent> dimmerPlayEventRepo, BaseAppFlow baseAppClass, ILastfmService lastfmService, ILogger<BaseViewModel> logger) : base(backupService,dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, songRepo, duplicateFinderService, artistRepo, albumModel, genreModel, dialogueService, playlistRepo, realmFact, folderServ, libScannerService, dimmerPlayEventRepo, baseAppClass, lastfmService, logger)
    {
        fPicker = picker;
        BackupService = backupService;
        this._logger = new LoggerFactory().CreateLogger<BaseViewModelAnd>();
        isAppBooting = true;
        this._logger.LogInformation("BaseViewModelAnd initialized.");
        audioService = audioServ;

        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
    }

    private DXCollectionView _collectionView;

    
    [ObservableProperty]
    public partial string CurrentSortDisplay { get; set; } = "Title";

    [ObservableProperty]
    public partial string CurrentFilterDisplay { get; set; } = "None";
 
    public void SetCollectionView(DXCollectionView collectionView)
    {
        _collectionView = collectionView;
      

    }
 

    [RelayCommand]
    private async Task ShowFilterPopup()
    {
        if (_collectionView != null)
        {
            var popup = new FilterSortPopup(_collectionView, this);
            await popup.ShowAsync();
        }
    }


    [RelayCommand]
    private void ApplyTQLSearch(string? parameter)
    {
        base.SearchToTQL(parameter);
    }

    public void ClearSubscriptionToSearchBar()
    {
        SearchBarTextEdit = null;
    }

    [RelayCommand]
    void SeekToPosition(double position)
    {
        IsProgrammaticSeek = true;
        base.SeekTrackPositionAsync(position);
    }

    [RelayCommand]
    void UpdatePreviewTime(double position)
    {
        //PreviewTimeLabel = (position).ToString("mm:ss");
    }

    [RelayCommand]
    void DragPreview(double position)
    {

    }

    [RelayCommand]
    void UserChangedTrackPosition(double position)
    {

    }


}