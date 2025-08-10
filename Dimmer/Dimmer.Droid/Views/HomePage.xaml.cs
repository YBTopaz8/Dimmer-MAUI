using DevExpress.Maui.Controls;
using DevExpress.Maui.Core.Internal;
using DevExpress.Maui.Editors;

using Dimmer.DimmerLive;
using Dimmer.DimmerSearch;
using Dimmer.Utilities;
using Dimmer.Utilities.CustomAnimations;
using Dimmer.Utilities.ViewsUtils;
using Dimmer.ViewModel;

using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Controls.Embedding;

using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Color = Microsoft.Maui.Graphics.Color;
using Platform = Microsoft.Maui.ApplicationModel.Platform;

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

        //MyViewModel.FiniInit();

        ////var baseVm = IPlatformApplication.Current.Services.GetService<BaseViewModel>();
        //AndMorphingButton.MorphingButton morph = new AndMorphingButton.MorphingButton(Platform.AppContext);
        //morph.SetText("TestYB", TextView.BufferType.Normal);
        //morph.Click += (s, e) =>
        //{
        //    Debug.WriteLine("Button Clicked");
        //    //morph.SetText("Clicked", TextView.BufferType.Normal);
        //    //morph.SetBackgroundColor(Android.Graphics.Color.Red);
        //    //morph.SetTextColor(Android.Graphics.Color.White);
        //};
        
        //var ss = morph.ToView();
        //ss.HeightRequest = 180;
        //ss.BackgroundColor = Colors.Red;
        
        //MyBtmBar.BtmBarStackLayout.Children.Add(ss);
        
    }

  
    private void ClosePopup(object sender, EventArgs e)
    {
        

        //SongsMenuPopup.Close();
    }



    string SearchParam = string.Empty;

    SongModelView selectedSongPopUp = new SongModelView();
    private void MoreIcosn_Clicked(object sender, EventArgs e)
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
        //AndroidTransitionHelper.BeginMaterialContainerTransform(this.RootLayout, HomeView, DetailView);
        //HomeView.IsVisible=false;
        //DetailView.IsVisible=true;

    }
   
    List<SongModelView> songsToDisplay = new();
    private void SortChoose_Clicked(object sender, EventArgs e)
    {

        var chip = sender as DXButton; // Or whatever your SfChip type is
        if (chip == null || chip.CommandParameter == null)
            return;

        string sortProperty = chip.CommandParameter.ToString();
        bool flowControl = SortSongs(sortProperty);
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

    private bool SortSongs(string sortProperty)
    {
        if (string.IsNullOrEmpty(sortProperty))
            return false;


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
            return false;
        }

        return true;
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

    }
    private async void DXButton_Clicked_3(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }
    SortOrder internalOrder = SortOrder.Asc;
    private bool SortIndeed()
    {

        return true;

    }

    private void SortCategory_LongPress(object sender, HandledEventArgs e)
    {
        SortIndeed();
    }


    private void Sort_Clicked(object sender, EventArgs e)
    {
        //SortBottomSheet.Show();
    }


    private void SongsColView_LongPress(object sender, CollectionViewGestureEventArgs e)
    {
        //SongsColView.Commands.ShowFilteringUIForm.Execute(null);
    }

    private void DXButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        //MyViewModel.LoadTheCurrentColView(SongsColView);
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

   

    private async void myPageSKAV_Closed(object sender, EventArgs e)
    {
        

        //await OpenedKeyboardToolbar.DimmOutCompletelyAndHide();
    }

    private void Sort_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void OpenDevExpressFilter_LongPress(object sender, HandledEventArgs e)
    {
        //SongsColView.Commands.ShowFilteringUIForm.Execute(null);
    }

   
   
    private void QuickFilterYears_Tap(object sender, HandledEventArgs e)
    {
    }
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds
    private async void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (DXSlider)sender;


        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.BaseVM.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }


    private void NowPlayingBtmSheet_Unloaded(object sender, EventArgs e)
    {
        //SongPicture.StopHeartbeat();

    }


    private void DXButton_Clicked(object sender, EventArgs e)
    {
        //BottomExpander.IsExpanded = !BottomExpander.IsExpanded;
        //MediaControlView.IsExpanded = false;
        //UtilsTabView.IsVisible=true;
    }
    private void SongTitleChip_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var txt = send.LongPressCommandParameter as string;
        txt = $"album:{txt}";

        MyViewModel.BaseVM.SearchSongSB_TextChanged(txt);
    }

    private void NowPlayingBtmSheet_Loaded(object sender, EventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        //NowPlayingUISection.Commands.ToggleExpandState.Execute(null);
    }


    private void DXCollectionView_SelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        DXCollectionView send = sender as DXCollectionView;
        var sel = send.SelectedItem;

        var ind = send.FindItemHandle(sel);
        send.ScrollTo(ind, DXScrollToPosition.Start);

        //int itemHandle = AllLyricsColView.FindItemHandle(MyViewModel.BaseVM.cur);
        //bool isFullyVisible = e.FirstVisibleItemHandle <= itemHandle && itemHandle <= e.LastVisibleItemHandle;

    }

    private void ClearSearch_LongPress(object sender, HandledEventArgs e)
    {
        //SearchBy.Text=string.Empty;
    }

    private void AddSearchFilter_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var paramss = send.TapCommandParameter as string;
        //SearchBy.Text=SearchBy.Text + " " +paramss;
    }

    private void BtmBar_ScrollToStart(object sender, EventArgs e)
    {
        MyViewModel.ScrollColViewToStart(new SongModelView());
       
    }

    private async void BtmBar_ToggleAdvanceFilters(object sender, EventArgs e)
    {
        //if (BelowBtmBar.IsVisible)
        //{
        //    await BelowBtmBar.DimmOutCompletelyAndHide();
        //}
        //else
        //{
        //    await BelowBtmBar.DimmInCompletelyAndShow();
        //}
    }

    private void BtmBar_ScrollToStart_1(object sender, EventArgs e)
    {

    }

    private async void am_DoubleTap(object sender, HandledEventArgs e)
    {
        //await BelowBtmBar.DimmOutCompletelyAndHide();
    }

    private void PingGest_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {

    }

    private void SongTitleChip_DoubleTap(object sender, HandledEventArgs e)
    {

    }

   

    private async void MoreIcon_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        SongModelView song = send.BindingContext as SongModelView;
        MyViewModel.BaseVM.SelectedSong=song;
        await MyViewModel.BaseVM.SaveUserNoteToSong(song);
        //await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    protected override bool OnBackButtonPressed()
    {


           this.NowPlayingUISection.IsExpanded=!NowPlayingUISection.IsExpanded;
        this.MainViewExpander.IsExpanded = !NowPlayingUISection.IsExpanded;

            BtmBarExp.IsExpanded = !NowPlayingUISection.IsExpanded;
        TopBeforeColView.IsExpanded = !TopBeforeColView.IsExpanded;

        
            return true;
        return base.OnBackButtonPressed();
    }

    private async void MoreaIcon_Clicked(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        SongModelView song = send.BindingContext as SongModelView;
        MyViewModel.BaseVM.SelectedSong=song;
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    private void DXButton_Clicked_4(object sender, EventArgs e)
    {

    }

    private void ViewSongOnly_Clicked(object sender, EventArgs e)
    {

    }

    private void ViewSongOnly_Tap(object sender, DXTapEventArgs e)
    {
        
    }

    private void MoreIcon_Tap(object sender, HandledEventArgs e)
    {

        //MoreModal.IsOpen=true;

    }

    private void ViewSongOnly_Clicked_1(object sender, EventArgs e)
    {

    }

    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {

    }

    private void ViewSongOnly_TouchDown(object sender, EventArgs e)
    {
        //if (this.QuickPanelBtmSheet.State != BottomSheetState.Hidden)
        //{
        //    this.QuickPanelBtmSheet.State= BottomSheetState.Hidden;
        //}
        //else
        //{
        //    this.QuickPanelBtmSheet.State= BottomSheetState.HalfExpanded;
        //}
    }

    private async void PlaySongClicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        await MyViewModel.BaseVM.PlaySong(song);
    }

    private void Chip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ViewNPQ_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.BaseVM.SearchSongSB_TextChanged(MyViewModel.BaseVM.CurrentPlaybackQuery);
        return;

    }

    private async void GoToAlbumPage(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        var val = song.AlbumName;
        if (string.IsNullOrWhiteSpace(val))
            return; // No album to show
        await Shell.Current.GoToAsync(nameof(AlbumPage), true);
        MyViewModel.BaseVM.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("album", val));
    }


    private async void GoToArtistPage(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        var val = song.OtherArtistsName;
        if (string.IsNullOrWhiteSpace(val))
            return; // No artists to show

        char[] dividers = new char[] { ',', ';', ':', '|', '-' };

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers
            .Select(name => name.Trim())                            // Trim whitespace from each name
            .Where(name => !string.IsNullOrWhiteSpace(name))        // Keep names that are NOT null or whitespace
            .ToArray();                                             // Convert to an array

        // If after all filtering there are no names, there is no need to show the action sheet.
        if (namesList.Length == 0)
        {
            return;
        }

        var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

        if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
        {
            return;
        }

        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        MyViewModel.BaseVM.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", selectedArtist));

    }

    private void BtmBar_RequestFocusOnMainView(object sender, EventArgs e)
    {

    }

    private void BtmBar_RequestFocusNowPlayingUI(object sender, EventArgs e)
    {

        this.NowPlayingUISection.IsExpanded=!NowPlayingUISection.IsExpanded;
        this.MainViewExpander.IsExpanded = !NowPlayingUISection.IsExpanded;

        BtmBarExp.IsExpanded = !NowPlayingUISection.IsExpanded;
        TopBeforeColView.IsExpanded = !TopBeforeColView.IsExpanded;

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
