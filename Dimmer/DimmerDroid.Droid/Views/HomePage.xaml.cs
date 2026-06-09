
global using Dimmer.Views.CustomViews;
global using View = Microsoft.Maui.Controls.View;
using Android.Views.InputMethods;
using DevExpress.Data.Extensions;
using DevExpress.Maui.Editors;
using DevExpress.Maui.Scheduler.Internal;
using Dimmer.Utilities;


namespace Dimmer.Views;

public partial class HomePage : ContentPage
{
    public HomePage(BaseViewModelAnd viewModelAnd, StatisticsViewModel statisticsView, LastFMViewModel lastFMVM, LoginViewModel loginVM)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
        MyLastFMViewModel = lastFMVM;
        MyLoginVM = loginVM;
        StatsViewModel = statisticsView;
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.OpenMediaUIOnNotificationTap), v => (MyViewModel.OpenMediaUIOnNotificationTap))
            .Subscribe(
                e =>
                {
                    if(e)
                    {
                        this.MainPageTabView.SelectedItemIndex = 1;
                    }

                });
        MyLastFMViewModel.LoadBaseViewModel(viewModelAnd);
        _ = Task.Run(() => loginVM.InitializeAsync());
     

    }
    
    BaseViewModelAnd MyViewModel { get; }
    public LastFMViewModel MyLastFMViewModel { get; }
    public LoginViewModel MyLoginVM { get; }
    public StatisticsViewModel StatsViewModel { get; }


    protected override void OnAppearing()
    {
        base.OnAppearing();

      


        if (!MyViewModel.IsInitialized)
        {
            InitializeAppLogic();
            //MyViewModel.LoadSongsInitially();
        }

        MyViewModel.WhenPropertyChange(nameof(MyViewModel.HomePageIndex), v => (MyViewModel.HomePageIndex))
            .Subscribe(
                async e =>
                {

                    await Task.Delay(2000);
                    switch (e)
                    {
                        case 0:
                            MyViewModel.StartTQLPipeLine();
                           
                        
                            var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                            SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
                            break;

                        case 2:
                            var songHandlePBCV = PlaybackQueueCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                            PlaybackQueueCV.ScrollTo(songHandlePBCV, DevExpress.Maui.Core.DXScrollToPosition.Start);
                            break;
                        default:
                            break;
                    }

                });
    }


    private void InitializeAppLogic()
    {
        
            try
            {
                var startTime = Java.Lang.JavaSystem.CurrentTimeMillis();

                MyViewModel.InitializeAllVMCoreComponents();

                var duration = Java.Lang.JavaSystem.CurrentTimeMillis() - startTime;
                Console.WriteLine($"InitializeAppLogic took {duration}ms");
                if (duration > 2000)
                    Android.Util.Log.Warn("ANR_WARNING", $"OnCreate took {duration}ms - ANR risk!");
            }
            catch (Exception ex)
            {
                 Shell.Current.DisplayAlertAsync("Fatal Error Init Logic", ex.Message, "ok");
                Console.WriteLine($"VM INIT CRASH: {ex}");
                Android.Util.Log.Error("DIMMER_INIT", ex.ToString());
            }
        
    }

    private async void TapToPlaySongGestRecog_Tapped(object sender, TappedEventArgs e)
    {

        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        //var songsInCV = SongsCV.ItemsSource;


        List<SongModelView> songsInCV = new();
        for (int i = 0; i < SongsCV.VisibleItemCount; i++)
        {
            var itemHandle = SongsCV.GetItemHandleByVisibleIndex(i);

            if (SongsCV.GetItem(itemHandle) is not SongModelView songByItemHandle) continue;
            songsInCV.Add(songByItemHandle);
        }



        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, songsInCV);
    }

    private void CurrentPlayingArtistChip_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void NPMiddleGridSection_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void BtmBarCoverImageView_Loaded(object sender, EventArgs e)
    {
        DXImage img = (DXImage)sender;
        var platView = img.Handler?.PlatformView as Android.Views.View;

        if (platView is null)
            return;
        platView.Click += (s, e) =>
        {
            var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
        };
    }
    private void CurrentPlayingTitleChip_Tap(object sender, DXTapEventArgs e)
    {
        MainPageTabView.SelectedItemIndex = 1;
    }

    private void SearchBtn_Clicked(object sender, EventArgs e)
    {
        SearchText.Focus();

    }

    private void SongInCVTapGR_Tapped(object sender, TappedEventArgs e)
    {

    }


    private void SearchText_TextChanged(object sender, EventArgs e)
    {
        string? txt = SearchText.Text;
        if (txt is not null)
        {
            MyViewModel.SearchToTQL(txt);
        }
    }

    private void MoreBtn_Tap(object sender, DXTapEventArgs e)
    {

        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        MyViewModel.SelectedSong = song;
        SingleSongBtmSheet.Show();
    }

    private async void ImageOnCollectionViewTapped(object sender, TappedEventArgs e)
    {


    }

    private void EditSongChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ViewSongChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void MoreDXButton_Clicked(object sender, EventArgs e)
    {
        View currentBtnView= (View)sender;
        
        
    
    }

    //private void PlayButton_Clicked(object sender, EventArgs e)
    //{

    //}

    //private async void MiddleGridSection_TappedToPlaySong(object sender, TappedEventArgs e)
    //{

    //    var send = (View)sender;
    //    var song = (SongModelView)send.BindingContext;
    //    //var songsInCV = SongsCV.ItemsSource;
    //    var platView = SongsCV.Handler?.PlatformView;

    //    Debug.WriteLine(SongsCV.VisibleItemCount);
    //    Debug.WriteLine(SongsCV.ScrollItemCount);

    //    List<SongModelView> songsInCV = new();
    //    for (int i = 0; i < SongsCV.VisibleItemCount; i++)
    //    {
    //        var itemHandle = SongsCV.GetItemHandleByVisibleIndex(i);

    //        if (SongsCV.GetItem(itemHandle) is not SongModelView songByItemHandle) continue;
    //        songsInCV.Add(songByItemHandle);
    //    }
    //    //Debug.WriteLine(songsInCV?.GetType());


    //    await MyViewModel.PlaySongAsync(song,CurrentPage.HomePage,songsInCV);
    //}

    //private async void TitleChip_Tap(object sender, HandledEventArgs e)
    //{
    //    MiddleGridSection_TappedToPlaySong(sender, new TappedEventArgs( e));

    //}

    //private void ArtistChip_Tap(object sender, HandledEventArgs e)
    //{
    //    var send = (View)sender;
    //    var song = (SongModelView)send.BindingContext;
    //    //var artInDb = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(song.Id)?.Artist.ToArtistModelView();
    //    //if(artInDb == null)
    //    //    return;
    //    //MyViewModel.SetSelectedArtist(artInDb);
    //    MyViewModel.SelectedSong = song;
    //    ArtistsChoiceBtmSheet.Show();
    //}

    //private async void ArtistChip_DoubleTap(object sender, HandledEventArgs e)
    //{

    //    var send = (View)sender;
    //    var song = (SongModelView)send.BindingContext;

    //    MyViewModel.SetSelectedArtist(song.Artist);

    //    await Shell.Current.GoToAsync(nameof(ArtistPage), true);

    //}

    //private void ArtistChip_LongPress(object sender, HandledEventArgs e)
    //{

    //}

    //private void PlaybackQueueBtmSheet_Loaded(object sender, EventArgs e)
    //{

    //}


    //private void PBQueueBtmSheet_StateChanged(object sender, DevExpress.Maui.Core.ValueChangedEventArgs<BottomSheetState> e)
    //{

    //}

    //private void SearchBar_TextChanged(object sender, EventArgs e)
    //{
    //    string? txt = SearchBarTextEdit.Text;
    //    if(txt is not null)
    //    {
    //        MyViewModel.SearchToTQL(txt);
    //    }
    //}

    //private void MenuBtn_Tap(object sender, HandledEventArgs e)
    //{
    //    Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;

    //}

    //private void SearchBar_Unloaded(object sender, EventArgs e)
    //{
    //    MyViewModel.ClearSubscriptionToSearchBar();
    //}

    //private void SearchBar_Loaded(object sender, EventArgs e)
    //{



    //    MyViewModel.SubscribeToPlayCount(SearchBarTextEdit);



    //}


    //private async void NPBottomBar_PanUpdated(object sender, PanUpdatedEventArgs e)
    //{
    //    bool IsSwipedUp = e.TotalY < -100; // Adjust the threshold as needed
    //    bool IsSwipedDown = e.TotalY > 100; // Adjust the threshold as needed
    //    bool IsSwipedLeft = e.TotalX < -100; // Adjust the threshold as needed
    //    bool IsSwipedRight = e.TotalX > 100; // Adjust the threshold as needed

    //    if (IsSwipedUp)
    //    {
    //        NPBtmSheet.Show();
    //        // Handle swipe up action
    //        Debug.WriteLine("Swiped Up");
    //    }
    //    else if (IsSwipedDown)
    //    {
    //        //SearchBarTextEdit.Focus();

    //    InputMethodManager? imm = (InputMethodManager?)MainApplication.Context.GetSystemService(Activity.InputMethodService);
    //        var view = SearchBarTextEdit.Handler?.PlatformView as Android.Views.View;
    //        imm?.ShowSoftInput(view, ShowFlags.Implicit);


    //// Handle swipe down action
    //Debug.WriteLine("Swiped Down");
    //    }
    //    else if (IsSwipedLeft)
    //    {
    //        await MyViewModel.PreviousTrackAsync();
    //        // Handle swipe left action
    //        Debug.WriteLine("Swiped Left");
    //    }
    //    else if (IsSwipedRight)
    //    {
    //        await MyViewModel.NextTrackAsync();
    //        // Handle swipe right action
    //        Debug.WriteLine("Swiped Right");
    //    }

    //}



    //private void CurrentPlayingArtistChip_LongPress(object sender, HandledEventArgs e)
    //{

    //    var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

    //    SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    //}

    //private async void SettingsBtn_Clicked(object sender, EventArgs e)
    //{
    //    await Shell.Current.GoToAsync(nameof(SettingsPage), true);
    //}

    //private void ArtistChip_Tap_1(object sender, HandledEventArgs e)
    //{

    //}

    //private async void ArtistBtmChip_Tap(object sender, HandledEventArgs e)
    //{
    //    var artChip = (DevExpress.Maui.Editors.Chip)sender;
    //    var art = artChip.LongPressCommandParameter as ArtistModelView;

    //    MyViewModel.SetSelectedArtist(art);

    //    await Shell.Current.GoToAsync(nameof(ArtistPage), true);    
    //}

    //private void ArtistBtmChip_LongPress(object sender, HandledEventArgs e)
    //{
    //    var artChip = (DevExpress.Maui.Editors.Chip)sender;
    //    var art = artChip.LongPressCommandParameter as ArtistModelView;

    //    if(art != null && !string.IsNullOrEmpty(art.Name))
    //    {
    //        MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.ByArtist(art.Name));
    //    }
    //}

    //private void ArtistGrid_Tapped(object sender, TappedEventArgs e)
    //{

    //}

    //private void SearchArtistBtnTQL_Tap(object sender, HandledEventArgs e)
    //{

    //    var artChip = (DevExpress.Maui.Editors.Chip)sender;
    //    var art = artChip.LongPressCommandParameter as string;
    //    if (art is null) return;
    //    SearchBarTextEdit.Text = SearchBarTextEdit.Text + TQlStaticMethods.PresetQueries.ByArtist(art);
    //}

    //private async void ViewArtistBtn_Tap(object sender, HandledEventArgs e)
    //{
    //    var artChip = (DevExpress.Maui.Editors.Chip)sender;
    //    var art = artChip.LongPressCommandParameter as ArtistModelView;

    //    MyViewModel.SetSelectedArtist(art);

    //    await ArtistsChoiceBtmSheet.CloseAsync();
    //    await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    //}

    //private void DXToggleButton_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    //{

    //}

    


    private async void DeleteSongBtn_Tap(object sender, HandledEventArgs e)

    {
        var result = await Shell.Current.DisplayAlertAsync("Confirm Delete",
            "Delete Song", "Yes", "No");
        if (result)
        {
            await MyViewModel.DeleteSongs(new List<SongModelView>() { MyViewModel.SelectedSong! });
        }
    }



  

    //private void CurrentPlayingTitleChip_LongPress(object sender, HandledEventArgs e)
    //{
    //}

    //private void SongTitle_Loaded(object sender, EventArgs e)
    //{
    //    SongTitle.Text = MyViewModel.SelectedSong!.IsFavorite
    //        ? $"❤️{MyViewModel.SelectedSong?.Title}"
    //        : $"{MyViewModel.SelectedSong?.Title}";
    //    ;
    //}

    private void PlayNextBtn_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.SelectedSong is null)
            return;
        var song = MyViewModel.SelectedSong;
        MyViewModel.AddToNext(new List<SongModelView>() { song });


        var snackMsg = $"Added {song.Title} by {MyViewModel.SelectedSong.ArtistName} to Next in Queue";

        CommunityToolkit.Maui.Alerts.Toast msgToast = new CommunityToolkit.Maui.Alerts.Toast() { Text = snackMsg, Duration = CommunityToolkit.Maui.Core.ToastDuration.Short };

        SingleSongBtmSheet.Close();
        msgToast.Show();
    }

    private void SelectedSongBtmSheetAlbumNameChip_Tap(object sender, HandledEventArgs e)
    {

    }

    //private void AlbumChip_Tap(object sender, HandledEventArgs e)
    //{

    //}

    //private void ArtistsSongs_Loaded(object sender, EventArgs e)
    //{
    //    DXCollectionView cv = (DXCollectionView)sender;
    //    cv.ItemsSource = MyViewModel.SelectedArtist?.SongsByArtist;


    //}

    //private void DXButton_Loaded(object sender, EventArgs e)
    //{
    //    var send = (DXButton)sender;
    //    MyViewModel.WhenPropertyChange(MyViewModel.CurrentTqlQuery, v => (MyViewModel.CurrentTqlQuery))
    //        .Subscribe(
    //            e =>
    //            {
    //               if(string.IsNullOrEmpty(e) || string.IsNullOrWhiteSpace(e))
    //                {
    //                   send.IsVisible = false;
    //                   return;
    //                }
    //                send.IsVisible = true;

    //            });

    //}

    //private void NPMiddleGridSection_Tapped(object sender, TappedEventArgs e)
    //{
    //    CurrentPlayingTitleChip_Tap(sender, new DXTapEventArgs(new Point()));
    //}

    //private void SwipeGestureRecog_PanUpdated(object sender, PanUpdatedEventArgs e)
    //{
    //    var isSwipeUp = e.StatusType == GestureStatus.Running && e.TotalY < -100; // Adjust the threshold as needed
    //}

    //private void ViewPlaybackQueueBtn_Clicked(object sender, HandledEventArgs e)
    //{
    //    NPBtmSheet.ShowAndOpenPlaybackQueue();
    //}

    //private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    //{

    //}

    //private void TouchBehavior_LongPressCompleted(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
    //{

    //    var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

    //    SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    //}

    //private void SongsCV_PullToRefresh(object sender, EventArgs e)
    //{

    //}

    //private void CurrentPlayingTitleChip_Tap(object sender, DXTapEventArgs e)
    //{
    //    NPBtmSheet.NowPlayingExp.IsExpanded = true;
    //    NPBtmSheet.PlayBackQueueExp.IsExpanded = false;

    //    NPBtmSheet.Show();
    //    NPBtmSheet.State = BottomSheetState.FullExpanded;
    //}




    //private void BtmBarCoverImageView_Loaded(object sender, EventArgs e)
    //{
    //    DXImage img = (DXImage)sender;
    //    var platView = img.Handler?.PlatformView as Android.Views.View;

    //    if(platView is null)
    //        return;
    //    platView.Click += (s, e) =>
    //    {
    //        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
    //        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    //        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    //    };

    //    //platView.LongClickable = true;
    //    //platView.LongClick += async (s, e) =>
    //    //{
    //    //    var send = (View)sender;
    //    //    var song = MyViewModel.CurrentPlayingSongView;
    //    //    MyViewModel.SelectedSong = song;

    //    //};
    //}

    private async void SelectedSongBtmSheetArtistNameChip_Tap(object sender, HandledEventArgs e)
    {

        var send = (View)sender;
        var song = MyViewModel.SelectedSong;
        if (song is null)
            return;

        MyViewModel.SetSelectedArtist(song.Artist);
        await SingleSongBtmSheet.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    }

    private async void ViewSongBtn_Clicked(object sender, EventArgs e)
    {
        


        if (Shell.Current.CurrentPage.GetType() != typeof(DetailsOverview))
            await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
    }



    private async void ArtistNameBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var artist = (ArtistModelView)send.CommandParameter as ArtistModelView;

        MyViewModel.SetSelectedArtist(artist);

        await Shell.Current.GoToAsync(nameof(ArtistPage), true);

    }

    private async void AlbumBtn_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var song = MyViewModel.SelectedSong;
        if (song is null)
            return;
        MyViewModel.SetSelectedAlbum(song.Album);


        await Shell.Current.GoToAsync(nameof(AlbumPage), true);
    }

    private async void SingleSongPopup_Loaded(object sender, EventArgs e)
    {
        await StatsViewModel.LoadSongQuickStatsAsync(MyViewModel.SelectedSong);
    }

  
    private void ActionsRadarChart_SelectionChanged(object sender, DevExpress.Maui.Charts.SelectionChangedEventArgs e)
    {

    }

    




    private void DrawerHamburger_Clicked(object sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;

    }


    private void DXButton_Clicked(object sender, EventArgs e)
    {

    }

    private void PlaybackQueueGrid_Loaded(object sender, EventArgs e)
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.PlaybackQueue), v => MyViewModel.PlaybackQueue)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pbQueue =>
            {
                if (pbQueue.Count < 1)
                {
                    PlaybackQueueGrid.IsVisible = false;
                }
                else
                {
                    PlaybackQueueGrid.IsVisible = true;
                }
            });

    }

    private void NowPlayingHighlightBtn_TapPressed(object sender, DXTapEventArgs e)
    {
        MainPageTabView.SelectedItemIndex = 1;
    }

    private void BtmBarGrid_Loaded(object sender, EventArgs e)
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(song =>
            {
                if (string.IsNullOrEmpty(song.TitleDurationKey))
                {
                    BtmBarGrid.IsVisible = false;
                }
                else
                {
                    BtmBarGrid.IsVisible = true;
                }
            });

    }

    private void NowPlayingView_SwitchToPlayBackQueue(object sender, EventArgs e)
    {
        MainPageTabView.SelectedItemIndex = 2;
    }

    private void PlaybackQueueCV_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {

    }

    private async void PlaySongInQueue_Tap(object sender, DXTapEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await MyViewModel.PlaySongWithActionAsync(song, PlaybackAction.JumpInQueue);

    }

    private async void AddSongToFav_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        DXButton send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
            return;
        await MyViewModel.AddFavoriteRatingToSongAsync(song);
    }


    private async void RemoveSongFromQueueBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await MyViewModel.RemoveFromQueue(song);
    }

    private void ScrollToInPlayBackQueue_Tap(object sender, DXTapEventArgs e)
    {
        var curIndex = MyViewModel.PlaybackQueue.IndexOf(MyViewModel.CurrentPlayingSongView); 
        var curHandle = PlaybackQueueCV.GetItemHandle(curIndex);
        PlaybackQueueCV.ScrollTo(curHandle, DXScrollToPosition.Start);
    }

    List<string> SortItems = new List<string>();
    private void FilterChipGroup_Loaded(object sender, EventArgs e)
    {
    }


    //private async void SelectedSongBtmSheetAlbumNameChip_Tap(object sender, HandledEventArgs e)
    //{

    //    var send = (View)sender;
    //    var song = MyViewModel.SelectedSong;
    //    if(song is null)
    //        return;
    //    MyViewModel.SetSelectedAlbum(song.Album);

    //    await SingleSongBtmSheet.CloseAsync();
    //    await Shell.Current.GoToAsync(nameof(AlbumPage), true);
    //}

    private void SongsCV_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SongsCV.VisibleItemCount))
        {

            var newCount = (SongsCV.ItemsSource as ReadOnlyObservableCollection<SongModelView>)?.Count;
            string? fullStr = newCount.ToString();
            SearchText.Suffix = fullStr;
        }
    }

    private void SortByChipGroup_SelectionChanged(object sender, EventArgs e)
    {
        FilterChipGroup send = (FilterChipGroup)sender;
        var indices = send.SelectedIndexes;
    }
    
                    
    private void SortByListPicker_Loaded(object sender, EventArgs e)
    {
    }

    private void SortByListPicker_FilterChanged(object sender, FilterChangedEventArgs e)
    {

    }

    private void SortByListPicker_PickerShowing(object sender, PickerShowingEventArgs e)
    {

    }

    private void AddToQueueButton_Clicked(object sender, EventArgs e)
    {
        
    }

    private async void ViewArtist_Clicked(object sender, EventArgs e)
    {

        var send = (DXButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        if (artist is null) return;
      

        MyViewModel.SetSelectedArtist(artist);
        await ArtistsMgtBtmSheet.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    }

    private void PreviewArtistSongsBtn_CheckedChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        var send = (DXToggleButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        if (artist is null) return;
        if (e.NewValue)
        {
            MyViewModel.SwapMainSongsToArtistSongs(artist);
        }
        else
        {
            MyViewModel.SwapBackToMainSongs();
        }
    }

    private void OtherArtistsName_CheckedChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        var dxBtn = (DXButton)sender;
        var song = dxBtn.CommandParameter as SongModelView;
        if (song is null) return;
        MyViewModel.SelectedSong = song;
        var songHandle = SongsCV.FindItemHandle(song);

        SongsCV.ScrollTo(songHandle, DXScrollToPosition.Start);
        ArtistsMgtBtmSheet.Show(BottomSheetState.HalfExpanded);
    }

    private void AddNextToCurrentPlayingSong_Clicked(object sender, EventArgs e)
    {

    }

 
    private void AddToEndOfQueue_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddListOfSongsToQueueEnd(MyViewModel.SelectedArtist.SongsByArtist);
    }

    private void AddPlaybackQueue_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddToNext(MyViewModel.SelectedArtist.SongsByArtist);
    }

    private void AddRemoveMyFavs_CheckedChanged(object sender, EventArgs e)
    {
        CheckEdit chBx = (CheckEdit)sender;
        var isChecked = chBx.IsChecked;

    }

    private void SongsCV_Loaded(object sender, EventArgs e)
    {
        MyViewModel.SetCollectionView(SongsCV);
    }

    private void IsFavorite_CheckedChanged(object sender, EventArgs e)
    {
       
    }

    private void FilterCheckItem_Loaded(object sender, EventArgs e)
    {
        var send = (FilterCheckItem)sender;
        send.Context = SongsCV.FilteringContext;
        send.FieldName = "IsFavorite";
    }

    private void FilterCheckedListPickerItem_Loaded(object sender, EventArgs e)
    {

    }

    private void ArtistFilterCheckedListPickerItem_Loaded(object sender, EventArgs e)
    {
        var artistFiltChck = (FilterCheckedListPickerItem)sender;

        artistFiltChck.Context = SongsCV.FilteringContext;
        artistFiltChck.FieldName = "OtherArtistsName";

    }

    private void AlbumFilterCheckedListPickerItem_Loaded(object sender, EventArgs e)
    {
        var albumFiltChck = (FilterCheckedListPickerItem)sender;

        albumFiltChck.Context = SongsCV.FilteringContext;
        albumFiltChck.FieldName = "AlbumName";

    }

    private void GenreFilterCheckedListPickerItem_Loaded(object sender, EventArgs e)
    {
        var albumFiltChck = (FilterCheckedListPickerItem)sender;
        albumFiltChck.ItemsSource = MyViewModel.SearchResults.Select(x => x.Genre).ToList();
        albumFiltChck.Context = SongsCV.FilteringContext;
        albumFiltChck.FieldName = "GenreName";

    }

    private void LastDatePlayedFilterDateRange_Loaded(object sender, EventArgs e)
    {
        var dateFilterEdit = (FilterDateRangeItem)sender;
        dateFilterEdit.Min = MyViewModel.SearchResults.Min(x => x.LastPlayed)?.DateTime;
        dateFilterEdit.Max = MyViewModel.SearchResults.Max(x => x.LastPlayed)?.DateTime;
        dateFilterEdit.Context = SongsCV.FilteringContext;
        dateFilterEdit.FieldName = "LastPlayed";

    }

    private void DimsRangeSlider_Loaded(object sender, EventArgs e)
    {
        var dimsRangeSlider = (FilterNumericRangeSliderItem)sender;
        dimsRangeSlider.Min = MyViewModel.SearchResults.Min(x => x.PlayCompletedCount);
        dimsRangeSlider.Max = MyViewModel.SearchResults.Max(x => x.PlayCompletedCount);
        dimsRangeSlider.Context = SongsCV.FilteringContext;
        dimsRangeSlider.FieldName = "PlayCompletedCount";

    }

    private void SkipsRangeSlider_Loaded(object sender, EventArgs e)
    {
        var skipsRangeSlider = (FilterNumericRangeSliderItem)sender;
        skipsRangeSlider.Min = MyViewModel.SearchResults.Min(x => x.SkipCount);
        skipsRangeSlider.Max = MyViewModel.SearchResults.Max(x => x.SkipCount);
        skipsRangeSlider.Context = SongsCV.FilteringContext;
        skipsRangeSlider.FieldName = "SkipCount";
    }

    private void SortPopUp_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Show();
        return;

       
       
    }

    private void SortByFieldCV_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    {
        
    }

 

    private void CloseSortPopupBtn_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Close();
    }
    int currentSelectedSortIndex;

    private void ConfirmSortAndClosePopupBtn_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Close();
        SongsCV.SortDescriptions.Clear();
        switch (currentSelectedSortIndex)
        {
            case 0:

                break;
            case 1:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "Title"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });

                break;
            case 2:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "ArtistName"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });

                break;
            case 3:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "AlbumName"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });

                break;
            case 4:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "Genre Name"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });
                break;
            case 5:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "DurationInSeconds"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });
                break;
            case 6:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "PlayCompletedCount"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });
                break;
            case 7:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "LastPlayed"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });
                break;
            case 8:
                SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription()
                {
                    FieldName = "DateCreated"
           ,
                    SortOrder = (DataSortOrder)MyViewModel.CurrentSortOrderInt
                });
                break;
            default:
                break;
        }
    }

    private void SortDownBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf((send.BindingContext as string)!);
        MyViewModel.CurrentSortDisplay = (send.BindingContext as string)!;
        MyViewModel.CurrentSortOrder = SortOrder.Desc;
        MyViewModel.CurrentSortOrderInt = 2;
    }

    private void SortUpBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf((send.BindingContext as string)!);
        MyViewModel.CurrentSortDisplay = (send.BindingContext as string)!;
        MyViewModel.CurrentSortOrder = SortOrder.Asc;
        MyViewModel.CurrentSortOrderInt = 1;

    }

    private void SortFieldBtn_Clicked(object sender, EventArgs e)
    {
        DXButton send = (DXButton)sender;
        var selectedField = send.BindingContext as string;
        if (string.IsNullOrEmpty(selectedField)) return;
        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf(selectedField);

        if(currentSelectedSortIndex ==0)
        {
            MyViewModel.CurrentSortOrder = SortOrder.None;
            MyViewModel.CurrentSortOrderInt = 0;
            return;
        }
        if(MyViewModel.CurrentSortDisplay== selectedField)
        {
            MyViewModel.CurrentSortOrder = MyViewModel.CurrentSortOrder == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
            MyViewModel.CurrentSortOrderInt = (int)MyViewModel.CurrentSortOrder;
            
        }

        MyViewModel.CurrentSortDisplay = (send.BindingContext as string)!;
    }

    private void HasSyncLyricsFilter_Loaded(object sender, EventArgs e)
    {
        var send = (FilterCheckItem)sender;
        send.Context = SongsCV.FilteringContext;
        send.FieldName = "HasSyncedLyrics";


    }

    private void SelectedSongArtistBtn_Clicked(object sender, EventArgs e)
    {

    }

    private async void GoToSelectedSongAlbumPage_Clicked(object sender, EventArgs e)
    {
        MyViewModel.SetSelectedAlbum(MyViewModel.SelectedSong!.Album);
        await SingleSongBtmSheet.CloseAsync();
        await Shell.Current.GoToAsync(nameof(AlbumPage));
    }

    private async void GoToSelectedSongOverViewPage_Clicked(object sender, EventArgs e)
    {
        await SingleSongBtmSheet.CloseAsync();
                await Shell.Current.GoToAsync(nameof(DetailsOverview));
    }

    

    private async void ToggleFavBtn_Tap(object sender, HandledEventArgs e)
    {
        await MyViewModel.AddFavoriteRatingToSongAsync(MyViewModel.SelectedSong!);
    }

    private async void ToggleFavBtn_LongPress(object sender, HandledEventArgs e)
    {
        await MyViewModel.RemoveSongFromFavoriteAsync(MyViewModel.SelectedSong!);
    }

    private  void OpenSortBtn_Clicked(object sender, EventArgs e)
    {
       FilterBottomSheet.Close();
        SortPopUp.Show();
    }

    private void OpenFilterBtn_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Close();
        FilterBottomSheet.Show();
        
    }






    //private void SearchIconBtn_Tapped(object sender, HandledEventArgs e)
    //{
    //    SearchBarTextEdit.Focus();
    //}

    //private void SearchBarTextEdit_Focused(object sender, FocusEventArgs e)
    //{

    //    TQLFilterExpander.SetIsExpanded(true, true);
    //    //InputMethodManager? imm = (InputMethodManager?)MainApplication.Context.GetSystemService(Activity.InputMethodService);
    //    //var view = SearchBarTextEdit.Handler?.PlatformView as Android.Views.View;
    //    //imm?.ShowSoftInput(view, ShowFlags.Implicit);

    //}

    //private void SearchBarTextEdit_Unfocused(object sender, FocusEventArgs e)
    //{
    //    TQLFilterExpander.SetIsExpanded(false, true);

    //}

    //private void ArtistsPicker_Tap(object sender, HandledEventArgs e)
    //{
    //    Debug.WriteLine(sender?.GetType());
    //    //FilteredArtistsChoiceChip
    //}

    //private void ArtistsPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //{
    //    //if(e.PropertyName == nameof(ArtistsPicker.SelectedItems))
    //    //{

    //    //    FilteredArtistsChoiceChip.ItemsSource = ArtistsPicker.SelectedItems;
    //    //    // this should, 
    //    //}
    //}

    //private void IncludeFavCheckEdit_CheckedChanged(object sender, EventArgs e)
    //{
    //    //switch (IncludeFavCheckEdit.IsChecked)
    //    //{
    //    //    case null:

    //    //        break;
    //    //    case true:

    //    //        break;
    //    //    case false:

    //    //        break;
    //    //    default:
    //    //        break;
    //    //}
    //}

    //private void AddRule_Clicked(object sender, EventArgs e)
    //{
    //    //string? selectedField = ArtistFieldPicker.SelectedItem?.ToString();
    //    //if (string.IsNullOrEmpty(selectedField)) return;

    //    //var newRule = new VisualFilterRule();

    //    //switch (selectedField)
    //    //{
    //    //    case "Artist":
    //    //        // Here, you would open your ArtistsPicker, get the result, and assign it
    //    //        //string chosenArtist = await PickArtistAsync(); // Implement this UI flow
    //    //        //newRule.FieldAlias = "ar";
    //    //        //newRule.DisplayField = "Artist";
    //    //        //newRule.Value = chosenArtist;
    //    //        break;

    //    //    case "Favorites":
    //    //        newRule.FieldAlias = "fav";
    //    //        newRule.DisplayField = "Favorites";
    //    //        newRule.Value = "true";
    //    //        break;

    //    //        // Add cases for Genre, Year, Length, etc.
    //    //}

    //    //MyViewModel.ActiveFilterRules.Add(newRule);
    //    MyViewModel.UpdateGeneratedTql();
    //}

    //private void ArtistFieldPicker_Tap(object sender, HandledEventArgs e)
    //{

    //}

    //private void ArtistToggleButton_CheckedChanged(object sender, ValueChangedEventArgs<bool> e)
    //{
    //    switch (e.NewValue)
    //    {
    //        case true:
    //            //ArtistFieldPicker.Commands.Show.Execute(null);
    //            break;
    //        case false:
    //            break;
    //        default:
    //            break;
    //    }
    //}

    //private void SearchIconBtn_DoubleTap(object sender, HandledEventArgs e)
    //{


    //    var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

    //    SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);

    //}

    //private void TQLMyFavChip_Tap(object sender, HandledEventArgs e)
    //{
    //    SearchBarTextEdit.Text = SearchBarTextEdit.Text + " my fav";


    //}

    //private void TQLShuffleChip_Tap(object sender, HandledEventArgs e)
    //{

    //    SearchBarTextEdit.Text = SearchBarTextEdit.Text + " shuffle";
    //}

    //private void EditSongChip_Tap(object sender, HandledEventArgs e)
    //{

    //}

    //private async void ViewSongChip_Tap(object sender, HandledEventArgs e)
    //{

    //    if (Shell.Current.CurrentPage.GetType() != typeof(DetailsOverview))
    //    {
    //        SingleSongPopup.Close();
    //        await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
    //    }
    //}
}