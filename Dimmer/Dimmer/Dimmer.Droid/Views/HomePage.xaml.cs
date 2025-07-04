using DevExpress.Maui.Controls;
using DevExpress.Maui.Core.Internal;
using DevExpress.Maui.Editors;

using Dimmer.Utilities;
using Dimmer.Utilities.CustomAnimations;
using Dimmer.Utilities.FileProcessorUtils;
using Dimmer.ViewModel;

using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Color = Microsoft.Maui.Graphics.Color;

namespace Dimmer.Views;

public partial class HomePage : ContentPage
{

    public BaseViewModelAnd MyViewModel { get; internal set; }
    public HomePage(BaseViewModelAnd vm)
    {
        InitializeComponent();
        MyViewModel=vm;

        //MyViewModel!.LoadPageViewModel();
        BindingContext = vm;
        //NavChips.ItemsSource = new List<string> { "Home", "Artists", "Albums", "Genres", "Settings"};
        //NavChipss.ItemsSource = new List<string> { "Home", "Artists", "Albums", "Genres", "Settings" };
        this.HideSoftInputOnTapped=true;


    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel.FiniInit();

        var baseVm = IPlatformApplication.Current.Services.GetService<BaseViewModel>();

    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        MyViewModel.BaseVM.SearchSongSB_TextChanged(SearchBy.Text);

    }

    private void ClosePopup(object sender, EventArgs e)
    {

        //SongsMenuPopup.Close();
    }



    string SearchParam = string.Empty;

    SongModelView selectedSongPopUp = new SongModelView();
    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
        var send = (Chip)sender;
        if (send.BindingContext is not SongModelView paramss)
        {
            return;
        }
        selectedSongPopUp = paramss;


        MyViewModel.BaseVM.SetCurrentlyPickedSongForContext(paramss);



        //SongsMenuPopup.Show();

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.BaseVM.SelectedSongForContext;
        if (song is null)
        {
            return;
        }
        await MyViewModel.BaseVM.SelectedArtistAndNavtoPage(song);

        //await SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
    }

    private async void SongsColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        await MyViewModel.BaseVM.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
        //AndroidTransitionHelper.BeginMaterialContainerTransform(this.RootLayout, HomeView, DetailView);
        //HomeView.IsVisible=false;
        //DetailView.IsVisible=true;

    }
    private void ByTitle()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {

                SongsColView.FilterString = $"Contains([Title], '{SearchBy.Text}')";
            }

        }
    }

    List<SongModelView> songsToDisplay = new();
    private void SortChoose_Clicked(object sender, EventArgs e)
    {

        var chip = sender as DXButton; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        if (string.IsNullOrEmpty(sortProperty))
            return;


        // Update current sort state
        MyViewModel.BaseVM.CurrentSortProperty = sortProperty;


        SortOrder newOrder;

        // Toggle order if sorting by the same property again
        newOrder = (MyViewModel.BaseVM.CurrentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;


        MyViewModel.BaseVM.CurrentSortOrder = newOrder;
        MyViewModel.BaseVM.CurrentSortOrderInt = (int)newOrder;
        // Optional: Update UI to show sort indicators (e.g., change chip appearance)
        bool flowControl = SortIndeed();
        if (!flowControl)
        {
            return;
        }

        // Optional: Scroll to top after sorting
        // if (SongsColView.CurrentItems.Count > 0)
        // {
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.Start, true);
        // }
    }

    private void AddToPlaylist_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        var pl = MyViewModel.BaseVM.AllPlaylists;
        var listt = new List<SongModelView>();
        listt.Add(song);

        MyViewModel.BaseVM.AddToPlaylist("Playlists", listt);
    }

    private void CloseNowPlayingQueue_Tap(object sender, HandledEventArgs e)
    {

        Debug.WriteLine(this.Parent.GetType());
        //this.IsExpanded = !this.IsExpanded;

    }
    private async void DXButton_Clicked_3(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    SortOrder internalOrder = SortOrder.Asc;
    private bool SortIndeed()
    {
        return true;

        MyViewModel.BaseVM.CurrentSortOrderInt = (int)MyViewModel.BaseVM.CurrentSortOrder;

        return true;
    }

    private void SortCategory_LongPress(object sender, HandledEventArgs e)
    {
        SortIndeed();
    }
    private void ByAll()
    {
        ApplyAdvancedFilter();


    }

    // At the top of your class
    private readonly Dictionary<string, string> _fieldMappings = new(StringComparer.OrdinalIgnoreCase)
{
    { "t", "Title" }, { "title", "Title" },
    { "ar", "OtherArtistsName" }, { "artist", "OtherArtistsName" },
    { "al", "AlbumName" }, { "album", "AlbumName" },
    { "year", "ReleaseYear" },
    { "bpm", "BPM" },
    { "len", "DurationInSeconds" }, // Use the property path for sub-properties
    { "explicit", "IsExplicit" }
    // Add other boolean or numeric fields here
};

    // Our master regex that captures all operators and prefixes
    private static readonly Regex _powerSearchRegex = new Regex(
        @"\b(t|title|ar|artist|al|album|year|bpm|len|explicit):(!)?(>|<|\^|\$|~)?(?:""([^""]*)""|(\S+))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private void ApplyAdvancedFilter()
    {
        var searchText = SearchBy.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            SongsColView.FilterString = string.Empty;
            return;
        }

        // Generate the powerful filter string and apply it directly.
        SongsColView.FilterString = GenerateDevExpressFilterString(searchText);
    }

    private string GenerateDevExpressFilterString(string searchText)
    {
        var allFilters = new List<string>();

        var remainingText = _powerSearchRegex.Replace(searchText, match =>
        {
            // 1. Extract all parts from the user's input
            string prefix = match.Groups[1].Value.ToLower();
            bool isNegated = match.Groups[2].Success;
            string op = match.Groups[3].Success ? match.Groups[3].Value : string.Empty;
            string value = match.Groups[4].Success ? match.Groups[4].Value : match.Groups[5].Value;

            if (_fieldMappings.TryGetValue(prefix, out var fieldName))
            {
                string criterion = BuildCriterion(fieldName, op, value);
                if (!string.IsNullOrEmpty(criterion))
                {
                    // Add the final formatted part to our list of filters
                    allFilters.Add(isNegated ? $"NOT ({criterion})" : criterion);
                }
            }
            return string.Empty; // Consume the matched part
        }).Trim();

        // 4. Handle any leftover general text as a broad, fuzzy search
        if (!string.IsNullOrEmpty(remainingText))
        {
            var words = remainingText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var sanitizedWord = word.Replace("'", "''");
                var generalFilter = $"(Contains([Title], '{sanitizedWord}') OR " +
                                    $"Contains([OtherArtistsName], '{sanitizedWord}') OR " +
                                    $"Contains([AlbumName], '{sanitizedWord}'))";
                allFilters.Add(generalFilter);
            }
        }

        // 5. Join all generated conditions with AND to create the final string
        return string.Join(" AND ", allFilters);
    }

    // This helper method builds a single piece of the criteria string
    private string BuildCriterion(string fieldName, string op, string value)
    {
        // IMPORTANT: Sanitize value for DevExpress by doubling single quotes
        var sanitizedValue = value.Replace("'", "''");

        // Handle Text Fields (Title, Artist, Album)
        if (fieldName == "Title" || fieldName == "OtherArtistsName" || fieldName == "AlbumName")
        {
            // OR Condition: artist:drake|wizkid
            if (sanitizedValue.Contains('|'))
            {
                var options = sanitizedValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var orGroup = string.Join(" OR ", options.Select(opt => $"Contains([{fieldName}], '{opt}')"));
                return $"({orGroup})";
            }
            // Other Text Operations
            return op switch
            {
                "^" => $"StartsWith([{fieldName}], '{sanitizedValue}')",
                "$" => $"EndsWith([{fieldName}], '{sanitizedValue}')",
                "~" => $"Contains([{fieldName}], '{sanitizedValue}')", // Note: True fuzzy (Levenshtein) is not supported. Fallback to Contains.
                _ => $"Contains([{fieldName}], '{sanitizedValue}')"
            };
        }

        // Handle Numeric Fields (Year, BPM, Length)
        if (fieldName == "ReleaseYear" || fieldName == "BPM" || fieldName == "DurationInSeconds")
        {
            // Range: year:2010-2020
            if (sanitizedValue.Contains('-'))
            {
                var parts = sanitizedValue.Split('-');

                // --- THE FIX: Validate the parts of the range ---
                // Ensure we have exactly two parts and neither is empty.
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]) && !string.IsNullOrWhiteSpace(parts[1]))
                {
                    // Optional but recommended: Also check if they are valid numbers.
                    // This prevents inputs like "year:abc-def".
                    if (double.TryParse(parts[0], out _) && double.TryParse(parts[1], out _))
                    {
                        return $"[{fieldName}] between ({parts[0]}, {parts[1]})";
                    }
                }
                // If the range is invalid/incomplete, do nothing and return an empty string.
                return string.Empty;
            }
            string finalOp = string.IsNullOrEmpty(op) ? "=" : op;

            // Also validate single numeric values to prevent errors
            if (double.TryParse(sanitizedValue, out _))
            {
                return $"[{fieldName}] {finalOp} {sanitizedValue}";
            }
            return string.Empty;
        }
        // Handle Boolean Fields (IsExplicit)
        if (fieldName == "IsExplicit")
        {
            return $"[{fieldName}] = {value.ToLower()}";
        }

        return string.Empty;
    }
    private void ByArtist()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString = $"Contains([OtherArtistsName], '{SearchBy.Text}')";

            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
    }

    private void Sort_Clicked(object sender, EventArgs e)
    {
        //SortBottomSheet.Show();
    }


    private void SongsColView_LongPress(object sender, CollectionViewGestureEventArgs e)
    {
        SongsColView.Commands.ShowFilteringUIForm.Execute(null);
    }

    private void DXButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.LoadTheCurrentColView(SongsColView);
    }
    private async void ArtistChip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;

        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }

        if (await MyViewModel.BaseVM.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }

        //await this.AnimateFadeOutBack(600);
        //await this.CloseAsync();
    }
    private async void SongTitleChip_Tap(object sender, HandledEventArgs e)
    {
        //await CloseAsync();

        MyViewModel.BaseVM.SelectedSongForContext = MyViewModel.BaseVM.CurrentPlayingSongView;
        //await this.AnimateFadeOutBack(600);

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    //private void SongsColView_Loaded(object sender, EventArgs e)
    //{
    //    //var ss = this.GetPlatformView();
    //    //Debug.WriteLine(ss.Id);
    //    //var ee = ss.GetChildren();
    //    //foreach (var item in ee)
    //    //{
    //    //    Debug.WriteLine(item.Id);
    //    //    Debug.WriteLine(item.GetType());
    //    //}

    //    //var q = ss.GetChildrenInTree();
    //    //foreach (var item in q)
    //    //{
    //    //    Debug.WriteLine(item.Id);
    //    //    Debug.WriteLine(item.GetType());
    //    //}

    //    //var o = ss.GetPlatformParents();
    //    //foreach (var item in o)
    //    //{
    //    //    Debug.WriteLine(item.Id);
    //    //    Debug.WriteLine(item.GetType());
    //    //}


    //    //var nn = SongsColView.GetPlatformView();
    //    //Debug.WriteLine(nn.Id);
    //    //Debug.WriteLine(nn.GetType());

    //}


    private void DXButton_Clicked_2(object sender, EventArgs e)
    {

    }




    /*
   

    private static void CurrentlyPlayingSection_ChipLongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {
        Debug.WriteLine(sender.GetType());
        var send = (Chip)sender;
        var song = send.LongPressCommandParameter;
        Debug.WriteLine(song);
        Debug.WriteLine(song.GetType());

    }


    private void SongsColView_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {
        int itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
        bool isFullyVisible = e.FirstVisibleItemHandle <= itemHandle && itemHandle <= e.LastVisibleItemHandle;

    }
  

    private void ShowMoreBtn_Clicked(object sender, EventArgs e)
    {
        View s = (View)sender;
        SongModelView song = (SongModelView)s.BindingContext;
        MyViewModel.SetCurrentlyPickedSong(song);
        //SongsMenuPopup.Show();
    }
    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var qs = IPlatformApplication.Current.Services.GetService<QuickSettingsTileService>();
        qs!.UpdateTileVisualState(true, e.Item as SongModelView);
        MyViewModel.LoadAndPlaySongTapped(e.Item as SongModelView);
    }

    private async void MediaChipBtn_Tap(object sender, ChipEventArgs e)
    {

        ChoiceChipGroup? ee = (ChoiceChipGroup)sender;
        string? param = e.Chip.TapCommandParameter.ToString();
        if (param is null)
        {
            return;
        }
        var CurrentIndex = int.Parse(param);
        switch (CurrentIndex)
        {
            case 0:
                MyViewModel.ToggleRepeatMode();
                break;
            case 1:
                await MyViewModel.PlayPrevious();
                break;
            case 2:
            case 3:
                await MyViewModel.PlayPauseAsync();

                break;
            case 4:
                await MyViewModel.PlayNext(true);
                break;
            case 5:
                MyViewModel.IsShuffle = !MyViewModel.IsShuffle;
                break;

            case 6:
                MyViewModel.IncreaseVolume();
                break;

            default:
                break;
        }

    }

    private void SearchSong_Tap(object sender, HandledEventArgs e)
    {
        //await ToggleSearchPanel();
    }

    private void ViewNowPlayPage_Tap(object sender, HandledEventArgs e)
    {
        //MyViewModel.UpdateContextMenuData(MyViewModel.MySelectedSong);
        //ContextBtmSheet.State = BottomSheetState.HalfExpanded;

        ////MyViewModel.LoadAllArtistsAlbumsAndLoadAnAlbumSong();
        //ContextBtmSheet.State = BottomSheetState.HalfExpanded;
        //ContextBtmSheet.HalfExpandedRatio = 0.8;

    }


    */


    int prevViewIndex = 0;
    async Task AnimateColor(VisualElement element, Color color)
    {
        await element.MyBackgroundColorTo(color, length: 300);
        await Task.Delay(300); // Reduce freeze by using a lower delay
        await element.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
    }



    private void NowPlayingBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        //if (e.NewValue !=BottomSheetState.FullExpanded)
        //{
        //    await BtmBar.AnimateSlideUp(btmBarHeight);
        //}
    }

    private void SongsColView_LongPress_1(object sender, CollectionViewGestureEventArgs e)
    {

    }

    private void QuickFilterYears_DoubleTap(object sender, HandledEventArgs e)
    {

    }

    private async void SearchBy_Focused(object sender, FocusEventArgs e)
    {
        //await BtmBarZone.AnimateSlideDown(80);
    }

    private async void myPageSKAV_Closed(object sender, EventArgs e)
    {
        //await BtmBarZone.AnimateSlideUp(80);

        await OpenedKeyboardToolbar.DimmOutCompletelyAndHide();
    }

    private void Sort_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void OpenDevExpressFilter_LongPress(object sender, HandledEventArgs e)
    {
        SongsColView.Commands.ShowFilteringUIForm.Execute(null);
    }

    private void SongsColView_FilteringUIFormShowing(object sender, FilteringUIFormShowingEventArgs e)
    {

    }

    private async void BtmBar_RequestFocusOnMainView(object sender, EventArgs e)
    {
        SearchBy.Focus();
        await OpenedKeyboardToolbar.DimmInCompletelyAndShow();
    }

    private void BtmBar_RequestFocusNowPlayingUI(object sender, EventArgs e)
    {
        UISection.IsExpanded=!UISection.IsExpanded;
        MainViewExpander.IsExpanded = !UISection.IsExpanded;
    }

    private async void OpenDevExpressFilter_Tap(object sender, HandledEventArgs e)
    {
        //myPageSKAV.IsOpened = !myPageSKAV.IsOpened;
        SearchBy.Unfocus();
        await OpenedKeyboardToolbar.DimmOutCompletelyAndHide();

    }

    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;
    private async void RefreshLyrics_Clicked(object sender, EventArgs e)
    {
        if (_isLyricsProcessing)
        {
            // Optionally, offer to cancel the running process
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

        // Create a new CancellationTokenSource for this operation
        _lyricsCts = new CancellationTokenSource();

        // The IProgress<T> object automatically marshals calls to the UI thread.
        var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
        {
            // This code runs on the UI thread safely!
            MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
            MyProgressLabel.Text = $"Processing: {progress.CurrentFile}";
        });

        try
        {
            // Get the list of songs you want to process
            var songsToRefresh = MyViewModel.BaseVM.SearchResults; // Or your full master list
            var lryServ = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
            // --- Call our static, background-safe method ---
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
            // Clean up and hide UI elements
            _isLyricsProcessing = false;
            MyProgressBar.IsVisible = false;
            MyProgressLabel.IsVisible = false;
        }
    }

    private void ScrollToCurrSong_Tap(object sender, HandledEventArgs e)
    {
        int itemHandle = SongsColView.FindItemHandle(MyViewModel.BaseVM.CurrentPlayingSongView);
        SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);

    }

    private void ArtistsChip_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var txt = send.Text;
        SearchBy.Text = $"artist:{txt}";
    }

    private void QuickFilterYears_LongPress(object sender, HandledEventArgs e)
    {


    }

    private void AlbumFilter_LongPress(object sender, HandledEventArgs e)
    {

        var send = (Chip)sender;
        var txt = send.Text;
        SearchBy.Text = $"album:{txt}";
    }

    private void QuickFilterYears_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var param = send.TapCommandParameter as DXExpander;
        if (param is null)
        {
            return;
        }
        param.IsExpanded =!param.IsExpanded;
    }
    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (DXSlider)sender;


        //MyViewModel.BaseVM.SeekTrackPosition(ProgressSlider.Value);
    }



    private void NowPlayingBtmSheet_Unloaded(object sender, EventArgs e)
    {
        //SongPicture.StopHeartbeat();

    }


    private void DXButton_Clicked(object sender, EventArgs e)
    {
        //BottomExpander.IsExpanded = !BottomExpander.IsExpanded;


    }
    private void SongTitleChip_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var txt = send.LongPressCommandParameter as string;
        txt = $"album:{txt}";

        MyViewModel.BaseVM.SearchSongSB_TextChanged(txt);
    }

    private async void NowPlayingBtmSheet_Loaded(object sender, EventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        //UISection.Commands.ToggleExpandState.Execute(null);
    }

    private void BtmBar_RequestFocusNowPlayingUI_1(object sender, EventArgs e)
    {

    }
}







































































/*
private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
{
    //MyViewModel.ToggleRepeatModeCommand.Execute(true);
}

private void CurrQueueColView_Tap(object sender, CollectionViewGestureEventArgs e)
{
    MyViewModel.CurrentQueue = 1;
    //if (MyViewModel.IsOnSearchMode)
    //{
    //    MyViewModel.CurrentQueue = 1;
    //    List<SongModelView?> filterSongs = Enumerable.Range(0, SongsColView.VisibleItemCount)
    //             .Select(i => SongsColView.GetItemHandleByVisibleIndex(i))
    //             .Where(handle => handle != -1)
    //             .Select(handle => SongsColView.GetItem(handle) as SongModelView)
    //             .Where(item => item != null)
    //             .ToList()!;

    //}
    //MyViewModel.PlaySong(e.Item as SongModelView);
    // use your NEW playlist queue logic to pass this value btw, no need to fetch this sublist, as it's done.
    //you can even dump to the audio player queue and play from there. 
    //and let the app just listen to the queue changes and update the UI accordingly.
}


private void SaveCapturedLyrics_Clicked(object sender, EventArgs e)
{
    //MyViewModel.SaveLyricsToLrcAfterSyncingCommand.Execute(null);
}

private void StartSyncing_Clicked(object sender, EventArgs e)
{
    //await PlainLyricSection.DimmOut();
    //PlainLyricSection.IsEnabled = false;
    ////MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
    //IsSyncing = true;

    //await SyncLyrView.DimmIn();
    //SyncLyrView.IsVisible=true;
}

bool IsSyncing = false;
private void CancelAction_Clicked(object sender, EventArgs e)
{
    //await PlainLyricSection.DimmIn();
    //PlainLyricSection.IsEnabled = true;

    ////MyViewModel.PrepareLyricsSync(LyricsEditor.Text);
    //IsSyncing = false;

    //await SyncLyrView.DimmOut();
    //SyncLyrView.IsVisible=false;
}
private void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
{

    //await Task.WhenAll(ManualSyncLyricsView.AnimateFadeOutBack(), LyricsEditor.AnimateFadeOutBack(), OnlineLyricsResView.AnimateFadeInFront());

    //await MyViewModel.FetchLyrics(true);

}
private void ViewLyricsBtn_Clicked(object sender, EventArgs e)
{
    return;
    //LyricsEditor.Text = string.Empty;
    Button send = (Button)sender;
    string title = send.Text;
    //Content thisContent = (Content)send.BindingContext;
    if (title == "Synced Lyrics")
    {
        //await MyViewModel.SaveLyricToFile(thisContent!, false);
    }
    else
    if (title == "Plain Lyrics")
    {
        //LyricsEditor.Text = thisContent!.PlainLyrics;
        PasteLyricsFromClipBoardBtn_Clicked(send, e);
    }
}
private void PasteLyricsFromClipBoardBtn_Clicked(object sender, EventArgs e)
{
    //await Task.WhenAll(ManualSyncLyricsView.AnimateFadeInFront(), LyricsEditor.AnimateFadeInFront(), OnlineLyricsResView.AnimateFadeOutBack());

    //if (Clipboard.Default.HasText)
    //{
    //    LyricsEditor.Text = await Clipboard.Default.GetTextAsync();
    //}


}

private void ContextIcon_Tap(object sender, HandledEventArgs e)
{
    //MyViewModel.LoadArtistSongs();
    //ContextBtmSheet.State = BottomSheetState.HalfExpanded;
    //ContextBtmSheet.HalfExpandedRatio = 0.8;

}
private void SearchOnline_Clicked(object sender, EventArgs e)
{
    ImageButton send = (ImageButton)sender;
    //MyViewModel.CntxtMenuSearchCommand.Execute(send.CommandParameter);

}
Border LyrBorder { get; set; }


private void Stamp_Clicked(object sender, EventArgs e)
{
    ImageButton send = (ImageButton)sender;
    //MyViewModel.CaptureTimestampCommand.Execute((LyricPhraseModel)send.CommandParameter);

}

private void DeleteLine_Clicked(object sender, EventArgs e)
{
    ImageButton send = (ImageButton)sender;

    //MyViewModel.DeleteLyricLineCommand.Execute((LyricPhraseModel)send.CommandParameter);

}

private void Chip_Tap(object sender, HandledEventArgs e)
{
    Chip send = (Chip)sender;
    string? param = send.TapCommandParameter.ToString();
    //MyViewModel.ToggleRepeatModeCommand.Execute(true);
    //switch (param)
    //{
    //    case "repeat":


    //        break;
    //    case "shuffle":
    //        MyViewModel.CurrentQueue = 1;
    //        break;
    //    case "Lyrics":
    //        MyViewModel.CurrentQueue = 2;
    //        break;
    //    default:
    //        break;
    //}

}

private void SingleSongBtn_Clicked(object sender, EventArgs e)
{
    MyViewModel.CurrentQueue = 1;
    View s = (View)sender;
    SongModelView? song = s.BindingContext as SongModelView;
    //MyViewModel.CurrentPage = PageEnum.AllAlbumsPage;
    //MyViewModel.PlaySong(song);

}
private void ResetSongs_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
{
    //MyViewModel.LoadArtistAlbumsAndSongs(MyViewModel.SelectedArtistOnArtistPage);
}
private void DXCollectionView_Tap(object sender, CollectionViewGestureEventArgs e)
{
    View send = (View)sender;

    AlbumModelView? curSel = send.BindingContext as AlbumModelView;
    //MyViewModel.AllArtistsAlbumSongs=MyViewModel.GetAllSongsFromAlbumID(curSel!.Id);
}

private void ToggleShuffle_Tap(object sender, HandledEventArgs e)
{
    //MyViewModel.ToggleShuffleState();
}


private void AddAttachmentBtn_Clicked(object sender, EventArgs e)
{
    //if (ThoughtBtmSheetBottomSheet.State == BottomSheetState.Hidden)
    //{
    //    ThoughtBtmSheetBottomSheet.State = BottomSheetState.HalfExpanded;
    //}
    //else
    //{
    //    ThoughtBtmSheetBottomSheet.State = BottomSheetState.Hidden;
    //}

}

//private async void SaveNoteBtn_Clicked(object sender, EventArgs e)
//{
//    UserNoteModelView note = new()
//    {
//        UserMessageText=NoteText.Text,

//    };
//   await  MyViewModel.SaveUserNoteToDB(note,MyViewModel.SecondSelectedSong);
//}



//private void ChipGroup_ChipTap(object sender, ChipEventArgs e)
//{
//    switch (e.Chip.Text)
//    {
//        case "Home":
//            HomeTabView.SelectedItemIndex=1;
//            break;
//        case "Settings":
//            HomeTabView.SelectedItemIndex=2;
//            break;

//        default:
//            break;
//    }
//}

//private void BtmSheetHeader_Clicked(object sender, EventArgs e)
//{

//}

//private async void NowPlayingBtmSheet_StateChanged(object sender, Syncfusion.Maui.Toolkit.BottomSheet.StateChangedEventArgs e)
//{
//    Debug.WriteLine(e.NewState);
//    Debug.WriteLine(e.OldState);
//    if (e.NewState == Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Collapsed)
//    {
//        await BtmBar.AnimateSlideUp(450);
//        NowPlayingBtmSheet.State = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Hidden;
//        NowPlayingBtmSheet.IsVisible=false;

//    }

//}

//private async void CloseNowPlayingBtmSheet_Clicked(object sender, EventArgs e)
//{
//    await BtmBar.AnimateSlideUp(450);
//    NowPlayingBtmSheet.State = Syncfusion.Maui.Toolkit.BottomSheet.BottomSheetState.Hidden;

//}

//private void BtmBar_Loaded(object sender, EventArgs e)
//{
//    Debug.WriteLine(BtmBar.Height);
//    Debug.WriteLine(BtmBar.HeightRequest);
//}

//private void SlideView_CurrentItemChanged(object sender, ValueChangedEventArgs<object> e)
//{

//}

//private void HomeTabView_Loaded(object sender, EventArgs e)
//{
//    Debug.WriteLine(HomeTabView.GetType());
//}

private void ChoiceChipGroup_Loaded(object sender, EventArgs e)
{
    var send = (ChipGroup)sender;
    var src = send.ItemsSource;
    if (src is not null)
    {
        Debug.WriteLine(send.ItemsSource.GetType());
    }
    Debug.WriteLine(sender.GetType());
}

private void ChoiceChipGroup_SelectionChanged(object sender, EventArgs e)
{

}

private void NavChips_ChipClicked(object sender, EventArgs e)
{

}

private async void ChangeFolder_Clicked(object sender, EventArgs e)
{


    var selectedFolder = (string)((ImageButton)sender).CommandParameter;
    await MyViewModel.SelectSongFromFolderAndroid(selectedFolder);
}


private void DeleteBtn_Clicked(object sender, EventArgs e)
{
    var send = (ImageButton)sender;
    var param = send.CommandParameter.ToString();
    MyViewModel.DeleteFolderPath(param);
}
private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
{
    await MyViewModel.SelectSongFromFolderAndroid();
}

private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
{

}



private void ShowBtmSheet_Clicked(object sender, EventArgs e)
{
}

private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
{

}

private void SettingsNavChips_ChipClicked(object sender, EventArgs e)
{

}

private void SearchBy_Focused(object sender, FocusEventArgs e)
{
    //SearchBy.HorizontalOptions
}

private void SearchBy_Unfocused(object sender, FocusEventArgs e)
{

}

private void SongsColView_PullToRefresh(object sender, EventArgs e)
{
    //var mapper = IPlatformApplication.Current.Services.GetService<IMapper>();
    //SongsColView.ItemsSource = mapper.Map<ObservableCollection<SongModelView>>(BaseAppFlow.MasterList);
}
}

*/