using DevExpress.Maui.Controls;
using DevExpress.Maui.Core.Internal;
using DevExpress.Maui.Editors;

using Dimmer.Utilities;
using Dimmer.Utilities.CustomAnimations;
using Dimmer.ViewModel;

using System.ComponentModel;
using System.Text.RegularExpressions;

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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel.FiniInit();

        var baseVm = IPlatformApplication.Current.Services.GetService<BaseViewModel>();

    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(SearchBy.Text))
        {
            ByAll();
            return;
        }
        switch (SearchParam)
        {
            case "Title":
                ByTitle();
                break;
            case "Artist":
                ByArtist();
                break;
            case "":
                ByAll();
                break;
            default:
                ByAll();
                break;
        }

    }

    private void ClosePopup(object sender, EventArgs e)
    {

        SongsMenuPopup.Close();
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



        SongsMenuPopup.Show();

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.BaseVM.SelectedSongForContext;
        if (song is null)
        {
            return;
        }
        await MyViewModel.BaseVM.SelectedArtistAndNavtoPage(song);

        await SongsMenuPopup.CloseAsync();
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
        newOrder = (MyViewModel.BaseVM.CurrentSortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;


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
        await this.SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    SortOrder internalOrder = SortOrder.Ascending;
    private bool SortIndeed()
    {
        ObservableCollection<SongModelView> songs = MyViewModel.BaseVM.NowPlayingDisplayQueue;
        if (songs == null || !songs.Any())
            return false;
        internalOrder =  internalOrder== SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

        MyViewModel.BaseVM.CurrentSortOrder = internalOrder;

        switch (MyViewModel.BaseVM.CurrentSortProperty)
        {
            case "Title":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByTitle(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Artist": // Assuming CommandParameter is "Artist" for ArtistName
                SongsColView.ItemsSource =    CollectionSortHelper.SortByArtistName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Album": // Assuming CommandParameter is "Album" for AlbumName
                SongsColView.ItemsSource =  CollectionSortHelper.SortByAlbumName(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Genre":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByGenre(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Duration":
                SongsColView.ItemsSource =   CollectionSortHelper.SortByDuration(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "Year": // Assuming CommandParameter for ReleaseYear
                SongsColView.ItemsSource =   CollectionSortHelper.SortByReleaseYear(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            case "DateAdded": // Assuming CommandParameter for DateCreated
                SongsColView.ItemsSource = CollectionSortHelper.SortByDateAdded(songs, MyViewModel.BaseVM.CurrentSortOrder);
                songsToDisplay=SongsColView.ItemsSource as List<SongModelView> ?? new List<SongModelView>();
                break;
            default:
                System.Diagnostics.Debug.WriteLine($"Unsupported sort property: {MyViewModel.BaseVM.CurrentSortProperty}");
                // Reset sort state if property is unknown, or do nothing
                MyViewModel.BaseVM.CurrentSortProperty = string.Empty;
                MyViewModel.BaseVM.CurrentTotalSongsOnDisplay= songsToDisplay.Count;
                break;

        }
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

       
    }// At the top of your class, e.g., with your other member variables
    private readonly Dictionary<string, string> _searchPrefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    // Short and long prefixes for user convenience
    { "t", "Title" },
    { "title", "Title" },
    { "ar", "ArtistName" },
    { "artist", "ArtistName" },
    { "al", "AlbumName" },
    { "album", "AlbumName" }
    // Add more here if you have other fields, e.g., { "g", "Genre" }
};

    private void ApplyAdvancedFilter()
    {
        var searchText = SearchBy.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            SongsColView.FilterString = string.Empty;
            return;
        }

        var specificFilters = new List<string>();
        var generalSearchTerms = new List<string>();

        // 1. Regex to find our "prefix:value" patterns.
        // This pattern finds a prefix from our dictionary, a colon, and then either a quoted string
        // or a single word (non-whitespace characters).
        var prefixKeysPattern = string.Join("|", _searchPrefixes.Keys.Select(Regex.Escape));
        var regex = new Regex($@"\b({prefixKeysPattern}):(?:""([^""]*)""|(\S+))", RegexOptions.IgnoreCase);

        var remainingText = regex.Replace(searchText, match =>
        {
            var prefix = match.Groups[1].Value.ToLower();
            // Value can be in group 2 (quoted) or group 3 (unquoted)
            var value = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;

            if (_searchPrefixes.TryGetValue(prefix, out var columnName))
            {
                // IMPORTANT: Sanitize value to handle single quotes in the search term itself.
                var sanitizedValue = value.Replace("'", "''");
                specificFilters.Add($"Contains([{columnName}], '{sanitizedValue}')");
            }

            return string.Empty; // Remove the matched part from the original string
        }).Trim();

        // 2. The 'remainingText' is now our general "fuzzy" search query.
        // We split it into words, and every word must be found somewhere.
        if (!string.IsNullOrEmpty(remainingText))
        {
            var words = remainingText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var sanitizedWord = word.Replace("'", "''");
                // Each word must be in at least one of the main fields
                var fuzzyFilter = $"(Contains([Title], '{sanitizedWord}') OR " +
                                  $" Contains([ArtistName], '{sanitizedWord}') OR " +
                                  $" Contains([AlbumName], '{sanitizedWord}'))";
                generalSearchTerms.Add(fuzzyFilter);
            }
        }

        // 3. Combine all filters
        var allFilters = new List<string>();
        if (specificFilters.Any())
        {
            // All specific filters must be met
            allFilters.Add($"({string.Join(" AND ", specificFilters)})");
        }
        if (generalSearchTerms.Any())
        {
            // All general words must be found
            allFilters.Add($"({string.Join(" AND ", generalSearchTerms)})");
        }

        // Final filter string combines specific and general searches
        SongsColView.FilterString = string.Join(" AND ", allFilters);
    }

    private void ByArtist()
    {
        if (!string.IsNullOrEmpty(SearchBy.Text))
        {
            if (SearchBy.Text.Length >= 1)
            {
                SongsColView.FilterString = $"Contains([ArtistName], '{SearchBy.Text}')";

            }
            else
            {
                SongsColView.FilterString = string.Empty;
            }
        }
    }

    private void Sort_Clicked(object sender, EventArgs e)
    {
        SortBottomSheet.Show();
    }

    private void ArtistsChip_LongPress(object sender, HandledEventArgs e)
    {

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