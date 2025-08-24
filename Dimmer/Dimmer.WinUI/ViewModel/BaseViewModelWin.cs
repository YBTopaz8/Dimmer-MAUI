// --- START OF FILE BaseViewModelWin.cs ---

using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.Data.ModelView.DimmerSearch;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch.TQL;
using Dimmer.DimmerSearch.TQL.TQLCommands;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.LastFM;
using Dimmer.Utilities.FileProcessorUtils;
// Assuming SkiaSharp and ZXing.SkiaSharp are correctly referenced for barcode scanning

// Assuming Vanara.PInvoke.Shell32 and TaskbarList are for Windows-specific taskbar progress
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinUIPages;

using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Controls;

using Realms;

using System.Threading.Tasks;

using Vanara.PInvoke;

using WinUI.TableView;

using FieldType = Dimmer.DimmerSearch.TQL.FieldType;
using TableView = WinUI.TableView.TableView;
using Window = Microsoft.UI.Xaml.Window;

namespace Dimmer.WinUI.ViewModel; 

public partial class BaseViewModelWin: BaseViewModel

{

    private readonly IWindowManagerService windowManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    private readonly IWindowManagerService winMgrService;
    private readonly LoginViewModel loginViewModel;
    private readonly IFolderPicker _folderPicker;
    public BaseViewModelWin(IMapper mapper, MusicDataService musicDataService,LoginViewModel _loginViewModel,
         IDimmerStateService dimmerStateService, IFolderPicker _folderPicker, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo,  IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, ILogger<BaseViewModel> logger) : base(mapper, dimmerStateService,musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, logger)
    {
        this.loginViewModel=_loginViewModel;
        this._folderPicker = _folderPicker;
        UIQueryComponents.CollectionChanged += (s, e) =>
        {
            RebuildAndExecuteQuery();
        };

        AddNextEvent +=BaseViewModelWin_AddNextEvent;
    }

    private async void BaseViewModelWin_AddNextEvent(object? sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var win = winMgr.GetOrCreateUniqueWindow(() => new AllSongsWindow(this));
        win.Close();

        // wait 4s and reopen it
        await Task.Delay(4000);

            var newWin = winMgr.GetOrCreateUniqueWindow(() => new AllSongsWindow(this));
            newWin.Activate();

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
            if (UIQueryComponents?.Count >0)
            {
                var tt = new LogicalJoinerViewModel(RebuildAndExecuteQuery) as IQueryComponentViewModel;
                UIQueryComponents.Add(tt);
            }

            var er = new ActiveFilterViewModel(tqlField, displayText, tqlClause, RemoveFilterCommand) as IQueryComponentViewModel;

            UIQueryComponents.Add(er);
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
        if (UIQueryComponents?.Count>0)
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
                if (clauses.Count >0)
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
    public partial int MediaBarGridRowPosition { get; set; }
    public CollectionView SongColView { get; internal set; }


    [ObservableProperty]
    public partial List<string> DraggedAudioFiles { get; internal set; }
    public Window CurrentWinUIPage { get; internal set; }

    [RelayCommand]
    public void SwapMediaBarPosition()
    {
        if (MediaBarGridRowPosition==0)
        {
            MediaBarGridRowPosition = 1;
        }
        else
        {
            MediaBarGridRowPosition=0;
        }
    }

    public void RescanFolderPath(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }
        AddMusicFolderByPassingToService(folderPath);
    }
    public async Task AddMusicFolderViaPickerAsync()
    {


        var res = await _folderPicker.PickAsync(CancellationToken.None);

        if (res is not null && res.Folder is not null)
        {


            string? selectedFolderPath = res!.Folder!.Path;



            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                AddMusicFolderByPassingToService(selectedFolderPath);
            }
            else
            {

            }
        }
        else
        {

        }
    }
    public async Task PickFolderToScan()
    {
        await AddMusicFolderViaPickerAsync();
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
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong=selectedSec;
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
    public partial string ArtistColumnFilterText {get;set;}= "";

    // Example for the "HasLyrics" column
    [ObservableProperty]
    public partial bool HasLyricsColumnIsFiltered {get;set;}= false;

    // This will hold the final, visible count
    [ObservableProperty]
    public partial int VisibleSongCount {get;set;}= 0;

    [ObservableProperty]
    public partial TableView? MyTableVIew {get;set;}

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

    public async Task ApplyCurrentImageToMainArtist(SongModelView? selectedSong)
    {
        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(selectedSong.Id);
            if (songInDb is null)
            {
                return;
            }
            var album = songInDb.Album;
            var songArtist = songInDb.Artist;
            if (album is null || songArtist is null)
            {
                return;
            }

            songArtist.ImagePath= songInDb.CoverImagePath;

            // save changes

            realm.Add(songArtist, update: true);


        });


    }

    [RelayCommand]
    public async Task PickAndApplyImageToSong(SongModelView? selectedSong)
    {

        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an image",
            FileTypes = FilePickerFileType.Images,
        });
        if (result is null)
        {
            return;
        }


        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(selectedSong.Id);
            if (songInDb is null)
            {
                return;
            }

            songInDb.CoverImagePath = result.FullPath;

            // save changes

            realm.Add(songInDb, update: true);


        });


    }

    [RelayCommand]
    public async Task ApplCurrentImageToSong(SongModelView? selectedSong)
    {

        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }
        

        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(selectedSong.Id);
            if (songInDb is null)
            {
                return;
            }

            songInDb.CoverImagePath = selectedSong.CoverImagePath;

            // save changes

            realm.Add(songInDb, update: true);


        });


    }

    [RelayCommand]
    public async Task ApplyCurrentImageToAllSongsInAlbum(SongModelView? selectedSong)
    {

        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(selectedSong.Id);
            if (songInDb is null)
            {
                return;
            }
            var album = songInDb.Album;
            var songsInAlbum = songInDb.Album.SongsInAlbum;
            if (album is null || songsInAlbum is null)
            {
                return;
            }
            foreach (var song in songsInAlbum)
            {
                song.CoverImageBytes = songInDb.CoverImageBytes;
                song.CoverImagePath = songInDb.CoverImagePath;
            }

            // save changes

            realm.Add(songInDb, update: true);


        });

    }

    [RelayCommand]

    public async Task ShareCurrentPlayingAsStoryInCardLikeGradient(SongModelView? selectedSong)
    {

        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }

        // first create the image with SkiaSharp
        var imagePath = CoverArtService.CreateStoryImageAsync(SelectedSong,null);
        if (string.IsNullOrEmpty(imagePath))
        {
            await Shell.Current.DisplayAlert("Error", "Failed to create story image.", "OK");
            return;
        }
        // then share it
        ShareFileRequest request = new ShareFileRequest
        {
            Title = $"Share {SelectedSong.Title} by {SelectedSong.ArtistName}",
            File = new ShareFile(imagePath),
        };  
        await Share.RequestAsync(request);




    }


    internal async Task SaveCurrentCoverToDisc(SongModelView? selectedSong)
    {
        // save current cover to disc using file saver in pictures folder
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }
        // Save the image to the Pictures folder with a unique name

        var fileName = $"{SelectedSong.Title}_{SelectedSong.ArtistName}.jpg";
        // Use FileSaver from CommunityToolkit.Maui.Storage
        

        var bytess = File.ReadAllBytes(SelectedSong.CoverImagePath);
        var stream = new MemoryStream(bytess);
        var result = await FileSaver.Default.SaveAsync(fileName, SelectedSong.CoverImagePath,stream);

        if (result.IsSuccessful)
        {

            ShareFileRequest request = new ShareFileRequest
            {
                Title = $"Share {SelectedSong.Title} by {SelectedSong.ArtistName}",
                File = new ShareFile(result.FilePath),
            };
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save image: {result.Exception.Message}", "OK");
        }

    }
}