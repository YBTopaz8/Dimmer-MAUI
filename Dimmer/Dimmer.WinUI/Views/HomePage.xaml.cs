//using Dimmer.DimmerLive.Models;
using Dimmer.DimmerLive;
using Dimmer.DimmerSearch;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinUIPages;

using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
using DataTemplate = Microsoft.Maui.Controls.DataTemplate;
using DragEventArgs = Microsoft.Maui.Controls.DragEventArgs;
using DragStartingEventArgs = Microsoft.Maui.Controls.DragStartingEventArgs;
using Label = Microsoft.Maui.Controls.Label;
using SortOrder = Dimmer.Utilities.SortOrder;
using View = Microsoft.Maui.Controls.View;
using Window = Microsoft.UI.Xaml.Window;
using WinUIControls = Microsoft.UI.Xaml.Controls;

namespace Dimmer.WinUI.Views;

    public partial class HomePage : ContentPage
    {


        public BaseViewModelWin MyViewModel { get; internal set; }



        public HomePage(BaseViewModelWin vm , IWinUIWindowMgrService windowMgrService)
        {
            InitializeComponent();
            BindingContext = vm;
            MyViewModel = vm;


        // --- Keep these lines. They correctly wire up the UI. ---

        //MyViewModel.TranslatedSearch= TranslatedSearch;
        //MyViewModel.SongsCountLabel = SongsCountLabel;
        _availableLayouts = new List<DataTemplate>
        {
            (DataTemplate)Resources["OGView"],
            (DataTemplate)Resources["GridOfFour"],
        };
        _availableItemsLayouts = new List<IItemsLayout>
        {
            new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = 5 },
            new GridItemsLayout(ItemsLayoutOrientation.Vertical) { Span = 6, VerticalItemSpacing = 10, HorizontalItemSpacing = 5 }
        };
        _windowMgrService = windowMgrService;

        // Subscribe to the new events
        _windowMgrService.WindowActivated += OnAnyWindowActivated;
        _windowMgrService.WindowClosed += OnAnyWindowClosed;
        _windowMgrService.WindowClosing += OnAnyWindowClosing;
    }

    private void OnAnyWindowClosing(object? sender, WinUIWindowMgrService.WindowClosingEventArgs e)
    {
        Debug.WriteLine($"A window is trying to close: {e.Window.Title}");

        // Example: Prevent a specific window from closing if it has unsaved changes
        //if (e.Window is AllSongsWindow editor && editor.HasUnsavedChanges)
        //{
        //    // You would typically show a dialog here asking the user to save.
        //    // If they cancel, you set e.Cancel = true;
        //    Debug.WriteLine($"Closing cancelled for {e.Window.Title} due to unsaved changes.");
        //    e.Cancel = true;
        //}
    }

    private void OnAnyWindowClosed(object? sender, Window closedWindow)
    {
        Debug.WriteLine($"A window was just closed: {closedWindow.Title}");
        // Maybe update a status bar or a "Window" menu list
    }

    private void OnAnyWindowActivated(object? sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState != WindowActivationState.Deactivated && sender is Window activatedWindow)
        {
            Debug.WriteLine($"Window Activated: {activatedWindow.Title}");
            // You could use this to update a "currently active document" display
        }
    }
    private List<DataTemplate> _availableLayouts; 
    private readonly List<IItemsLayout> _availableItemsLayouts;
    private readonly IWinUIWindowMgrService _windowMgrService;
    private int _currentLayoutIndex = 0;
    
      private void ChangeLayout_Clicked(object sender, EventArgs e)
    {
        // 1. Cycle to the next layout index
        _currentLayoutIndex = (_currentLayoutIndex + 1) % _availableLayouts.Count;

        // 2. Get the current ItemsSource and hold onto it
        var items = SongsColView.ItemsSource;

        // 3. IMPORTANT: Set ItemsSource to null to force a hard reset
        SongsColView.ItemsSource = null;

        // 4. Apply the new layout AND the new template
        SongsColView.ItemsLayout = _availableItemsLayouts[_currentLayoutIndex];
        SongsColView.ItemTemplate = _availableLayouts[_currentLayoutIndex];

        // 5. Restore the ItemsSource. The CollectionView will now rebuild
        //    itself using the new layout and template.
        SongsColView.ItemsSource = items;
    
    }
    private async void ViewSongMFI_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.CommandParameter as SongModelView;

        await this.FadeOut(200, 0.7);
        MyViewModel.SelectedSong = song;
        await this.FadeIn(350, 1);

    }

    private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(e.NewTextValue);
    }
    private void Button_Clicked(object sender, EventArgs e)
    {

    }
    private async void ViewSongDetails_PointerPressed(object sender, PointerEventArgs e)
    {
        var view = (Microsoft.Maui.Controls.View)sender;
        var gestRec = view.GestureRecognizers[0] as PointerGestureRecognizer;
        MyViewModel.SelectedSong=gestRec.PointerPressedCommandParameter as SongModelView;

        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);

    }

    private async void NavigateToSelectedSongPageAsync(object sender, EventArgs e)
    {
        var view = (Microsoft.Maui.Controls.View)sender;
        var selectedSec = view.BindingContext as SongModelView;
        await MyViewModel.ProcessAndMoveToViewSong(selectedSec);
    }

    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {
        var view = (Microsoft.Maui.Controls.MenuFlyoutItem)sender;
        var selectedSec = view.BindingContext as SongModelView;
        await MyViewModel.ProcessAndMoveToViewSong(selectedSec);
    }
    private async void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {
        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(nativeElement).Properties;

        if (!properties.IsMiddleButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            return;

            
        }
        //var ee = e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers;
        //if (e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
        //{
        //    return;
        //}
        var send = (Microsoft.Maui.Controls.View)sender;
        var gest = send.GestureRecognizers[0] as PointerGestureRecognizer;
        if (gest is null)
        {
            return;
        }
        var field = gest.PointerReleasedCommandParameter as string;
        var val = gest.PointerPressedCommandParameter as string;
        if (field is "artist")
        {
            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = val
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List


            var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

            if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
            {
                return;
            }
            SearchSongSB_Clicked(sender, e);
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));

            return;
        }

        SearchSongSB_Clicked(sender, e);
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));

    }


    ////////








    protected async override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPageContext = CurrentPage.AllSongs;
        MyViewModel.SongColView = SongsColView;

       await MyViewModel.InitializeParseUser();
    }


    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {

        //MyViewModel.SearchSongSB_TextChanged(MyViewModel.CurrentPlaybackQuery+ " >>addend!");
    }

    private async void ArtistsEffectsView_LongPressed(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;

        if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }
    }

    private async void PlaySongGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Microsoft.Maui.Controls.View)sender;
        var song = send.BindingContext as SongModelView;
        if(MyViewModel.PlaybackQueue.Count<1)
        {
            MyViewModel.SearchSongSB_TextChanged(">>addnext!");
        }
      await  MyViewModel.PlaySong(song, CurrentPage.HomePage);
        //ScrollToSong_Clicked(sender, e);
    }

    private void CurrPlayingSongGesRec_Tapped(object sender, TappedEventArgs e)
    {
        //var song = e.Parameter as SongModelView;
        //if (song is not null)
        //{
        //    DeviceStaticUtils.SelectedSongOne = song;
        //    await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
        //    return;
        //}

        //switch (e.Parameter)
        //{
        //    case "Alb":
        //        //DeviceStaticUtils.SelectedAlbumOne = song.AlbumId;
        //        //await Shell.Current.GoToAsync(nameof(AlbumPage), true);
        //        return;
        //    default:
        //        break;
        //}
        //if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        //{
        //    await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        //}
    }

    private bool isOnFocusMode;

    List<SongModelView> songsToDisplay = new();

    private void ArtistsChip_Clicked(object sender, EventArgs e)
    {

    }
    private string _currentSortProperty = string.Empty;
    private SortOrder _currentSortOrder = SortOrder.Asc;

    private void Sort_Clicked(object sender, EventArgs e)
    {
        // Or whatever your SfChip type is
        if (sender is not SfChip chip || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString()!;
        if (string.IsNullOrEmpty(sortProperty))
            return;


        SortOrder newOrder;
        if (_currentSortProperty == sortProperty)
        {
            // Toggle order if sorting by the same property again
            newOrder = (_currentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;
        }
        else
        {
            // Default to ascending when sorting by a new property
            newOrder = SortOrder.Asc;
        }

        // Update current sort state
        _currentSortProperty = sortProperty;
        _currentSortOrder = newOrder;

        MyViewModel.SearchSongSB_TextChanged($"{_currentSortOrder} {_currentSortProperty}");
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)

        // Optional: Scroll to top after sorting
        // if (SongsColView.CurrentItems.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }

    public class SortHeaderClass
    {
        public string SortProperty { get; set; } = string.Empty;
        public bool IsAscending { get; set; }

        public List<SortHeaderClass> DefaultHeaders { get; set; } = new List<SortHeaderClass>
    {
        new SortHeaderClass { SortProperty = "Title", IsAscending = true },
        new SortHeaderClass { SortProperty = "Artist", IsAscending = true },
        new SortHeaderClass { SortProperty = "Album", IsAscending = true },
        new SortHeaderClass { SortProperty = "Genre", IsAscending = true },
        new SortHeaderClass { SortProperty = "Duration", IsAscending = true },
        new SortHeaderClass { SortProperty = "Year", IsAscending = true },
        new SortHeaderClass { SortProperty = "DateAdded", IsAscending = true }
    };

        public SortHeaderClass() { }
    }

    private void Filter_Clicked(object sender, EventArgs e)
    {

    }


    private void DataPointSelectionBehavior_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangedEventArgs e)
    {
        Debug.WriteLine(e.NewIndexes);
        Debug.WriteLine(e.NewIndexes.GetType());

    }

    private void AddToPlaylistClicked(object sender, EventArgs e)
    {
        //PlaylistPopup.IsOpen = !PlaylistPopup.IsOpen;
        //MyViewModel.ActivePlaylistModel
    }
    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        try
        {
            //Debug.WriteLine(SongsColView.ItemsSource.GetType());
            var obsCol = SongsColView.ItemsSource as ReadOnlyObservableCollection<SongModelView>;

            var index = obsCol.ToList();
            var iind = index.FindIndex(x => x.Id== MyViewModel.CurrentPlayingSongView.Id);
            if (iind<0)
            {
                return;
            }
            SongsColView.ScrollTo(index: iind, -1, ScrollToPosition.Start, true);

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void PlaylistsChip_Clicked(object sender, EventArgs e)
    {
    }

 




    private void QuickSearchAlbum_Clicked(object sender, EventArgs e)
    {

        SearchSongSB_Clicked(sender, e);
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", ((MenuFlyoutItem)sender).CommandParameter.ToString()));

    }

    private async void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        var val = song.OtherArtistsName;
        char[] dividers = [',', ';', ':', '|', '-'];

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
            .Select(name => name.Trim())                           // Trim whitespace from each name
            .ToArray();                                             // Convert to a List
        if (namesList  is not null && namesList.Length==1)
        {
            SearchSongSB_Clicked(sender, e);
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", namesList[0]));

            return;
        }
        var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

        if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
        {
            return;
        }

        SearchSongSB_Clicked(sender, e);
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));

        return;
    }

    private void PointerRecog_PointerEntered(object sender, PointerEventArgs e)
    {
        

    }


    private async void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
        

    }


    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds
    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

        private CancellationTokenSource _lyricsCts;
        private bool _isLyricsProcessing = false;
        private async void RefreshLyrics_Clicked(object sender, EventArgs e)
        {
            var res = await DisplayAlert("Refresh Lyrics", "This will process all songs in the library to update lyrics. Do you want to continue?", "Yes", "No");

            if (!res)
            {
                return; // User cancelled the operation
            }


            if (_isLyricsProcessing)
            {
                bool cancel = await DisplayAlert("Processing...", "Lyrics are already being processed. Cancel the current operation?", "Yes, Cancel", "No");
                if (cancel)
                {
                    _lyricsCts?.Cancel();
                }
                return;
            }

            _isLyricsProcessing = true;
            MyProgressBar.IsVisible = true; // Show a progress bar
            MyProgressLabel.IsVisible = true; // Show a label



            _lyricsCts = new CancellationTokenSource();



            var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
            {
                MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
                MyProgressLabel.Text = $"Processing: {progress.CurrentFile}";
            });

            try
            {

                await MyViewModel.LoadSongDataAsync(progressReporter, _lyricsCts);
                await DisplayAlert("Complete", "Lyrics processing finished!", "OK");
            }
            catch (OperationCanceledException)
            {
                await DisplayAlert("Cancelled", "The operation was cancelled.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
            }
            finally
            {
                _isLyricsProcessing = false;
                MyProgressBar.IsVisible = false;
                MyProgressLabel.IsVisible = false;
            }
        }


    private void Label_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {

    }

    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }

    private void MiddleClickGest_PointerReleased(object sender, PointerEventArgs e)
    {
        var send = (Label)sender;
        var gestRec = send.GestureRecognizers[0] as PointerGestureRecognizer;
        var field = gestRec.PointerEnteredCommandParameter as string;
        var valuee = gestRec.PointerReleasedCommandParameter as string;


        Microsoft.UI.Input.PointerDeviceType ee = e.PlatformArgs.PointerRoutedEventArgs.Pointer.PointerDeviceType;
        Windows.System.VirtualKeyModifiers ewe = e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers;

        if (ewe==Windows.System.VirtualKeyModifiers.Control || ewe==Windows.System.VirtualKeyModifiers.Menu|| ewe==Windows.System.VirtualKeyModifiers.Shift && ee==Microsoft.UI.Input.PointerDeviceType.Mouse)
        {
        }
    }
    private void ArtistSfEffectsView_TouchUp(object sender, EventArgs e)
    {

    }

    private void ResetChip_Clicked(object sender, EventArgs e)
    {

    }
    /*
     * 
     *    private void SongsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }
    private Compositor _compositor;
    private Visual _scrollViewerContentVisual; // The visual we will animate for scrolling
    private WinUIControls.ListView? _nativeListView;
    private void AllLyricsColView_Loaded(object sender, EventArgs e)
    {
        if (AllLyricsColView.Handler?.PlatformView is WinUIControls.ListView nativeListView)
        {

            return;
            _nativeListView = nativeListView;


        }
    }

    private async void PointerRecog_PointerExited(object sender, PointerEventArgs e)
    {
        await Task.WhenAll(SongsGrid.DimmIn(),
            TranslatedSearch.DimmOut(),
             AdvSearch.DimmOutCompletelyAndHide(),
            UtilitySection.DimmInCompletelyAndShow
            ()
            );
        SearchSongSB.Unfocus();
    }
    private async void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

        await Task.WhenAll(SongsGrid.DimmOut(),
             AdvSearch.DimmInCompletelyAndShow(),
            SearchSongSB.AnimateHeight(65, 650, Easing.SpringOut));

    }



    private async void AllEvents_Clicked(object sender, EventArgs e)
    {
        this.IsBusy=true;
        if (!AllEventsBorder.IsVisible)
        {
            MyViewModel.GetStatsGeneral();
            await Task.WhenAll(AllEventsBorder.AnimateFadeInFront(400), SearchSection.AnimateFadeOutBack(400), SongsGrid.AnimateFadeOutBack(400), LyricsView.AnimateFadeOutBack(400));

        }
        else
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeOutBack(400), SearchSection.AnimateFadeInFront(400), SongsGrid.AnimateFadeInFront(400), LyricsView.AnimateFadeOutBack(400));


        }
        this.IsBusy=false;

    }

    private void DataPointSelectionBehavior_SelectionChanging(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangingEventArgs e)
    {
        var ee = e.NewIndexes;
        var old = e.OldIndexes;

        var pie = sender as PieSeries;
        if (e.NewIndexes is null || e.NewIndexes.Count<1)
        {
            return;

        }
        var indexx = e.NewIndexes[0];
        var itemm = pie.ItemsSource as ObservableCollection<DimmerStats>;
        if (itemm is null)
        {
            return;
        }
        var songg = itemm[indexx];
        if (songg is null)
        {
            return;
        }
        if (songg.Song is null)
        {
            return;
        }
        var sss = MyViewModel.SearchResults.First(x => x.Id==songg.Song.Id);


        MyViewModel.SelectedSong=sss;
        ColViewOfTopSongs.SelectedItem=       songg;
        Debug.WriteLine($"Selected Song: {songg.Song.Title}, Played {songg.Count} times.");

        //var actSong=((PieSeries)sender)
        Debug.WriteLine(ee);
        Debug.WriteLine(old);

    }

    private async void returnHome_Clicked(object sender, EventArgs e)
    {

        if (!AllEventsBorder.IsVisible)
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeInFront(400), MainUI.AnimateFadeOutBack(400));

        }
        else
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeOutBack(400), MainUI.AnimateFadeInFront(400));


        }

    }

    private void savee_Clicked(object sender, EventArgs e)
    {

    }
    
    */
    private void LyricsChip_Clicked(object sender, EventArgs e)
    {
        //await Task.WhenAll(LyricsView.AnimateFadeInFront(400), SongsGrid.AnimateFadeOutBack(300), AllEventsBorder.AnimateFadeOutBack(300));
    }
    private void ArtistsEffectsView_LongPressed_1(object sender, EventArgs e)
    {

    }

    private void ArtistsEffectsView_TouchDown(object sender, EventArgs e)
    {

    }

    private void ViewUtilsBtn_Clicked(object sender, EventArgs e)
    {
    }


    private void Button_Clicked_1(object sender, EventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(string.Empty);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var view = (Microsoft.Maui.Controls.View)sender;
        var gestRec = view.GestureRecognizers[0] as TapGestureRecognizer;
        MyViewModel.SelectedSong=gestRec.CommandParameter as SongModelView;
    }


    private void CloseSideBar_Clicked(object sender, EventArgs e)
    {

    }

    private void SidePage_DragOver(object sender, DragEventArgs e)
    {

    }

    private void SidePage_Drop(object sender, DropEventArgs e)
    {
        var dd = e.Data;
        var s = dd.Properties.Values;
        var ss = dd.Properties.Keys;
    }

    private void SongDrag_DragStarting(object sender, DragStartingEventArgs e)
    {
        var dd = e.Data;
        var ee = dd.View;
        var re = dd.Properties;
        dd.Text = "Drop at the to view";
    }

    private async void SongViewPointer_PointerExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.FadeOut(700, 0.7);
    }

    private async void SongViewPointer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.FadeIn(300, 0.7);
    }

    private async void OnAddQuickNoteClicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        if (song is null)
        {
            return;
        }
        // Prompt the user for a note

       
      await  MyViewModel.SaveUserNoteToSong(song);


    }

    private async void OnLabelClicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;

        var param = send.CommandParameter as string;

        if (song is null && param is not null)
        {
            return;
        }

        switch (param)
        {
            case "DeleteSys":

                var listOfSongsToDelete = new List<SongModelView> { song };

                await MyViewModel.DeleteSongs(listOfSongsToDelete);
                break;
            case "OpenFileExp":

                await MyViewModel.OpenFileInOtherApp(song);
                break;

            default:
                break;
        }
    }

    private void Button_Clicked_2(object sender, EventArgs e)
    {

    }

    private void AddFilter_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddFilterCommand.Execute(null);
    }

    private void AllEvents_Clicked(object sender, EventArgs e)
    {

    }

    private  void TopExpander_Collapsed(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {


    }
    private void TopExpander_Expanding(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandingAndCollapsingEventArgs e)
    {
        
    }
    private  void TopExpander_Expanded(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {
      

    }

    private void TopExpander_Collapsing(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandingAndCollapsingEventArgs e)
    {
     

    }

  
    private void OnToggleTopViewClicked(object sender, EventArgs e)
    {
        
    }
    private  void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        var properties = e.GetCurrentPoint(nativeElement).Properties;

        if (properties.IsRightButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            
           
            // Handle mouse button 4
        }
    }


  

    private void SearchBtn_Clicked(object sender, EventArgs e)
    {
       
    }

    private async void OnNavigateToExperimentalPage(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ExperimentsPage), true);
    }

    private async void SongDropRecognizer_DragLeave(object sender, Microsoft.Maui.Controls.DragEventArgs e)
    {
await this.FadeIn(500, 1.0);
  }

    private async void SongDropRecognizer_DragOver(object sender, Microsoft.Maui.Controls.DragEventArgs e)
    {
        await this.FadeOut(500, 0.8);
        var dragData = e.PlatformArgs.DragEventArgs.DataView;
        var dUI = e.PlatformArgs.DragEventArgs.DragUIOverride;
        var dataaF = dragData.AvailableFormats;
        // Set the visual feedback for the user
        dUI.IsCaptionVisible = true;
        dUI.IsGlyphVisible = true;
        dUI.IsContentVisible = true;
        
        // --- STRATEGY: CHECK FOR INTERNAL DRAG FIRST, THEN EXTERNAL ---
        
        // 1. CHECK FOR INTERNAL DRAG: Do we have our custom text format?
        // DataView is the universal container for dragged data.
        if (dragData.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
        {
            // IMPORTANT: We cannot read the data content in DragOver. It's too slow.
            // We can only check for the PRESENCE of the format. 
            // We'll trust our own app to have formatted it correctly.
            // A better way is to define a custom data format.
            e.AcceptedOperation = DataPackageOperation.Copy;
            dUI.Caption = "Add to Queue";
            return; // We've made our decision.
        }

        // 2. CHECK FOR EXTERNAL DRAG: Are there files being dragged from Explorer?
        if (dragData.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            // We can't get the items here, but we can tell the system we are prepared to handle them.
            // We'll do the actual file type check in the Drop event.
            // For now, we optimistically accept the drag.
            e.AcceptedOperation = DataPackageOperation.Copy;
            dUI.Caption = "Add Songs to Library";
            return;
        }

        // 3. REJECT: If neither format is present, reject the drop.
        e.AcceptedOperation = DataPackageOperation.None;
        dUI.Caption = "Cannot drop here";



        //if (dataa.Count < 1)
        //{
        //    return;
        //}
        //// keep to internal list to show unsupported file type in alert later
        //if (MyViewModel.DraggedAudioFiles is null)
        //{
        //    MyViewModel.DraggedAudioFiles = new List<string>();
        //}

        //MyViewModel.DraggedAudioFiles.Clear();
        
        //// Check if the dragged items are audio files
        //foreach (var item in dataa)
        //{
            
        //    if (item is StorageFile file)
        //    {
        //        var fileExtension = Path.GetExtension(file.Path);
        //        if (!SupportedAudioExtensions.Contains(fileExtension))
        //        {
                    
        //            await DisplayAlert("Unsupported File Type", $"The file '{file.Name}' is not a supported audio format.", "OK");
        //            // Set the accepted operation to None to reject the drop
        //            e.PlatformArgs.DragEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        //            return;
        //        }
        //        else
        //        {
        //            // store all audio files in a list for process in drop event
        //            MyViewModel.DraggedAudioFiles.Add(file.Path);
        //        }
        //    }
        //}
        //// If all files are audio files, accept the drop
        //e.PlatformArgs.DragEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
     
    }
    private async Task ShowInvalidFilesDialog(List<string> invalidFiles)
    {
        var dialog = new WinUIControls.ContentDialog
        {
            Title = "Unsupported File Types",
            Content = $"The following files are not supported and were ignored:\n\n{string.Join("\n", invalidFiles)}",
            CloseButtonText = "OK",
        };
        await dialog.ShowAsync();
    }
    private async void SongDropRecognizer_Drop(object sender, DropEventArgs e)
    {
        await this.FadeIn(500, 1.0);
        if (e.PlatformArgs is null)
        {
            return; // Drop was not accepted, so we do nothing.
        }

        var deferral = e.PlatformArgs.DragEventArgs.GetDeferral();

        try
        {
            // --- STRATEGY: PROCESS DATA BASED ON THE FORMAT, INTERNAL FIRST ---

            // 1. HANDLE INTERNAL DRAG
            if (e.PlatformArgs.DragEventArgs.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var textData = await  e.PlatformArgs.DragEventArgs.DataView.GetTextAsync();
                if (textData.StartsWith("songids:"))
                {
                    var idString = textData.Substring("songids:".Length);
                    var songIds = idString.Split(',').ToList();

                    
                    MyViewModel.AddSongsByIdsToQueue(songIds); // Example action
                    return; // Job done.
                }
            }

            // 2. HANDLE EXTERNAL DRAG
            if (e.PlatformArgs.DragEventArgs.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync();
                if (items.Any())
                {
                    var supportedAudioExtensions = new HashSet<string>(
                      [".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus"],
                      StringComparer.OrdinalIgnoreCase
                    );

                    var validFiles = new List<string>();
                    var invalidFiles = new List<string>();

                    foreach (var item in items)
                    {
                        if (item is StorageFile file)
                        {
                            if (supportedAudioExtensions.Contains(file.FileType))
                            {
                                validFiles.Add(file.Path);
                            }
                            else
                            {
                                invalidFiles.Add(file.Name);
                            }
                        }
                    }

                    // Process the valid files
                    if (validFiles.Any())
                    {
                        
                        MyViewModel.AddMusicFoldersByPassingToService(validFiles);
                    }

                    // Inform the user about any invalid files
                    if (invalidFiles.Any())
                    {
                        await ShowInvalidFilesDialog(invalidFiles);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred while processing the dropped files: {ex.Message}", "OK");

            // Optionally show an error dialog to the user.
        }
        finally
        {
            // IMPORTANT: Complete the deferral.
            deferral.Complete();
        }
        //MyViewModel.AddMusicFoldersByPassingToService(MyViewModel.DraggedAudioFiles);
    }

    private async void SongViewPointer_PointerPressed(object sender, PointerEventArgs e)
    {

        try
        {

            this.IsEnabled=false;
            var send = (View)sender;


            var gest = send.GestureRecognizers[0] as PointerGestureRecognizer;

            var pointerParamPressed = gest.PointerPressedCommandParameter as SongModelView;
            var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement).Properties;


            if (properties.IsRightButtonPressed)
            {
                this.IsEnabled=true;
                return; // Do not process right-clicks here, let the context menu handle it

            }
            else if (properties.IsMiddleButtonPressed)
            {
                // Handle Middle Click

                PlaySongGestRec_Tapped(send, null);
                this.IsEnabled=true;
                return;
            }
            else if (properties.IsXButton2Pressed)
            {

            }
            await MyViewModel.ProcessAndMoveToViewSong(pointerParamPressed);
            this.IsEnabled=true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void PlayBtn_Clicked(object sender, EventArgs e)
    {
        PlaySongGestRec_Tapped(sender, null);
    }

    private void SongsColView_Scrolled(object sender, ItemsViewScrolledEventArgs e)
    {
        var ee = e.FirstVisibleItemIndex;
        var eew = e.CenterItemIndex;
        var eeq = e.LastVisibleItemIndex;
        
    }

    private void SongsColView_ScrollToRequested(object sender, ScrollToRequestEventArgs e)
    {
        var ee = e.Item;
        ScrollToMode ww = e.Mode;
        var www = e.Index;
    }

    private void ViewGenreMFI_Clicked(object sender, EventArgs e)
    {

    }

    private void SearchSongSB_Clicked(object sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var win = winMgr.GetOrCreateUniqueWindow(MyViewModel,windowFactory: () => new AllSongsWindow(MyViewModel));

        // move and resize to the center of the screen

        var pres = win?.AppWindow.Presenter;
        //window.SetTitleBar()
        if (pres is OverlappedPresenter p)
        {
            p.IsResizable = true;
            p.SetBorderAndTitleBar(true,true); // Remove title bar and border
            p.IsAlwaysOnTop = false;
        }


        Debug.WriteLine(win?.AppWindow.IsShownInSwitchers);//VERY IMPORTANT FOR WINUI 3 TO SHOW IN TASKBAR

    }

    private void SongsColView_Unloaded(object sender, EventArgs e)
    {

    }

    private void ColvViewGest_PointerEntered(object sender, PointerEventArgs e)
    {
        
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {

    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {

    }

    private void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
    {

    }

    private void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {

    }

    private void PlayNext_Clicked(object sender, EventArgs e)
    {

    }

    private void AddQueue_Clicked(object sender, EventArgs e)
    {

    }

    private void ViewArtist_Clicked(object sender, EventArgs e)
    {

    }

    private void PointerGestureRecognizer_PointerPressed_1(object sender, PointerEventArgs e)
    {

    }

    private void GlobalColView_PointerPressed(object sender, PointerEventArgs e)
    {
        var sendd = (View)sender;
        
        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(nativeElement).Properties;

        if (properties.IsMiddleButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            ScrollToSong_Clicked(sender, e);
            return;


        }


    }

    private void BlackListSong_Clicked(object sender, EventArgs e)
    {

    }

    private void CopySongs_Clicked(object sender, EventArgs e)
    {

    }

    private async void DeleteSongs_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.DeleteSongs(MyViewModel.SearchResults);
    }

    private void SearchBtn_Loaded(object sender, EventArgs e)
    {
      

        //SearchBtn.Behaviors.Add(new Microsoft.);
    }

    private void SearchBtn_Unloaded(object sender, EventArgs e)
    {
        SearchBtn.Behaviors.Clear();
    }

    private void ViewSongDetails_Clicked(object sender, EventArgs e)
    {
        MainPagePopup.IsOpen = !MainPagePopup.IsOpen;
    }

  
}

public class SongViewTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ListTemplate { get; set; }
    public DataTemplate? GridTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        // 'container' in MAUI is typically the ContentPresenter or ViewCell for the item.
        // We need to walk up the tree from this container to find the CollectionView.
        var collectionView = FindParent<CollectionView>(container);

        if (collectionView == null)
        {
            // If we can't find the CollectionView, we can't determine the mode.
            // Fall back to a default template.
            return GridTemplate!;
        }

        // Once we have the CollectionView, we can get its BindingContext, which is our ViewModel.
        if (collectionView.BindingContext is BaseViewModelWin viewModel)
        {
            // THE MAGIC: Check the CurrentViewMode property on the ViewModel
            // and return the appropriate template.
            return viewModel.CurrentViewMode == CollectionViewMode.Grid
                ? GridTemplate!
                : ListTemplate!;
        }

        // If the BindingContext isn't the expected ViewModel, fall back to the default.
        return GridTemplate!;
    }

    /// <summary>
    /// A helper method to traverse the MAUI logical tree upwards to find a parent of a specific type.
    /// This is the MAUI equivalent of traversing the visual tree.
    /// </summary>
    private T? FindParent<T>(BindableObject bindable) where T : BindableObject
    {
        Element? parent = bindable as Element;

        while (parent != null)
        {
            // Check if the current parent is the type we're looking for.
            if (parent is T correctlyTyped)
            {
                return correctlyTyped;
            }

            // Move up to the next parent in the logical tree.
            parent = parent.Parent;
        }

        // If we reach the top without finding the parent, return null.
        return null;
    }
}


