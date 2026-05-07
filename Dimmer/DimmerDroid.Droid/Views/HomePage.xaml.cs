
global using View = Microsoft.Maui.Controls.View;
global using Dimmer.Views.CustomViews;


namespace Dimmer.Views;

public partial class HomePage : ContentPage
{
    public HomePage(BaseViewModelAnd viewModelAnd, LastFMViewModel lastFMVM, SingleSongInCVPopup singleSongPop)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
        SingleSongPopup = singleSongPop;
        lastFMVM.LoadBaseViewModel(viewModelAnd);
    }
    SingleSongInCVPopup SingleSongPopup{ get; }
    BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Debug.WriteLine("HomePage OnAppearing" + MyViewModel.AppTitle +" "+BaseViewModel.CurrentAppStage);
        Debug.WriteLine("HomePage OnAppearing" + MyViewModel.SearchResults.Count);
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
        MainPageTabView.SelectedItemIndex = 2;
    }

    private void SearchBtn_Clicked(object sender, EventArgs e)
    {
        MainPageTabView.SelectedItemIndex = 0;
    }

    private void SongInCVTapGR_Tapped(object sender, TappedEventArgs e)
    {
        
    }

    private void SongInCVImgTapGR_Tapped(object sender, TappedEventArgs e)
    {
        var song = e.Parameter as SongModelView;
        if (song is null) return;
        SingleSongPopup.SetSelectedSongToShow(song);

        SingleSongPopup.Show();
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

    //private async void ToggleFavBtn_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    //{
    //   await MyViewModel.AddFavoriteRatingToSongAsync(MyViewModel.SelectedSong!);
    //}


    //private async void DeleteSongBtn_Tap(object sender, HandledEventArgs e)

    //{
    //    var result = await Shell.Current.DisplayAlertAsync("Confirm Delete",
    //        "Delete Song", "Yes", "No");
    //    if (result)
    //    {
    //        await MyViewModel.DeleteSongs(new List<SongModelView>(){MyViewModel.SelectedSong! });
    //    }
    //}

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

    //private void PlayNextBtn_Clicked(object sender, EventArgs e)
    //{
    //    if(MyViewModel.SelectedSong is null)
    //        return;
    //    var song = MyViewModel.SelectedSong;
    //    MyViewModel.AddToNext(new List<SongModelView>() { song });


    //    var snackMsg = $"Added {song.Title} by {MyViewModel.SelectedSong.ArtistName} to Next in Queue";

    //    CommunityToolkit.Maui.Alerts.Toast msgToast = new CommunityToolkit.Maui.Alerts.Toast() { Text = snackMsg, Duration= CommunityToolkit.Maui.Core.ToastDuration.Short };

    //    SingleSongBtmSheet.Close();
    //    msgToast.Show();
    //}

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

    //private void MoreBtn_Tap(object sender, DXTapEventArgs e)
    //{

    //    var send = (View)sender;
    //    var song = (SongModelView)send.BindingContext;
    //    MyViewModel.SelectedSong = song;
    //    SingleSongBtmSheet.Show();
    //}

    //private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    //{
    //    var send = (View)sender;
    //    var song = send.BindingContext as SongModelView;
    //    MyViewModel.SelectedSong = song;

    //    await SingleSongPopup.ShowAsync();

    //    //if (Shell.Current.CurrentPage.GetType() != typeof(DetailsOverview))
    //    //    await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
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

    //private async void SelectedSongBtmSheetArtistNameChip_Tap(object sender, HandledEventArgs e)
    //{

    //    var send = (View)sender;
    //    var song = MyViewModel.SelectedSong;
    //    if(song is null)
    //        return;

    //    MyViewModel.SetSelectedArtist(song.Artist);
    //    await SingleSongBtmSheet.CloseAsync();
    //    await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    //}

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

    //private void SongsCV_PropertyChanged(object sender, PropertyChangedEventArgs e)
    //{
    //    if(e.PropertyName == nameof(SongsCV.VisibleItemCount))
    //    {

    //        var newCount = (SongsCV.ItemsSource as ReadOnlyObservableCollection<SongModelView>)?.Count;
    //        string? fullStr = newCount.ToString();
    //        SearchBarTextEdit.Suffix = fullStr;
    //    }
    //}

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