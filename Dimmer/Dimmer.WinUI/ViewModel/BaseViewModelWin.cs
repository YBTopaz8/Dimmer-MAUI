// --- START OF FILE BaseViewModelWin.cs ---




using CommunityToolkit.WinUI;

using Dimmer.Utilities.Extensions;
using Dimmer.WinUI.Views.CustomViews.MauiViews;

using Colors = Microsoft.UI.Colors;
using FieldType = Dimmer.DimmerSearch.TQL.FieldType;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using MenuFlyoutSeparator = Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator;
using MenuFlyoutSubItem = Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using TableView = WinUI.TableView.TableView;
using Thickness = Microsoft.UI.Xaml.Thickness;
using ToggleMenuFlyoutItem = Microsoft.UI.Xaml.Controls.ToggleMenuFlyoutItem;
//using TableView = WinUI.TableView.TableView;

namespace Dimmer.WinUI.ViewModel;

public partial class BaseViewModelWin : BaseViewModel, IArtistActions

{

    public readonly IMauiWindowManagerService windowManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    public readonly IWinUIWindowMgrService winUIWindowMgrService;

    private readonly LoginViewModel loginViewModel;
    private readonly IFolderPicker _folderPicker; 
    public DimmerMultiWindowCoordinator DimmerMultiWindowCoordinator;

    public BaseViewModelWin(IMapper mapper, IDimmerStateService dimmerStateService,
        LoginViewModel _loginViewModel,
        DimmerMultiWindowCoordinator dimmerMultiWindowCoordinator,
        IMauiWindowManagerService mauiWindowManagerService,
        IWinUIWindowMgrService winUIWinMgrService,
    MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo, IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> PlaylistRepo, IRealmFactory RealmFact, IFolderMonitorService FolderServ, ILibraryScannerService LibScannerService, IRepository<DimmerPlayEvent> DimmerPlayEventRepo, BaseAppFlow BaseAppClass, ILogger<BaseViewModel> logger) : base(mapper, dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, PlaylistRepo, RealmFact, FolderServ, LibScannerService, DimmerPlayEventRepo, BaseAppClass, logger)
    {
        this.winUIWindowMgrService = winUIWinMgrService;
        this.loginViewModel = _loginViewModel;
        this._folderPicker = _folderPicker;
        DimmerMultiWindowCoordinator = dimmerMultiWindowCoordinator;
        DimmerMultiWindowCoordinator.BaseVM = this;
        UIQueryComponents.CollectionChanged += (s, e) =>
        {
            RebuildAndExecuteQuery();
        };

        windowManager = mauiWindowManagerService;
        //AddNextEvent += BaseViewModelWin_AddNextEvent;
        //MainWindowActivated
    }

    private void BaseViewModelWin_AddNextEvent(object? sender, EventArgs e)
    {
        //var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        //var win = winMgr.GetOrCreateUniqueWindow(this, windowFactory: () => new AllSongsWindow(this));
        //win?.Close();
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
    public Page CurrentWinUIPage { get; internal set; }

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

    [ObservableProperty]
    public partial bool? CanGoBack { get; set; }
    
    
    public DimmerWin MainWindow { get; set; }
    public DimmerMAUIWin MainMAUIWindow { get; set; }

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
            //VisibleSongCount = MyTableVIew.Items.Count;
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
        
        if (MainMAUIWindow is not null)
        {
            windowManager.ActivateWindow(MainMAUIWindow);
        }

    }

    public void OpenLyricsPopUpWindow(int Position) // 0 - Topleft, 1 - TopRight, 2 - BottomLeft, 3 - BottomRight
    {

        if (!CurrentPlayingSongView.HasSyncedLyrics) return;
       
        var syncLyricsWindow = windowManager.GetOrCreateUniqueWindow(windowFactory: () => new SyncLyricsPopUpViewWindow(this));
        if (syncLyricsWindow is null) return;
        var newPosition = new Windows.Graphics.RectInt32();
        newPosition.Width = 400;
        newPosition.Height = 400;
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
        var nativeWindow = PlatUtils.GetNativeWindowFromMAUIWindow();
        var press = nativeWindow.AppWindow.Presenter as OverlappedPresenter;
        press?.Minimize();

    }
    public override async Task AppSetupPageNextBtnClick(bool isLastTab)
    {
        await base.AppSetupPageNextBtnClick(isLastTab);

        if (isLastTab)
        {
            ShowWelcomeScreen = false;
            await Shell.Current.GoToAsync("..");
            return;
        }
    }

    public override void ResetCurrentPlaySongDominantColor()
    {
        if (CurrentPlaySongDominantColor is null)
            return;
        var c = CurrentPlaySongDominantColor;
        DominantBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(
        (byte)(c.Alpha * 255),
        (byte)(c.Red * 255),
        (byte)(c.Green * 255),
        (byte)(c.Blue * 255)
    ));
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
                RxSchedulers.UI.Schedule(() =>
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
        //var win = winUIWindowMgrService.GetOrCreateUniqueWindow(this, windowFactory: () => new AllSongsWindow(this));
        //if (win is null) return;
        //Debug.WriteLine(win.Visible);
        //Debug.WriteLine(win.AppWindow.IsShownInSwitchers);//VERY IMPORTANT FOR WINUI 3 TO SHOW IN TASKBAR
    }
   
    [ObservableProperty]
    public partial TableView MySongsTableView { get; set; }

    

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
            await MySongsTableView.SmoothScrollIntoViewWithItemAsync(CurrentPlayingSongView, ScrollItemPlacement.Center,false, true);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to scroll to current playing song: {ex.Message}", "OK");
        }
    }
    [RelayCommand]
    private async Task ScrollToSpecificSong(SongModelView song)
    {

        try
        {
            
            await MySongsTableView.SmoothScrollIntoViewWithItemAsync(song, ScrollItemPlacement.Center, false, true);

            await Task.Delay(200);
            var itemIndex= MySongsTableView.Items.IndexOf(song);
           
            var contentTableRow = MySongsTableView.ContainerFromIndex(itemIndex) as TableViewRow;
            var cellPresenter = contentTableRow?.CellPresenter;
            IList<TableViewCell>? cells = cellPresenter?.Cells;
            
            if(cells is null && cells?.Count > 0)
            {
                Debug.WriteLine("No cells found");
                return;
            }



            if(contentTableRow is null)
            {
                Debug.WriteLine("No content Table Row found");
                return;
            }

            if(cellPresenter is null)
            {
                Debug.WriteLine("No cell presenter found");
                return;
            }

            if(cells is null)
            {
                Debug.WriteLine("No cell presenter found");
                return;
            }

            //cellPresenter?.BorderBrush = new SolidColorBrush(Colors.Red);
            //cellPresenter?.BorderThickness = new Microsoft.UI.Xaml.Thickness(2);

            //contentTableRow?.BorderBrush = new SolidColorBrush(Colors.Pink);
            //contentTableRow?.BorderThickness = new Microsoft.UI.Xaml.Thickness(4);

            TableViewCell? coverImageCell = cells[0];

            await PulseWithBorderAsync(coverImageCell,pulses:2, duration:500);

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to scroll to current playing song: {ex.Message}", "OK");
        }
    }

    public static async Task PulseWithBorderAsync(
     TableViewCell element,
     int pulses = 2,
     double scale = 1.08,
     int duration = 180)
    {
        if (element is null) return;

        

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3(
            (float)(element.RenderSize.Width / 2),
            (float)(element.RenderSize.Height / 2),
            0f);

        for (int i = 0; i < pulses; i++)
        {
            // SCALE UP
            var up = compositor.CreateVector3KeyFrameAnimation();
            up.Target = "Scale";
            up.Duration = TimeSpan.FromMilliseconds(duration);
            up.InsertKeyFrame(1f, new Vector3((float)scale, (float)scale, 1f));

            visual.StartAnimation("Scale", up);

            if (element != null)
            {
                element.BorderThickness = new Thickness(2);
                element.BorderBrush = new SolidColorBrush(Colors.DarkSlateBlue);
            }

            await Task.Delay(duration);

            // SCALE DOWN
            var down = compositor.CreateVector3KeyFrameAnimation();
            down.Target = "Scale";
            down.Duration = TimeSpan.FromMilliseconds(duration);
            down.InsertKeyFrame(1f, new Vector3(1f, 1f, 1f));

            visual.StartAnimation("Scale", down);

            if (element != null)
            {
                element.BorderThickness = new Thickness(0);
                element.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }

            await Task.Delay(duration);
        }
    }




    private async void AnimateScaleControlUp(FrameworkElement btn)
    {
        try
        {
            if (btn.DataContext is not SongModelView song) return;
            if (song.CoverImagePath is null)
                return;

            await btn.DispatcherQueue.EnqueueAsync(() => { });
            var compositor = ElementCompositionPreview.GetElementVisual(btn).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(btn);

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, new Vector3(1.05f));
            scale.Duration = TimeSpan.FromMilliseconds(350);
            rootVisual.CenterPoint = new Vector3((float)btn.ActualWidth / 2, (float)btn.ActualHeight / 2, 0);
            rootVisual.StartAnimation("Scale", scale);

            //var img = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(song.CoverImagePath, UriKind.Absolute));
            //FocusedSongImage.Source = img;

            //FocusedSongTextBlockTitle.Content = song.Title;
            //FocusedSongTextBlockArtistName.Content = song.ArtistName;
            //FocusedSongTextBlockAlbumName.Content = song.AlbumName;
            //FocusedSongTextBlockGenre.Content = song.GenreName;


        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnimateExpand Exception: {ex.Message}");
        }
    }

    private void AnimateCollapseControlDown(FrameworkElement framework)
    {
        try
        {

            // collapse animation (optional)
            var compositor = ElementCompositionPreview.GetElementVisual(framework).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(framework);
            var scaleBack = compositor.CreateVector3KeyFrameAnimation();
            scaleBack.InsertKeyFrame(1f, new Vector3(1f));
            scaleBack.Duration = TimeSpan.FromMilliseconds(300);
            rootVisual.StartAnimation("Scale", scaleBack);

        }
        catch (Exception ex)
        {

            Debug.WriteLine($"AnimateCollapse Exception: {ex.Message}");
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<WindowEntry> AllWindows { get; set; }
    [ObservableProperty]
    public partial SolidColorBrush DominantBrush { get; set; }

    [RelayCommand]
    public void RefreshWindows()
    {
        AllWindows.Clear();
        foreach (var win in DimmerMultiWindowCoordinator.Windows)
        {
            AllWindows.Add(win);
        }
    }

    [RelayCommand]
    public void SaveAllWindows()
    {
        DimmerMultiWindowCoordinator.SaveAll();
    }

    [RelayCommand]
    public void ShowControlPanel()
    {
        DimmerMultiWindowCoordinator.ShowControlPanel();
    }

    public void QuickViewArtist(string artistName)
    {
        Debug.WriteLine($"Quick view for artist: {artistName}");
        // TODO: open artist popup or small window
    }

    public void PlaySongsByArtistInCurrentAlbum(string artistName)
    {
        Debug.WriteLine($"Play songs by {artistName} in current album.");
        // TODO: filter and start playback from current album list
    }

    public void PlayAllSongsByArtist(string artistName)
    {
        Debug.WriteLine($"Play all songs by {artistName}.");
        // TODO: query Realm for all songs where Artist == artistName
    }

    public void QueueAllSongsByArtist(string artistName)
    {
        Debug.WriteLine($"Queue all songs by {artistName}.");
        // TODO: add matching songs to NowPlayingQueue
    }

    public void NavigateToArtistPage(string artistName)
    {
        Debug.WriteLine($"Navigating to artist page: {artistName}");
        // TODO: open a WinUI page or a MAUI subview with artist info
    }

    public bool IsArtistFavorite(string artistName)
    {
        Debug.WriteLine($"Checking favorite status for {artistName}");
        // TODO: query Realm for favorite
        return false;
    }

    public void ToggleFavoriteArtist(string artistName, bool isFavorite)
    {
        Debug.WriteLine($"Set favorite={isFavorite} for {artistName}");
        // TODO: update Realm favorites collection
    }

    public int GetArtistPlayCount(string artistName)
    {
        Debug.WriteLine($"Fetching play count for {artistName}");
        // TODO: return number of times artist's songs have been played
        return 0;
    }

    public bool IsArtistFollowed(string artistName)
    {
        Debug.WriteLine($"Checking if {artistName} is followed");
        // TODO: check Realm or local list
        return false;
    }

    [RelayCommand]
    private async Task OpenDimmerWindow()
    {
        MainWindow = winUIWindowMgrService.GetOrCreateUniqueWindow<DimmerWin>(this, () => new DimmerWin())!;
        if (MainWindow is null) return;

        await DimmerMultiWindowCoordinator.SnapAllToHomeAsync();
        

    }

    public async Task LoadFullArtistDetails(ArtistModelView artist)
    {

        SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(artist.Name));
        var tempVar = await lastfmService.GetArtistInfoAsync(artist.Name);

        if (tempVar is not null)
        {
            artist.Bio = tempVar.Biography.Content;

            var similar = tempVar.Similar.Select(x => x.Name);
            //tempVar.Url;
            // find matches for any time in search results
            ObservableCollection<string> are = new System.Collections.ObjectModel.ObservableCollection<string>(similar);
            artist.ListOfSimilarArtists = are;
        }
        artist.TotalSongsByArtist = SearchResults.Count(x => x.ArtistToSong.Any(a => a.Name == artist.Name));
        artist.TotalAlbumsByArtist = SearchResults.Count(x => x.Album.Artists.Any(a => a.Name == artist.Name));

        
        RxSchedulers.UI.Schedule(()=>
        {
            SelectedArtist = artist;
        });
    }

    public override async void ShowAllSongsWindowActivate()
    {
        base.ShowAllSongsWindowActivate();

        await OpenDimmerWindow();
        MainWindow.NavigateToPage(typeof(AllSongsListPage));
    }

    public void NavigateToAnyPageOfGivenType(Type pageType)
    {
        if (MainWindow is null) return;
        MainWindow.NavigateToPage(pageType);
    }

    /// <summary>
    /// Populates a MenuFlyout tailored to right-clicking a song title.
    /// - Implements utilities and external searches.
    /// - Throws NotImplementedException for app-specific features (navigation, playback, editing).
    /// </summary>
    public void PopulateSongTitleContextMenuFlyout(Microsoft.UI.Xaml.Controls.MenuFlyout flyout, SongModelView songmodelView)
    {

        if (flyout == null) throw new ArgumentNullException(nameof(flyout));
        if (songmodelView == null) throw new ArgumentNullException(nameof(songmodelView));

        var realm = RealmFactory.GetRealmInstance();
        var song = realm.Find<SongModel>(songmodelView.Id);
        if (song is null) return;

        // --- 1) Navigation
        // Can't implement app navigation because we don't know your navigation service.
        flyout.Items.Add(MI($"Open song page…", () => throw new NotImplementedException("Navigation to song page: wire this to your navigation service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 2) Actions
        // Playback / queue actions require your app playback service -> throw NotImplementedException.
        flyout.Items.Add(MI("Play Now", () => throw new NotImplementedException("Play Now: wire to your playback service.")));
        flyout.Items.Add(MI("Play Next", () => throw new NotImplementedException("Play Next: wire to your playback service.")));
        flyout.Items.Add(MI("Add to End of Queue", () => throw new NotImplementedException("Add to Queue: wire to your playback/queue service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 3) Edit
        // Editing song metadata / toggling favorite requires app-specific DB write UI/logic -> throw.
        flyout.Items.Add(MI("Edit song info…", () => throw new NotImplementedException("Edit song info: open your edit dialog / view model here.")));

        // Favorite toggle: we don't know schema; throw so developer wires proper behaviour.
        flyout.Items.Add(Toggle("Favorite", false, _ => throw new NotImplementedException("Toggle favorite: implement according to your SongModel fields / services.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 4) Lyrics
        // View lyrics: best-effort: try to open a search to find lyrics online (fully implemented)
        flyout.Items.Add(MI("View lyrics (web)", async () => await LaunchUriFromStringAsync(BuildLyricsSearchUrl(song.Title, song.ArtistName))));
        // Edit lyrics: requires your UI -> throw
        flyout.Items.Add(MI("Edit lyrics…", () => throw new NotImplementedException("Edit lyrics: open your lyrics editor here.")));
        // Fetch lyrics (try a quick web search) — implemented as search fallback
        flyout.Items.Add(MI("Fetch lyrics (web)", async () => await LaunchUriFromStringAsync(BuildLyricsSearchUrl(song.Title, song.ArtistName))));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 5) Utilities (fully implemented where possible)
        flyout.Items.Add(MI("Copy song title", () => CopyToClipboard(song.Title ?? string.Empty)));
        flyout.Items.Add(MI("Copy full song info", () => CopyToClipboard($"{song.Title} — {song.ArtistName} ({song.Album})")));
        flyout.Items.Add(MI("Open file location…", async () =>
        {
            await OpenFileInOtherApp(songmodelView);
        }));
        flyout.Items.Add(MI("Share story card…", () => throw new NotImplementedException("Share story card: implement share generation & DataTransferManager logic.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 6) Search / External
        flyout.Items.Add(Sub("Search on...",
            MakeExternalItem("Google (song)", BuildGoogleSearchUrl($"{song.Title} {song.ArtistName}")),
            MakeExternalItem("YouTube (song)", BuildYouTubeSearchUrl($"{song.Title} {song.ArtistName}")),
            MakeExternalItem("Spotify (song)", BuildSpotifySearchUrl($"{song.Title} {song.ArtistName}")),
            MakeExternalItem("Deezer (song)", BuildDeezerSearchUrl($"{song.Title} {song.ArtistName}"))
        ));
    }
    /// <summary>
    /// Populates a MenuFlyout when the user right-clicks an artist within a song.
    /// Implements copy & external links. Other app-specific parts throw NotImplementedException.
    /// </summary>
    public void PopulateSongArtistContextMenuFlyout(Microsoft.UI.Xaml.Controls.MenuFlyout flyout, SongModelView songmodelView)
    {
        ArgumentNullException.ThrowIfNull(flyout);
        ArgumentNullException.ThrowIfNull(songmodelView);

        var realm = RealmFactory.GetRealmInstance();
        var song = realm.Find<SongModel>(songmodelView.Id);
        if (song is null) return;

        // Determine main and other artists from the relationship
        var songMainArtist = song.ArtistToSong?.FirstOrDefault();
        var allArtistRelations = song.ArtistToSong?.ToArray() ?? Array.Empty<ArtistModel>();

        // If we have no artist info, nothing to show
        if (songMainArtist == null && allArtistRelations.Length == 0) return;

        // Choose display name(s)
        string primaryName = songMainArtist?.Name ?? allArtistRelations.FirstOrDefault()?.Name ?? "(Unknown artist)";

        // --- 1) Navigation
        flyout.Items.Add(MI($"Open artist page…", () => throw new NotImplementedException("Navigation to artist page: wire this to your navigation service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 2) Actions (play/queue/add to playlist) -> require playback/playlist service
        flyout.Items.Add(MI("Play all by artist", () => throw new NotImplementedException("Play all by artist: wire to your playback service.")));
        flyout.Items.Add(MI("Queue all by artist", () => throw new NotImplementedException("Queue all by artist: wire to your queue service.")));
        flyout.Items.Add(MI("Add artist to playlist…", () => throw new NotImplementedException("Add to playlist: open your Add to Playlist dialog.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 3) Edit
        flyout.Items.Add(MI("Edit artist info…", () => throw new NotImplementedException("Edit artist: open your artist edit dialog.")));
        flyout.Items.Add(Toggle("Follow artist", false, _ => throw new NotImplementedException("Follow/unfollow artist: implement according to your follow model/service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- 4) Utilities (copy name + external searches implemented)
        flyout.Items.Add(MI("Copy artist name", () => CopyToClipboard(primaryName)));
        flyout.Items.Add(Sub("Find on...",
            MakeExternalItem("Spotify (artist)", BuildSpotifySearchUrl(primaryName)),
            MakeExternalItem("YouTube Music (artist)", BuildYouTubeSearchUrl(primaryName)),
            MakeExternalItem("MusicBrainz (artist)", BuildMusicBrainzArtistSearchUrl(primaryName)),
            MakeExternalItem("Discogs (artist)", BuildDiscogsArtistSearchUrl(primaryName))
        ));
    }
    /// <summary>
    /// Populates a MenuFlyout for album context.
    /// Implements external searches and copy utilities. App-specific actions throw NotImplementedException.
    /// </summary>
    public void PopulateSongAlbumContextMenuFlyout(Microsoft.UI.Xaml.Controls.MenuFlyout flyout, SongModelView songmodelView)
    {
        if (flyout == null) throw new ArgumentNullException(nameof(flyout));
        if (songmodelView == null) throw new ArgumentNullException(nameof(songmodelView));

        var realm = RealmFactory.GetRealmInstance();
        var albumInDb = realm.Find<SongModel>(songmodelView.Id)?.Album;
        if (albumInDb is null) return;

        var artistsInAlbum = albumInDb.Artists?.ToArray() ?? Array.Empty<ArtistModel>();
        var songsInAlbum = albumInDb.SongsInAlbum?.ToArray() ?? Array.Empty<SongModel>();

        // --- Navigation
        flyout.Items.Add(MI($"Open album page…", () => throw new NotImplementedException("Navigation to album page: wire to your navigation service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- Actions (play / queue / add to playlist) -> app-specific
        flyout.Items.Add(MI("Play album", () => throw new NotImplementedException("Play album: wire to playback service.")));
        flyout.Items.Add(MI("Queue album", () => throw new NotImplementedException("Queue album: wire to queue service.")));
        flyout.Items.Add(MI("Add album to playlist…", () => throw new NotImplementedException("Add album to playlist: open playlist UI.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- Edit
        flyout.Items.Add(MI("Edit album info…", () => throw new NotImplementedException("Edit album info: open edit dialog.")));
        flyout.Items.Add(Toggle("Favorite album", false, _ => throw new NotImplementedException("Toggle album favorite: implement per your model/service.")));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- Utilities
        flyout.Items.Add(MI("Copy album name", () => CopyToClipboard(albumInDb.Name ?? string.Empty)));
        flyout.Items.Add(Sub("Artists in album",
            artistsInAlbum.Select(a => MI(a.Name ?? "(unknown)", () => throw new NotImplementedException("Artist click: implement navigation or quick view."))).Cast<MenuFlyoutItemBase>().ToArray()
        ));

        flyout.Items.Add(new MenuFlyoutSeparator());

        // --- External search
        var albumLabel = $"{albumInDb.Name} {string.Join(" ", artistsInAlbum.Select(a => a.Name))}";
        flyout.Items.Add(Sub("Search on...",
            MakeExternalItem("Spotify (album)", BuildSpotifySearchUrl(albumLabel)),
            MakeExternalItem("YouTube Music (album)", BuildYouTubeSearchUrl(albumLabel)),
            MakeExternalItem("Google (album)", BuildGoogleSearchUrl(albumLabel))
        ));

    }


    MenuFlyoutItem MI(string text, Action? onClick = null)
    {
        var x = new MenuFlyoutItem { Text = text };
        if (onClick != null) x.Click += (_, __) => onClick();
        return x;
    }

    ToggleMenuFlyoutItem Toggle(string text, bool state, Action<bool> change)
    {
        var t = new ToggleMenuFlyoutItem { Text = text, IsChecked = state };
        t.Click += (_, __) => change(t.IsChecked);
        return t;
    }

    MenuFlyoutSubItem Sub(string text, params MenuFlyoutItemBase[] items)
    {
        var s = new MenuFlyoutSubItem { Text = text };
        foreach (var i in items) s.Items.Add(i);
        return s;
    }

    MenuFlyoutItem MakeExternalItem(string label, string url)
    {
        var item = new MenuFlyoutItem { Text = label };
        item.Click += async (_, __) =>
        {
            try
            {
                await LaunchUriFromStringAsync(url);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"External launch failed: {ex.Message}");
            }
        };
        return item;
    }

    // ---------- Utilities ----------

    void CopyToClipboard(string text)
    {
        try
        {
            var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
            dp.SetText(text ?? string.Empty);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CopyToClipboard failed: {ex.Message}");
        }
    }

    // Use Launcher to open a URL. Returns a Task so callers can await where appropriate.
    async Task LaunchUriFromStringAsync(string uriStr)
    {
        if (string.IsNullOrWhiteSpace(uriStr)) return;
        var uri = new Uri(uriStr);
        await Windows.System.Launcher.LaunchUriAsync(uri);
        
    }

    // ---------- URL builders (simple, safe search URLs) ----------

    static string BuildGoogleSearchUrl(string query) =>
        $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";

    static string BuildYouTubeSearchUrl(string query) =>
        $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";

    static string BuildSpotifySearchUrl(string query) =>
        // Spotify web search is robust with query parameter
        $"https://open.spotify.com/search/{Uri.EscapeDataString(query)}";

    static string BuildDeezerSearchUrl(string query) =>
        $"https://www.deezer.com/search/{Uri.EscapeDataString(query)}";

    static string BuildLyricsSearchUrl(string title, string artist) =>
        BuildGoogleSearchUrl($"{title} {artist} lyrics");

    static string BuildMusicBrainzArtistSearchUrl(string artist) =>
        $"https://musicbrainz.org/search?query={Uri.EscapeDataString(artist)}&type=artist";

    static string BuildDiscogsArtistSearchUrl(string artist) =>
        $"https://www.discogs.com/search/?q={Uri.EscapeDataString(artist)}&type=artist";

    [ObservableProperty]
    public partial WinUIVisibility IsBackButtonVisible { get; set; }

    partial void OnIsBackButtonVisibleChanged(WinUIVisibility oldValue, WinUIVisibility newValue)
    {
        


    }
    [ObservableProperty]
    public partial bool AutoConfirmLastFMVar { get; set; }
    public override bool AutoConfirmLastFM(bool val)
    {

        AutoConfirmLastFMVar = base.AutoConfirmLastFM(val);

        return AutoConfirmLastFMVar;
    }

    public async Task CheckToCompleteActivation()
    {

        if (AutoConfirmLastFMVar)
        {
            ContentDialog lastFMConfirmDialog = new ContentDialog
            {
                Title = "LAST FM Confirm",
                Content = "Is Authorization done?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = MainWindow.ContentFrame.XamlRoot

            };
            var isLastFMAuthorized = await lastFMConfirmDialog.ShowAsync() == ContentDialogResult.Primary;

            if (isLastFMAuthorized)
            {
                await CompleteLastFMLoginAsync();
            }
            else
            {
                IsLastFMNeedsToConfirm = false;
                ContentDialog cancelledDialog = new ContentDialog
                {
                    Title = "Action Cancelled",
                    Content = "Last FM Authorization Cancelled",
                    CloseButtonText = "OK",
                    XamlRoot = MainWindow.ContentFrame.XamlRoot
                };
                await cancelledDialog.ShowAsync();

            }
        }




    }
}