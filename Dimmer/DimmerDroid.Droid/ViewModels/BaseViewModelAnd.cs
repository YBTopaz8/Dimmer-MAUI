using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Storage;
using DevExpress.Maui.Controls;
using DevExpress.Maui.Editors;
using Dimmer.Data;
using Dimmer.Interfaces;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Dimmer.Utilities.StatsUtils;
using Google.Android.Material.Dialog;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Label = Microsoft.Maui.Controls.Label;

namespace Dimmer.ViewModels;

public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    public BaseViewModelAnd(
        IDimmerStateService dimmerStateService,
        MusicDataService musicDataService,
        IAppInitializerService appInitializerService,
        IDimmerAudioService audioServ,
        ISettingsService settingsService,
        ILyricsMetadataService lyricsMetadataService,
        SubscriptionManager subsManager,
        LyricsMgtFlow lyricsMgtFlow,
        ICoverArtService coverArtService,
        IFolderMgtService folderMgtService,
        IRepository<SongModel> songRepo,
        IDuplicateFinderService duplicateFinderService,
        ILastfmService lastfmService,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumModel,
        IRepository<GenreModel> genreModel,
        IDialogueService dialogueService,
        IRepository<PlaylistModel> playlistRepo,
        IRealmFactory realmFact,
        IFolderMonitorService folderServ,
        ILibraryScannerService libScannerService,
        IRepository<DimmerPlayEvent> dimmerPlayEventRepo,
        BaseAppFlow baseAppClass,
        ILogger<BaseViewModel> logger
        ,

        DimmerBackupService backupService
        ,IFolderPicker picker



        ) : base(
        dimmerStateService,
        musicDataService,
        appInitializerService,
        audioServ,
        settingsService,
        lyricsMetadataService,
        subsManager,
        lyricsMgtFlow,
        coverArtService,
        folderMgtService,
        songRepo,
        duplicateFinderService,
        lastfmService,
        artistRepo,
        albumModel,
        genreModel,
        dialogueService,
        playlistRepo,
        realmFact,
        folderServ,
        libScannerService,
        dimmerPlayEventRepo,
        baseAppClass,
        logger)
    {


        fPicker = picker;
        BackupService = backupService;
        this._logger = new LoggerFactory().CreateLogger<BaseViewModelAnd>();
        isAppBooting = true;
        this._logger.LogInformation("BaseViewModelAnd initialized.");
        audioService = audioServ;

        Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
    }
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
    public partial int NowPlayingTabIndex { get; set; }

    [ObservableProperty]
    public partial bool NowPlayingUI { get; set; }

    partial void OnNowPlayingTabIndexChanged(int oldValue, int newValue)
    {

        switch (newValue)
        {
            case 0:
                IsNowPlayingQueue = false;
                IsNowAllSongsQueue = true;
                NowPlayingUI = false;

                break;
            case 1:


                IsNowPlayingQueue = false;

                IsNowAllSongsQueue = false;
                NowPlayingUI = true;

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
    public partial bool IsNowAllSongsQueue { get; set; } = true;




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
                await AddMusicFolderByPassingToService(folderPath);
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
    public partial Microsoft.Maui.Controls.View SelectedSongView { get; internal set; }

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
    public async Task DeleteFileFromSystem(SongModelView song)
    {
        bool confirm = await Shell.Current.DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{song.Title}' from your device? This action cannot be undone.", "Delete", "Cancel");
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

                }

                await Shell.Current.DisplayAlert("Deleted", $"'{song.Title}' has been deleted from your device.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete '{song.Title}': {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]

    public async Task ShareSongViewClipboard(SongModelView song)
    {


        var byteData = await ShareCurrentPlayingAsStoryInCardLikeGradient(song, true);

        string clipboardText = $"{song.Title} - {song.ArtistName}\nAlbum: {song.AlbumName}\n\nShared via Dimmer Music Player v{CurrentAppVersion}";

        if (byteData.imgBytes != null)
        {
            //await Clipboard.SetTextAsync(clipboardText);


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

    [RelayCommand]
    public async Task LoadFolderToScanForBackUpFiles()
    {
        try
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

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
   
    internal async Task RestoreAppDataAsync()
    {
        var tcs = new TaskCompletionSource<(bool includeDefault, string customPath)>();
        DXPopup backUpPopup = new DXPopup();
        backUpPopup.Content = new Label() { Text = "Do you want to select a folder to restore backup from? If no, it will look for backups in default location." };
       

        // Wait for user's decision
        var (includeDefaultLocation, secondaryPath) = await tcs.Task;

        // Now proceed with backup restoration
        var files = await BackupService.GetBackupFilesAsync(secondaryPath, includeDefaultLocation);

        if (files.Count != 0)
        {
            // TODO: Implement file picker for multiple backups
            var selectedFile = files.First();
            var result = await BackupService.RestoreFromBackupAsync(selectedFile);

            if (result.EventsRestored > 0)
            {
                Debug.WriteLine(result.ToString());
            
            }
            else
            {
                Debug.WriteLine($"Restore failed: {result.ErrorMessage}");
            
            }
        }

        BackupService.CleanupOldBackups(3);
    }

    internal async Task BackUpAppDataAsync()
    {
       


        //_ = BackupService.CreateCompleteBackupAsync(path);
    }

    public override void UpdateIsSearchResultEmpty(bool isSearchResultEmpty)
    {
        if (SearchBarTextEdit is null) return;
        var newCount = SearchResults.Count;
        if (IsSearchResultEmpty)
        {
            SearchBarTextEdit.Prefix = "🔍";
            
            SearchBarTextEdit.Suffix = string.Empty;


        }
        else
        {


            string fullStr = newCount.ToString();
            SearchBarTextEdit.Suffix = fullStr;
            SearchBarTextEdit.Prefix = string.Empty;


        }
    }
    TextEdit? SearchBarTextEdit;
    public void SubscribeToPlayCount(TextEdit searchBarTextEdit)
    {
        SearchBarTextEdit = searchBarTextEdit;
        var newCount = SearchResults.Count;
        if (!IsSearchResultEmpty)
        {


            string fullStr = newCount.ToString();
            SearchBarTextEdit.Suffix = fullStr;
            SearchBarTextEdit.Prefix = string.Empty;


        }
        else
        {
            //SearchBarTextEdit.Prefix = "🔍";
            SearchBarTextEdit.StartIcon = new FontImageSource
            {
                FontFamily = "Segoe Fluent Icons",
                Glyph = "\uE721",
                Size = 16
            };
            SearchBarTextEdit.IsStartIconVisible = true;
            SearchBarTextEdit.Suffix = string.Empty;


        }
    }
    public void ClearSubscriptionToSearchBar()
    {
        SearchBarTextEdit = null;
    }
}