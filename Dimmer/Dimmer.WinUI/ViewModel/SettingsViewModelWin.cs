using CommunityToolkit.Maui.Storage;
using Dimmer.Utils;
using Realms;

namespace Dimmer.WinUI.ViewModel;

public partial class SettingsViewModelWin : SettingsViewModel
{
    public BaseViewModelWin WinBaseVM { get; }
    public SettingsViewModelWin(BaseViewModelWin BaseVM, IDimmerStateService dimmerStateService, DimmerBackupService backupService, MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> songRepo, IDuplicateFinderService duplicateFinderService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> playlistRepo, IRealmFactory realmFact, IFolderMonitorService folderServ, ILibraryScannerService libScannerService, IRepository<DimmerPlayEvent> dimmerPlayEventRepo, BaseAppFlow baseAppClass, ILastfmService lastfmService, ILogger<BaseViewModel> logger) : base(dimmerStateService, backupService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, songRepo, duplicateFinderService, artistRepo, albumModel, genreModel, dialogueService, playlistRepo, realmFact, folderServ, libScannerService, dimmerPlayEventRepo, baseAppClass, lastfmService, logger)
    {
        WinBaseVM=BaseVM;
    }

    [RelayCommand]
    public async Task RestoreCompleteDataAsync()
    {

        MyRestoredResult = new();
        if (PickedUpBackup is not null)
            MyRestoredResult = await BackupService.RestoreCompleteDataAsync(PickedUpBackup, MyRestoredResult);
        IsRestoreDone = MyRestoredResult.Success;
        PickedUpBackup = null;
        var redoStats = new StatsRecalculator(RealmFactory, _logger);
        _ = redoStats.RecalculateAllStatisticsAsync();
    }
    [RelayCommand]
    public async Task PickFolderToRestoreAppDataAsync()
    {
        var tcs = new TaskCompletionSource<(bool includeDefault, string customPath)>();


        ContentDialog restoreFolderPickerContentDialog = new ContentDialog
        {
            Title = "Restore Backup",
            Content = "Do you want to select a folder to restore backup from? If no, it will look for backups in default location.",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = WinBaseVM.MainWindow.Content.XamlRoot
        };

        restoreFolderPickerContentDialog.PrimaryButtonClick += async (s, e) =>
        {

            var picker = new FileOpenPicker();
            // Get the HWND of the current window
            var hwnd = PlatUtils.DimmerHandle;
            // Initialize the picker with the window handle
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".json");
            var file = await picker.PickSingleFileAsync();


            if (file is null)
            {
                tcs.SetResult((false, string.Empty));

                return;
            }
            tcs.SetResult((true, file.Path));
        };
        restoreFolderPickerContentDialog.CloseButtonClick += (s, e) =>
        {
            tcs.SetResult((false, string.Empty));
        };

        await restoreFolderPickerContentDialog.ShowAsync();

        // Wait for user's decision
        var (includeDefaultLocation, secondaryPath) = await tcs.Task;

        SelectedFile = secondaryPath;
        PickedUpBackup = await BackupService.PickFolderTeRestoreFromBackupAsync(SelectedFile);




        //BackupService.CleanupOldBackups(3);
    }

    internal async Task BackUpAppDataAsync()
    {
        var tcs = new TaskCompletionSource<bool>();


        ContentDialog restoreFolderPickerContentDialog = new ContentDialog
        {
            Title = "Backup Data",
            Content = "Do you want to select a folder for backup? If no, backup will be saved in default location.",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",

            XamlRoot = WinBaseVM.MainWindow.Content.XamlRoot
        };

        restoreFolderPickerContentDialog.PrimaryButtonClick += async (s, e) =>
        {
            Debug.WriteLine($"BackUp started at {DateTime.Now}");
            MyBackupResult = await BackupService.CreateCompleteBackupAsync(BaseViewModel.CurrentAppVersion);


            Debug.WriteLine($"BackUp ended at {DateTime.Now}");
            tcs.SetResult((true));
        };
        restoreFolderPickerContentDialog.CloseButtonClick += (s, e) =>
        {
            tcs.SetResult((false));
        };

        await restoreFolderPickerContentDialog.ShowAsync();
    }
    [ObservableProperty]
    public partial DimmerBackupService.BackUpCompleteResult? MyBackupResult { get; set; }
    internal void LoadApplicationDataForBackUpPage()
    {
        var realm = RealmFactory.GetRealmInstance();
        TotalNumberOfSongs = realm.All<SongModel>().Count();
        TotalNumberOfArtists = realm.All<ArtistModel>().Count();
        TotalNumberOfAlbums = realm.All<AlbumModel>().Count();
        TotalNumberOfGenres = realm.All<GenreModel>().Count();
        TotalNumberOfPlaylists = realm.All<PlaylistModel>().Count();
        TotalNumberOfDimmerEvents = realm.All<DimmerPlayEvent>().Count();
        

        TotalNumberOfFavoriteSongs = realm.All<SongModel>().Filter("IsFavorite == true").Count();
    }

    [ObservableProperty]
    public partial int TotalNumberOfSongs { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfArtists { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfAlbums { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfPlaylists { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfGenres { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfFavoriteSongs { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfDimmerEvents { get; set; }
}