//using Dimmer.DimmerLive.Models;
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

        MyViewModel.TranslatedSearch= TranslatedSearch;
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

    private void PlaySongGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);
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
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

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
             AdvSearch.DimmOutCompletelyAndHide(),
            UtilitySection.DimmInCompletelyAndShow
            ()
            );
        SearchSongSB.Unfocus();
    }
    private async void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {

        await Task.WhenAll(SongsColView.DimmOut(),
             AdvSearch.DimmInCompletelyAndShow(),
            SearchSongSB.AnimateHeight(65, 650, Easing.SpringOut));

    }

    private async void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
        await Task.WhenAll(
         SearchSongSB.AnimateHeight(30, 500, Easing.SpringIn));

        SearchSongSB.FontSize = 16;

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
        if (!AllEventsBorder.IsVisible)
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeInFront(400), SearchSection.AnimateFadeOutBack(400), SongsView.AnimateFadeOutBack(400), LyricsView.AnimateFadeOutBack(400));

        }
        else
        {
            await Task.WhenAll(AllEventsBorder.AnimateFadeOutBack(400), SearchSection.AnimateFadeInFront(400), SongsView.AnimateFadeInFront(400), LyricsView.AnimateFadeOutBack(400));


        }

        MyViewModel.LoadStatsApp();

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

    private async void LyricsChip_Clicked(object sender, EventArgs e)
    {
        await Task.WhenAll(LyricsView.AnimateFadeInFront(400), SongsView.AnimateFadeOutBack(300), AllEventsBorder.AnimateFadeOutBack(300));
    }

    private void ArtistsEffectsView_LongPressed_1(object sender, EventArgs e)
    {

    }

    private void ArtistsEffectsView_TouchDown(object sender, EventArgs e)
    {

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

    private void ViewUtilsBtn_Clicked(object sender, EventArgs e)
    {
        UtilitiesHSL.IsVisible=!UtilitiesHSL.IsVisible;
    }

    private async void ViewNPQ_Clicked(object sender, EventArgs e)
    {

        MyViewModel.SearchSongSB_TextChanged(MyViewModel.CurrentPlaybackQuery);

        return;

        if (!SongsColView.IsVisible)
        {
            await Task.WhenAll(SongsColView.DimmInCompletelyAndShow(300), SingleSongView.DimmOutCompletelyAndHide(250));
        }
        else
        {

            await Task.WhenAll(SingleSongView.DimmInCompletelyAndShow(300), SongsColView.DimmOutCompletelyAndHide(250));
        }

    }
}
