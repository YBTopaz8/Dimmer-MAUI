// --- START OF FILE BaseViewModelWin.cs ---

using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using CommunityToolkit.Maui.Storage;
using CommunityToolkit.WinUI;

using Dimmer.Data.Models;
using Dimmer.Data.ModelView.DimmerSearch;
using Dimmer.DimmerSearch.TQL;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Dimmer.Orchestration;
using Dimmer.Resources.Localization;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinUIPages;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

using Windows.Graphics;
using Windows.Storage.Pickers;

using FieldType = Dimmer.DimmerSearch.TQL.FieldType;
using TableView = WinUI.TableView.TableView;
using Window = Microsoft.UI.Xaml.Window;

namespace Dimmer.WinUI.ViewModel;

public partial class BaseViewModelWin : BaseViewModel

{

    public readonly IMauiWindowManagerService windowManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    public readonly IWinUIWindowMgrService winUIWindowMgrService;
    private readonly LoginViewModel loginViewModel;
    private readonly IFolderPicker _folderPicker;
    public BaseViewModelWin(IMapper mapper, MusicDataService musicDataService, LoginViewModel _loginViewModel,
        IWinUIWindowMgrService winUIWindowMgrService,
        IMauiWindowManagerService mauiWindowManagerService,
         IDimmerStateService dimmerStateService, IFolderPicker _folderPicker,
         IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService,
         ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow,
         ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo,
         IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo,
         IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel,
         IDialogueService dialogueService, ILogger<BaseViewModel> logger) : base(mapper, dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, logger)
    {

        this.winUIWindowMgrService = winUIWindowMgrService;
        this.loginViewModel = _loginViewModel;
        this._folderPicker = _folderPicker;
        UIQueryComponents.CollectionChanged += (s, e) =>
        {
            RebuildAndExecuteQuery();
        };
        windowManager = mauiWindowManagerService;
        AddNextEvent += BaseViewModelWin_AddNextEvent;
        //MainWindowActivated
    }

    public void MainMAUIWindow_Activated()
    {
        //MainWindowActivated?.Invoke(this, EventArgs.Empty);
        MainWindow_Activated();


    }

    private void BaseViewModelWin_AddNextEvent(object? sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var win = winMgr.GetOrCreateUniqueWindow(this, windowFactory: () => new AllSongsWindow(this));
        win?.Close();
        //// wait 4s and reopen it
        //await Task.Delay(4000);

        //var newWin = winMgr.GetOrCreateUniqueWindow(this, windowFactory: () => new AllSongsWindow(this));
        //newWin.Activate();

    }

    [RelayCommand]
    private void RemoveFilter(ActiveFilterViewModel filterToRemove)
    {
        if (filterToRemove == null)
            return;
        if (UIQueryComponents is null)
            return;
        int index = UIQueryComponents.IndexOf(filterToRemove);


        if (index == -1)
            return;

        if (index < UIQueryComponents.Count && UIQueryComponents[index] is LogicalJoinerViewModel)
        {
            UIQueryComponents.RemoveAt(index); // Remove joiner after
        }
        else if (index > 0 && UIQueryComponents[index - 1] is LogicalJoinerViewModel)
        {
            UIQueryComponents.RemoveAt(index - 1); // Remove joiner before
        }
    }


    public async Task AddFilterAsync(string tqlField)
    {
        if (string.IsNullOrWhiteSpace(tqlField) || !FieldRegistry.FieldsByAlias.TryGetValue(tqlField, out var fieldDef))
        {
            return;
        }

        // Prevent adding duplicate boolean filters (e.g., two "Is Favorite" chips)
        if (fieldDef.Type == FieldType.Boolean && UIQueryComponents.OfType<ActiveFilterViewModel>().Any(f => f.Field == tqlField))
        {
            return; // Or show a message
        }

        string? tqlClause = null;
        string? displayText = null;

        // Use a custom content dialog for a much better UX than DisplayPromptAsync
        var (clause, display) = await ShowFilterInputDialogAsync(fieldDef);
        tqlClause = clause;
        displayText = display;

        if (tqlClause != null && displayText != null)
        {
            // If there are already filters, add a joiner first
            if (UIQueryComponents?.Count > 0)
            {
                var tt = new LogicalJoinerViewModel(RebuildAndExecuteQuery) as IQueryComponentViewModel;
                UIQueryComponents.Add(tt);
            }

            var er = new ActiveFilterViewModel(tqlField, displayText, tqlClause, RemoveFilterCommand) as IQueryComponentViewModel;

            UIQueryComponents?.Add(er);
        }
    }

    // A helper method for showing a context-aware dialog. This is a huge UX improvement.
    private async Task<(string? Clause, string? Display)> ShowFilterInputDialogAsync(FieldDefinition fieldDef)
    {
        string? tqlClause = null;
        string? displayText = null;

        switch (fieldDef.Type)
        {
            case FieldType.Boolean:
                // No input needed for booleans
                tqlClause = $"{fieldDef.PrimaryName}:true";
                displayText = fieldDef.Description;
                break;

            case FieldType.Text:
                var textDialog = new ContentDialog
                {
                    Title = $"Filter by {fieldDef.PrimaryName}",
                    Content = new TextBox { PlaceholderText = "Enter text to search for..." },
                    PrimaryButtonText = "Add",
                    CloseButtonText = "Cancel",
                };

                if (await textDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    var valuee = ((TextBox)textDialog.Content).Text;
                    if (!string.IsNullOrWhiteSpace(valuee))
                    {
                        string formattedValue = valuee.Contains(' ') ? $"\"{valuee}\"" : valuee;
                        tqlClause = $"{fieldDef.PrimaryName}:{formattedValue}";
                        displayText = $"{fieldDef.PrimaryName}: {valuee}";
                    }
                }
                break;

            // You would create similar ContentDialogs for Numeric/Date types,
            // potentially with operator buttons (<, >, =), etc.
            // For now, let's keep it simple with a prompt.
            case FieldType.Numeric:
            case FieldType.Duration:
            case FieldType.Date:
                string? value = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter value (e.g., >2000, 3:30, ago(\"1y\"))");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    tqlClause = $"{fieldDef.PrimaryName}:{value}";
                    displayText = $"{fieldDef.PrimaryName} {value}";
                }
                break;
        }

        return (tqlClause, displayText);
    }
    private void RebuildAndExecuteQuery()
    {
        if (UIQueryComponents?.Count > 0)
        {
            GeneratedTqlQuery = string.Empty;
            _searchQuerySubject.OnNext(string.Empty);
            return;
        }

        var clauses = new List<string>();
        LogicalOperator nextJoiner = LogicalOperator.And;

        // This is the core logic that turns the visual chips into a TQL string
        foreach (var component in UIQueryComponents)
        {
            if (component is ActiveFilterViewModel filter)
            {
                // If this is not the first filter, add the preceding joiner (AND/OR)
                if (clauses.Count > 0)
                {
                    clauses.Add(nextJoiner.ToString().ToLower());
                }
                clauses.Add($"({filter.TqlClause})"); // Wrap clauses in parentheses for safety
            }
            else if (component is LogicalJoinerViewModel joiner)
            {
                // Store the operator for the *next* filter
                nextJoiner = joiner.Operator;
            }
        }

        var fullQueryString = string.Join(" ", clauses);
        GeneratedTqlQuery = fullQueryString; // Update the UI property
        _searchQuerySubject.OnNext(fullQueryString); // Execute the query
    }

    [ObservableProperty]
    public partial string GeneratedTqlQuery { get; set; }
    [ObservableProperty]
    public partial string PopUpHeaderText { get; set; }

    [ObservableProperty]
    public partial int MediaBarGridRowPosition { get; set; }

    [ObservableProperty]
    public partial CollectionView PlaybackQueueCV { get; set; }


    [ObservableProperty]
    public partial List<string> DraggedAudioFiles { get; set; }
    public Window CurrentWinUIPage { get; internal set; }

    [RelayCommand]
    public void SwapMediaBarPosition()
    {
        if (MediaBarGridRowPosition == 0)
        {
            MediaBarGridRowPosition = 1;
        }
        else
        {
            MediaBarGridRowPosition = 0;
        }
    }


    public async Task AddMusicFolderViaPickerAsync()
    {
        try
        {


            var res = await _folderPicker.PickAsync(CancellationToken.None);

            if (res is not null && res.Folder is not null)
            {
                string? selectedFolderPath = res!.Folder!.Path;

                if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    _ = Task.Run(async () => await AddMusicFolderByPassingToService(selectedFolderPath));
                }

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    [ObservableProperty]
    public partial FlyoutBehavior AppShellFlyOutBehavior { get; set; }
    partial void OnAppShellFlyOutBehaviorChanged(FlyoutBehavior oldValue, FlyoutBehavior newValue)
    {
        switch (newValue)
        {
            case FlyoutBehavior.Disabled:
                break;
            case FlyoutBehavior.Flyout:
                break;
            case FlyoutBehavior.Locked:
                break;
            default:
                break;
        }
    }
    public async Task ProcessAndMoveToViewSong(SongModelView? selectedSec)
    {
        if (selectedSec is null)
        {
            if (CurrentPlayingSongView is null)
            {
                await Shell.Current.DisplayAlert("No Song Selected", "Please select a song to view its details.", "OK");
                return;
            }
            SelectedSong ??= CurrentPlayingSongView;

        }
        else
        {
            SelectedSong = selectedSec;
        }

        if (string.IsNullOrEmpty(SelectedSong.TitleDurationKey))
        {
            await Shell.Current.DisplayAlert("Issue!", "No Song Selected To View", "Ok");
            return;
        }
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
        
    }

    public async Task InitializeParseUser()
    {
        await loginViewModel.InitializeAsync();
    }

    // Example for the "Title" column
    [ObservableProperty]
    public partial SortDirection TitleColumnSortDirection { get; set; } = SortDirection.Ascending; // Default value

    // Example for the "Artist" column
    [ObservableProperty]
    public partial string ArtistColumnFilterText { get; set; } = "";

    // Example for the "HasLyrics" column
    [ObservableProperty]
    public partial bool HasLyricsColumnIsFiltered { get; set; } = false;

    // This will hold the final, visible count
    [ObservableProperty]
    public partial int VisibleSongCount { get; set; } = 0;

    [ObservableProperty]
    public partial TableView? MyTableVIew { get; set; }
    public DimmerWin MainMAUIWindow { get; internal set; }

    // --- The partial OnChanged methods that are our triggers ---

    partial void OnTitleColumnSortDirectionChanged(SortDirection oldValue, SortDirection newValue)
    {
        // The user has changed the sorting of the Title column!
        ScheduleVisibleCountUpdate();
    }

    private void ScheduleVisibleCountUpdate()
    {
        // Logic to update the VisibleSongCount based on current filters and sorts

        if (MyTableVIew is not null)
        {
            VisibleSongCount = MyTableVIew.Items.Count;
        }


    }

    partial void OnArtistColumnFilterTextChanged(string oldValue, string newValue)
    {
        // The user has typed in the filter box for the Artist column!
        ScheduleVisibleCountUpdate();
    }

    public event EventHandler? AllSongsWindowClosed;
    internal void OnAllSongsWindowClosed()
    {
        ActivateMainWindow();
    }


    
    internal void ActivateMainWindow()
    {
        var dimWindow = windowManager.GetWindow<DimmerWin>();
        
        if (dimWindow is not null)
        {
            windowManager.ActivateWindow(dimWindow);
        }

    }

    public void OpenLyricsPopUpWindow(int Position) // 0 - Topleft, 1 - TopRight, 2 - BottomLeft, 3 - BottomRight
    {

        if (!CurrentPlayingSongView.HasSyncedLyrics) return;

        
        var syncLyricsWindow = windowManager.GetOrCreateUniqueWindow(windowFactory: () => new SyncLyricsPopUpView(this));
        if (syncLyricsWindow is null) return;
        var newPosition = new RectInt32();
        newPosition.Width = 400;
        newPosition.Height= 400;
        switch (Position)
        {
            case 0:
                newPosition.X = 0;
                newPosition.Y = 0;

                break;
            case 1:
                newPosition.X = 0;
                newPosition.Y = 1;
                break;
            case 2:
                newPosition.X = 1;
                newPosition.Y = 1;
                break;
            case 3:
                newPosition.X = 1;
                newPosition.Y = 0;
                break;
            default:
                break;
        }
        //Application.Current?.OpenWindow(syncLyricsWindow);
        PlatUtils.OpenAndSetWindowToEdgePosition(syncLyricsWindow, newPosition);
        
    }
    public override async Task AppSetupPageNextBtnClick(bool isLastTab)
    {
        await base.AppSetupPageNextBtnClick(isLastTab);

        if (isLastTab)
        {
            ShowWelcomeScreen = false;
            await Shell.Current.GoToAsync("..");
            _ = Task.Run(EnsureAllCoverArtCachedForSongsAsync);
            return;
        }
    }
    protected override async Task ProcessSongChangeAsync(SongModelView value)
    {
        // 1. Let the base class do all of its work first.
        await base.ProcessSongChangeAsync(value);


        if (value.IsCurrentPlayingHighlight)
        {

            _logger.LogInformation($"Song changed and highlighted in ViewModel B: {value.Title}");

            if (PlaybackQueueCV is not null && PlaybackQueueCV.IsLoaded)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {

                    PlaybackQueueCV?.ScrollTo(value, position: ScrollToPosition.Center, animate: true);

                });
            }
        }
    }


    public async Task ShareSongViewClipboard(SongModelView song)
    {

        var byteData = await ShareCurrentPlayingAsStoryInCardLikeGradient(song, true);

        if (byteData.imgBytes != null)
        {

            BitmapSource? bitmapSource = null;
            using (var stream = new MemoryStream(byteData.imgBytes))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Freeze to make it cross-thread accessible
                bitmapSource = bitmap;
            }

            // listening to, text so, title, artistname, album with app name, and version.
            string clipboardText = $"{song.Title} - {song.ArtistName}\nAlbum: {song.AlbumName}\n\nShared via Dimmer Music Player v{CurrentAppVersion}";

            System.Windows.Clipboard.SetImage(bitmapSource);
            System.Windows.Clipboard.SetText(clipboardText);

        }
    }

    protected override async Task OnPlaybackStarted(PlaybackEventArgs args)
    {
        await base.OnPlaybackStarted(args);
        if (args.MediaSong is null) return;
       // await PlatUtils.ShowNewSongNotification(args.MediaSong.Title, args.MediaSong.ArtistName, args.MediaSong.CoverImagePath);
    }
    [RelayCommand]
    private void OpenAllSongsPageWinUI()
    {
        var win = winUIWindowMgrService.GetOrCreateUniqueWindow(this, windowFactory: () => new AllSongsWindow(this));
        if (win is null) return;
        Debug.WriteLine(win.Visible);
        Debug.WriteLine(win.AppWindow.IsShownInSwitchers);//VERY IMPORTANT FOR WINUI 3 TO SHOW IN TASKBAR
    }
    [RelayCommand]
    private void FilterBySelection()
    {
        // This command REPLACES the current query with one based on the selected cell.
        // Use case: "I don't care what I was searching for, show me *only* this."

        var selectedContent = MySongsTableView.GetSelectedContent(true);
        var tqlClause = TqlConverter.ConvertTableViewContentToTql(selectedContent);

        if (!string.IsNullOrWhiteSpace(tqlClause))
        {
            CurrentTqlQuery = tqlClause;
            _searchQuerySubject.OnNext(CurrentTqlQuery);
        }
    }
    [ObservableProperty]
    public partial TableView MySongsTableView { get; set; }

    [RelayCommand]
    private void AddFilterFromSelection()
    {
        // This command APPENDS the new filter to the existing one.
        // Use case: "I'm looking at songs from 2004. Now, narrow it down to *only* this artist."

        var selectedContent = MySongsTableView.GetSelectedContent(true);
        var tqlClause = TqlConverter.ConvertTableViewContentToTql(selectedContent);

        if (string.IsNullOrWhiteSpace(tqlClause))
            return;

        // If the current query is empty or just a directive, replace it.
        if (string.IsNullOrWhiteSpace(CurrentTqlQuery) || CurrentTqlQuery.Trim().Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            CurrentTqlQuery = tqlClause;
        }
        else
        {
            CurrentTqlQuery = $"{CurrentTqlQuery} {tqlClause}"; // Implicit AND
        }
        _searchQuerySubject.OnNext(CurrentTqlQuery);
    }

    [RelayCommand]
    private void IncludeSelection()
    {
        // This command uses the TQL 'add' keyword (OR logic).
        // Use case: "I'm looking at Kanye West. Now, show me Jay-Z *as well*."

        var selectedContent = MySongsTableView.GetSelectedContent(true);
        var tqlClause = TqlConverter.ConvertTableViewContentToTql(selectedContent);

        if (string.IsNullOrWhiteSpace(tqlClause))
            return;

        if (string.IsNullOrWhiteSpace(CurrentTqlQuery) || CurrentTqlQuery.Trim().Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            CurrentTqlQuery = tqlClause;
        }
        else
        {
            CurrentTqlQuery = $"{CurrentTqlQuery} add {tqlClause}";
        }
        _searchQuerySubject.OnNext(CurrentTqlQuery);
    }

    [RelayCommand]
    private void ExcludeSelection()
    {
        // This command uses the TQL 'remove' keyword (AND NOT logic).
        // Use case: "I'm looking at Rock music. Now, *remove* anything that is also Pop."

        var selectedContent = MySongsTableView.GetSelectedContent(true);
        var tqlClause = TqlConverter.ConvertTableViewContentToTql(selectedContent);

        if (string.IsNullOrWhiteSpace(tqlClause))
            return;

        if (string.IsNullOrWhiteSpace(CurrentTqlQuery) || CurrentTqlQuery.Trim().Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            // Excluding from "everything" doesn't make sense, so just start a new query
            CurrentTqlQuery = $"NOT ({tqlClause})";
        }
        else
        {
            CurrentTqlQuery = $"{CurrentTqlQuery} remove {tqlClause}";
        }
        _searchQuerySubject.OnNext(CurrentTqlQuery);
    }


    internal void AddSongsByIdsToQueue(List<string> songIds)
    {
        var songsToAdd = SearchResults.Where(s => songIds.Contains(s.Id.ToString()));
        if (songsToAdd is not null && songsToAdd.Any())
        {
            AddListOfSongsToQueueEnd(songsToAdd);
        }

    }

    [RelayCommand]
    public void OpenAndSelectFileInExplorer(SongModelView song)
    {
        if (song is not null && !string.IsNullOrWhiteSpace(song.FilePath) && System.IO.File.Exists(song.FilePath))
        {
            string argument = "/select, \"" + song.FilePath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }
    }




    [RelayCommand]
    public async Task LoadPlainLyricsFromFile()
    {

        await Shell.Current.DisplayAlert("Soon...", "Feature Not available Yet...", "OK");
        //var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
        //FilePicker.Default.PickAsync

        //// Open the picker for the user to pick a file
        //IReadOnlyList<StorageFile> files = await openPicker.PickMultipleFilesAsync();
        //if (files.Count > 0)
        //{
        //    StringBuilder output = new StringBuilder("Picked files:\n");
        //    foreach (StorageFile file in files)
        //    {
        //        output.Append(file.Name + "\n");
        //    }
        //    PickFilesOutputText = output.ToString();
        //}
        //else
        //{
        //    PickFilesOutputText = "Operation cancelled.";
        //}

        //await LoadPlainLyricsFromFile(PickFilesOutputText);


    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

    }
    [RelayCommand]
    private async Task ScrollToCurrentPlayingSong()
    {

        try
        {
            //MySongsTableView.ScrollIntoView(CurrentPlayingSongView, ScrollIntoViewAlignment.Leading);
            await MySongsTableView.SmoothScrollIntoViewWithItemAsync(CurrentPlayingSongView, ScrollItemPlacement.Center,
                 false, true);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to scroll to current playing song: {ex.Message}", "OK");
        }
    }
}