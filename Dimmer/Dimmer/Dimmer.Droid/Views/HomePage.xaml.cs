using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Android.Graphics;

using CommunityToolkit.Maui.Core.Extensions;

using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;
using DevExpress.Maui.Core.Internal;
using DevExpress.Maui.Editors;

using Dimmer.Utilities;
using Dimmer.Utilities.CustomAnimations;

using Color = Microsoft.Maui.Graphics.Color;
using View = Microsoft.Maui.Controls.View;

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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        //var ss = this.GetPlatformView();
        //Debug.WriteLine(ss.Id);
        //var ee = ss.GetChildren();
        //foreach (var item in ee)
        //{
        //    Debug.WriteLine(item.Id);
        //    Debug.WriteLine(item.GetType());
        //}

        //var q = ss.GetChildrenInTree();
        //foreach (var item in q)
        //{
        //    Debug.WriteLine(item.Id);
        //    Debug.WriteLine(item.GetType());
        //}

        //var o = ss.GetPlatformParents();
        //foreach (var item in o)
        //{
        //    Debug.WriteLine(item.Id);
        //    Debug.WriteLine(item.GetType());
        //}


        //var nn = SongsColView.GetPlatformView();
        //Debug.WriteLine(nn.Id);
        //Debug.WriteLine(nn.GetType());

    }

    private async void SongsColView_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        await MyViewModel.PlaySongFromListAsync(song, SongsColView.ItemsSource as IEnumerable<SongModelView>);
    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {
        var art = MyViewModel.CurrentPlayingSongView.ArtistIds?.FirstOrDefault();
        DeviceStaticUtils.SelectedArtistOne = art;
        await SongsMenuPopup.CloseAsync();
    }




















    private void ClosePopup(object sender, EventArgs e)
    {
        //SongsMenuPopup.Close();
    }
















    /*
    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        //MyViewModel.SeekSongPosition(currPosPer: ProgressSlider.Value);
    }

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
    private double _startX;
    private double _startY;
    private bool _isPanning;

    double btmBarHeight = 145;



    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        View send = (View)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _startX = BtmBar.TranslationX;
                _startY = BtmBar.TranslationY;
                break;

            case GestureStatus.Running:
                if (!_isPanning)
                    return; // Safety check

                BtmBar.TranslationX = _startX + e.TotalX;
                BtmBar.TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
                _isPanning = false;

                double deltaX = BtmBar.TranslationX - _startX;
                double deltaY = BtmBar.TranslationY - _startY;
                double absDeltaX = Math.Abs(deltaX);
                double absDeltaY = Math.Abs(deltaY);

                if (absDeltaX > absDeltaY) // Horizontal swipe
                {
                    if (absDeltaX > absDeltaY) // Horizontal swipe
                    {
                        try
                        {
                            if (deltaX > 0) // Right
                            {
                                HapticFeedback.Perform(HapticFeedbackType.LongPress);
                                Debug.WriteLine("Swiped Right");

                                await MyViewModel.NextTrack();

                                Task<bool> bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                            else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                await MyViewModel.PreviousTrack();

                                Task<bool> bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                        }
                        catch (Exception ex) // Handle exceptions
                        {
                            Debug.WriteLine($"Error: {ex.Message}"); // Log the error
                        }
                        finally
                        {
                            BtmBar.TranslationX = 0; // Reset translation
                            BtmBar.TranslationY = 0; // Reset translation

                        }
                    }

                    else // Left
                    {
                        try
                        {
                            Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                            await MyViewModel.PreviousTrack();
                            Debug.WriteLine("Swiped left");
                            Task t1 = send.MyBackgroundColorTo(Colors.MediumPurple, length: 300);
                            Task t2 = Task.Delay(500);
                            Task t3 = send.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
                            await Task.WhenAll(t1, t2, t3);
                        }
                        catch { }
                    }
                }
                else  //Vertical swipe
                {
                    if (deltaY > 0) // Down
                    {

                        try
                        {
                            int itemHandle = SongsColView.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                            SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);

                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  // Up
                    {
                        try
                        {
                            btmBarHeight=BtmBar.Height;
                            SongsMenuPopup.Show();
                            await BtmBar.AnimateSlideDown(BtmBar.Height);

                        }
                        catch { }
                    }

                }

                //await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:
                _isPanning = false;
                await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut); // Return to original position
                break;

        }
    }
    int prevViewIndex = 0;
    async Task AnimateColor(VisualElement element, Color color)
    {
        await element.MyBackgroundColorTo(color, length: 300);
        await Task.Delay(300); // Reduce freeze by using a lower delay
        await element.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
    }


    private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {
        //DXBorder send = (DXBorder)sender;


        await MyViewModel.PlayPauseToggleAsync();

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

private void SongsColView_LongPress(object sender, CollectionViewGestureEventArgs e)
{
    var song = (SongModelView)e.Item;
    MyViewModel.SetCurrentlyPickedSong(song);
    //ContextBtmSheet.Show();
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


string SearchParam = string.Empty;

//private void SearchBy_TextChanged(object sender, EventArgs e)
//{
//    if (string.IsNullOrEmpty(SearchBy.Text))
//    {
//        ByAll();
//        return;
//    }
//    switch (SearchParam)
//    {
//        case "Title":
//            ByTitle();
//            break;
//        case "Artist":
//            ByArtist();
//            break;
//        case "":
//            ByAll();
//            break;
//        default:
//            ByAll();
//            break;
//    }

//}

//private void ByTitle()
//{
//    if (!string.IsNullOrEmpty(SearchBy.Text))
//    {
//        if (SearchBy.Text.Length >= 1)
//        {
//            MyViewModel.IsOnSearchMode = true;
//            SongsColView.FilterString = $"Contains([Title], '{SearchBy.Text}')";
//        }
//        else
//        {
//            MyViewModel.IsOnSearchMode = false;
//            SongsColView.FilterString = string.Empty;
//        }
//    }
//}
//private void ByAll()
//{
//    if (!string.IsNullOrEmpty(SearchBy.Text))
//    {
//        if (SearchBy.Text.Length >= 1)
//        {
//            MyViewModel.IsOnSearchMode = true;
//            SongsColView.FilterString =
//                $"Contains([Title], '{SearchBy.Text}') OR " +
//                $"Contains([ArtistName], '{SearchBy.Text}') OR " +
//                $"Contains([AlbumName], '{SearchBy.Text}')";
//        }
//        else
//        {
//            MyViewModel.IsOnSearchMode = false;
//            SongsColView.FilterString = string.Empty;
//        }
//    }
//}
//private void ByArtist()
//{
//    if (!string.IsNullOrEmpty(SearchBy.Text))
//    {
//        if (SearchBy.Text.Length >= 1)
//        {
//            MyViewModel.IsOnSearchMode = true;
//            SongsColView.FilterString = $"Contains([ArtistName], '{SearchBy.Text}')";

//        }
//        else
//        {
//            MyViewModel.IsOnSearchMode = false;
//            SongsColView.FilterString = string.Empty;
//        }
//    }
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

//private async void PlayPauseBtn_Clicked(object sender, EventArgs e)
//{

//    if (MyViewModel.IsPlaying)
//    {

//        await MyViewModel.PlayPauseAsync();

//    }
//    else
//    {
//        if (MyViewModel.CurrentPositionInSeconds.IsZeroOrNaN())
//        {
//            await MyViewModel.PlaySong(MyViewModel.TemporarilyPickedSong, CurrentPage.HomePage);
//        }
//        else
//        {
//            await MyViewModel.PlayPauseAsync();
//        }
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