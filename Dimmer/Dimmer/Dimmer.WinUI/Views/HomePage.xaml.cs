﻿//using Dimmer.DimmerLive.Models;
using Dimmer.DimmerSearch;
using Dimmer.Interfaces.Services.Interfaces;

using DynamicData;
using DynamicData.Binding;
using Compositor = Microsoft.UI.Composition.Compositor;
using Visual = Microsoft.UI.Composition.Visual;
using Microsoft.Maui.Controls.Internals;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

using MoreLinq;

using ReactiveUI;

using System.DirectoryServices;
using System.Numerics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using Windows.UI.Composition;

using SortOrder = Dimmer.Utilities.SortOrder;
using WinUIControls = Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CompositionBatchTypes = Microsoft.UI.Composition.CompositionBatchTypes;
using CompositionEasingFunction = Microsoft.UI.Composition.CompositionEasingFunction;
using Syncfusion.Maui.Toolkit.Charts;
using Label = Microsoft.Maui.Controls.Label;
using Dimmer.DimmerLive;
using DataTemplate = Microsoft.Maui.Controls.DataTemplate;
using DragStartingEventArgs = Microsoft.Maui.Controls.DragStartingEventArgs;
using View = Microsoft.Maui.Controls.View;
using DragEventArgs = Microsoft.Maui.Controls.DragEventArgs;

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{


    public BaseViewModelWin MyViewModel { get; internal set; }
 


    public HomePage(BaseViewModelWin vm)
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
    }
  
    private List<DataTemplate> _availableLayouts; 
    private readonly List<IItemsLayout> _availableItemsLayouts; 

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
        var ee = e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers;
        if (e.PlatformArgs.PointerRoutedEventArgs.KeyModifiers != Windows.System.VirtualKeyModifiers.Control)
        {
            return;
        }
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


            var res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", string.Empty, namesList);

            if (string.IsNullOrEmpty(res))
            {
                return;
            }
            SearchSongSB.Text = StaticMethods.SetQuotedSearch("artist", res);

            return;
        }

        SearchSongSB.Text = StaticMethods.SetQuotedSearch(field, val);

    }


    ////////








    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPageContext = CurrentPage.HomePage;
    }


    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {

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
      await  MyViewModel.PlaySong(song);
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

        var chip = sender as SfChip; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
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


    private static async void ViewSong_Clicked(object sender, EventArgs e)
    {

        var song = (SongModelView)((MenuFlyoutItem)sender).CommandParameter;

        DeviceStaticUtils.SelectedSongOne = song;
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
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
        SearchSongSB.Text= ((MenuFlyoutItem)sender).CommandParameter.ToString();
        SearchSongSB.Focus();
    }

    private void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {

    }

    private async void PointerRecog_PointerEntered(object sender, PointerEventArgs e)
    {
        SearchSongSB.Focus();
        await Task.WhenAll(
            TranslatedSearch.DimmIn(),
            UtilitySection.DimmInCompletelyAndShow()
            );


    }


    private async void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
        await Task.WhenAll(
         SearchSongSB.AnimateHeight(30, 500, Easing.SpringIn));

        SearchSongSB.FontSize = 16;

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
            SearchSongSB.Text= StaticMethods.SetQuotedSearch(field.ToString(), valuee);
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

    private void AllLyricsColView_SelectionChanged_1(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        var newItem = e.CurrentSelection;
        if (newItem.Count > 0)
        {

            AllLyricsColView.ScrollTo(item: newItem[0], ScrollToPosition.Start, animate: true);
        }
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
    private async void LyricsChip_Clicked(object sender, EventArgs e)
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
        UtilitiesHSL.IsVisible=!UtilitiesHSL.IsVisible;
    }

    private void ViewNPQ_Clicked(object sender, EventArgs e)
    {
        SearchSongSB.Text=MyViewModel.CurrentPlaybackQuery;
        return;



    }

    private void Button_Clicked_1(object sender, EventArgs e)
    {
        SearchSongSB.Text=string.Empty;
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
        await send.FadeOut(300, 0.5);
    }

    private async void SongViewPointer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.FadeIn(300, 0.3);
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

       
      await  MyViewModel.SaveUserNoteToDbLegacy(song);


    }

    private void OnLabelClicked(object sender, EventArgs e)
    {

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

    private void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

    }

    private async void TopExpander_Collapsed(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {

        var topView = TopExpander.Header;


        await Task.WhenAll(Task.Delay(200),topView.SlideInFromLeft(700), 
            TopViewBtmpart.BounceIn(200), LeftUtilSide.BounceIn(200),RightUtilSide.BounceIn(200));

    }
    private void TopExpander_Expanding(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandingAndCollapsingEventArgs e)
    {
        
    }
    private async void TopExpander_Expanded(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandedAndCollapsedEventArgs e)
    {
        var topView = TopExpander.Header;
        await Task.WhenAll(topView.SlideOutToRight(1000),
        TopViewBtmpart.BounceOut(500), LeftUtilSide.BounceOut(200), RightUtilSide.BounceOut(200));

    }

    private void TopExpander_Collapsing(object sender, Syncfusion.Maui.Toolkit.Expander.ExpandingAndCollapsingEventArgs e)
    {
     

    }

    private void CloseTopExpander_PointerPressed(object sender, PointerEventArgs e)
    {

        var arggs = e.PlatformArgs.PointerRoutedEventArgs;
        OnGlobalPointerPressed(sender, arggs);
    }
    private void OnToggleTopViewClicked(object sender, EventArgs e)
    {
        TopExpander.IsExpanded = !TopExpander.IsExpanded;
        
    }
    private  void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        var properties = e.GetCurrentPoint(nativeElement).Properties;

        if (properties.IsRightButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            
            TopExpander.IsExpanded = false;
            // Handle mouse button 4
        }
    }


    private async void TransferSession_Clicked(object sender, EventArgs e)
    {
        bool isOkToTransfer = await MyViewModel.IsUserOkayForTransfer();
        if (isOkToTransfer)
        {
            await Task.WhenAll(GridSongsColView.DimmOutCompletelyAndHide(), ShareSongView.DimmInCompletelyAndShow());
        }
        else
        {
            var result = await DisplayAlert("No Account found", "Log in to use Dimmer session transfer?", "OK", "Cancel");
            if (result)
            {
                await DisplayAlert("title","Now performing sign up","OK");
            }

        }
    }

    private async void SearchBtn_Clicked(object sender, EventArgs e)
    {
        if(!QuickSearchBar.IsVisible)
        {
            await QuickSearchBar.BounceIn(500);
            
            QuickSearchBar.Focus();
        }
        else
        {
            await QuickSearchBar.BounceOut(500);
            QuickSearchBar.Unfocus();
        }
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
        var dataa = await dragData.GetStorageItemsAsync();
      var  SupportedAudioExtensions = new HashSet<string>(
          new[] { ".mp3", ".flac", ".wav", ".m4a", ".aac", ".ogg", ".opus" },
          StringComparer.OrdinalIgnoreCase
      );
        if (dataa.Count < 1)
        {
            return;
        }
        // keep to internal list to show unsupported file type in alert later
        if (MyViewModel.DraggedAudioFiles is null)
        {
            MyViewModel.DraggedAudioFiles = new List<string>();
        }

        MyViewModel.DraggedAudioFiles.Clear();
        
        // Check if the dragged items are audio files
        foreach (var item in dataa)
        {
            
            if (item is StorageFile file)
            {
                var fileExtension = Path.GetExtension(file.Path);
                if (!SupportedAudioExtensions.Contains(fileExtension))
                {
                    
                    await DisplayAlert("Unsupported File Type", $"The file '{file.Name}' is not a supported audio format.", "OK");
                    // Set the accepted operation to None to reject the drop
                    e.PlatformArgs.DragEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                    return;
                }
                else
                {
                    // store all audio files in a list for process in drop event
                    MyViewModel.DraggedAudioFiles.Add(file.Path);
                }
            }
        }
        // If all files are audio files, accept the drop
        e.PlatformArgs.DragEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
     
    }

    private void SongDropRecognizer_Drop(object sender, DropEventArgs e)
    {
        // songs dropped so we can load in viewmodel
        if (MyViewModel.DraggedAudioFiles is null || MyViewModel.DraggedAudioFiles.Count < 1)
        {
            return; // No files to process
        }
        // Process the dropped audio files
        MyViewModel.AddMusicFoldersByPassingToService(MyViewModel.DraggedAudioFiles);
    }

    private void SongViewPointer_PointerPressed(object sender, PointerEventArgs e)
    {

    }
}