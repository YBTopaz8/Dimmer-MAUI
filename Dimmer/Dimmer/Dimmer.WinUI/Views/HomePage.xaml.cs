//using Dimmer.DimmerLive.Models;
using Dimmer.DimmerSearch;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.FileProcessorUtils;

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

using Windows.UI.Composition;

using SortOrder = Dimmer.Utilities.SortOrder;
using WinUIControls = Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using CompositionBatchTypes = Microsoft.UI.Composition.CompositionBatchTypes;
using CompositionEasingFunction = Microsoft.UI.Composition.CompositionEasingFunction;

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    private void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(e.NewTextValue);
        // Optional: Update a summary label
        //SummaryLabel.Text = query.Humanize();
    }


    public HomePage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;


        // --- Keep these lines. They correctly wire up the UI. ---

        //MyViewModel.TranslatedSearch= TranslatedSearch;
        //MyViewModel.SongsCountLabel = SongsCountLabel;

    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }











    protected override void OnAppearing()
    {
        base.OnAppearing();
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

    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        await MyViewModel.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
    }

    private void SkipPrev_Clicked(object sender, EventArgs e)
    {

    }

    private void PlayPauseBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void SkipNext_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenSongStats_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenArtistWindow_Clicked(object sender, EventArgs e)
    {

    }

    private void OpenAlbumWindow_Clicked(object sender, EventArgs e)
    {

    }

    private async void CurrPlayingSongGesRec_Tapped(object sender, TappedEventArgs e)
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
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    List<SongModelView> songsToDisplay = new();

    private void ArtistsChip_Clicked(object sender, EventArgs e)
    {

    }
    private string _currentSortProperty = string.Empty;
    private SortOrder _currentSortOrder = SortOrder.Asc;

    private async void Sort_Clicked(object sender, EventArgs e)
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

    private async void StatsSfChip_Clicked(object sender, EventArgs e)
    {
        if (SongsView.IsVisible)
        {
            MyViewModel.LoadStatsApp();

            await Task.WhenAll(SongsView.AnimateFadeOutBack(400), StatsView.AnimateFadeInFront(300));


        }
        else
        {
            await Task.WhenAll(SongsView.AnimateFadeInFront(300), StatsView.AnimateFadeOutBack(400));


        }
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

    private void SongsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
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

    private async void PointerRecog_PointerExited(object sender, PointerEventArgs e)
    {
        await Task.WhenAll(SongsColView.DimmIn(),
            TranslatedSearch.DimmOut(),
             AdvSearch.DimmInCompletelyAndShow(),
            UtilitySection.DimmOutCompletelyAndHide
            ()
            );
        SearchSongSB.Unfocus();
    }
    private async void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

        await Task.WhenAll(SongsColView.DimmOut(),
             AdvSearch.DimmInCompletelyAndShow(),
            SearchSongSB.AnimateHeight(150, 650, Easing.SpringOut));

    }

    private async void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
        await Task.Delay(500);
        await Task.WhenAll(SongsColView.DimmIn(),
     TranslatedSearch.DimmOut(), AdvSearch.DimmOutCompletelyAndHide(), UtilitySection.DimmOutCompletelyAndHide(),
         SearchSongSB.AnimateHeight(50, 500, Easing.SpringIn));

        SearchSongSB.FontSize = 17;

    }


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
            MyViewModel.SearchSongSB_TextChanged(string.Empty); // Clear the search bar to refresh the list
            var songsToRefresh = MyViewModel.SearchResults; // Or your full master list
            var lryServ = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
            await SongDataProcessor.ProcessLyricsAsync(songsToRefresh, lryServ, progressReporter, _lyricsCts.Token);

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

        Debug.WriteLine(AllEventsColView.ItemsSource);
        if (!AllEventsBorder.IsVisible)
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeInFront(400), StatsView.AnimateFadeOutBack(400), SongsColView.AnimateFadeOutBack(400));

        }
        else
        {


        }


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
}
