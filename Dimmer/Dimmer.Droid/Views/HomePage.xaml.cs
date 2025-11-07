

using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Android.Text;

using Dimmer.Utils.Extensions;

using Microsoft.Maui.Layouts;

using Realms;

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
        this.HideSoftInputOnTapped=true;

    }

    protected override async void OnAppearing()
    {
        try
        {

            base.OnAppearing();
            MyViewModel.MyHomePage = this;
            //MyViewModel.MyHomePage.pla

        MainViewTabView.SelectedItemIndex = 0;


        _ = InitializeAsync();

    }

    private async Task InitializeAsync()
    {
        MyViewModel.DumpCommand.Execute(null);
        try
        {
            MyViewModel.CurrentPageContext = CurrentPage.HomePage;
            MyViewModel.CurrentMAUIPage = this;

            if (MyViewModel.ShowWelcomeScreen)
            {
                //await Shell.Current.GoToAsync(nameof(WelcomePage), true);
                return;
            }

            await Task.Delay(4000);

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        MyViewModel.DumpCommand.Execute(null);
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


        MyViewModel.SetCurrentlyPickedSongForContext(paramss);



        //SongsMenuPopup.Show();

    }


    private async void GotoArtistBtn_Clicked(object sender, EventArgs e)
    {

        var song = MyViewModel.SelectedSong;
        if (song is null)
        {
            return;
        }
        await MyViewModel.SelectedArtistAndNavtoPage(song);

        //await SongsMenuPopup.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
    }

    private void SongsColView_Tap(object sender, CollectionViewGestureEventArgs e)
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
        //     SongsColView.ScrollTo(songs.FirstOrDefault(), ScrollToPosition.StartAsync, true);
        // }
    }

    private bool SortSongs(string sortProperty)
    {
        if (string.IsNullOrEmpty(sortProperty))
            return false;


        // Update current sort state
        MyViewModel.CurrentSortProperty = sortProperty;


        SortOrder newOrder;

        // Toggle order if sorting by the same property again
        newOrder = (MyViewModel.CurrentSortOrder == SortOrder.Asc) ? SortOrder.Desc : SortOrder.Asc;


        MyViewModel.CurrentSortOrder = newOrder;
        MyViewModel.CurrentSortOrderInt = (int)newOrder;
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

        var pl = MyViewModel.AllPlaylists;

        var listt = new List<SongModelView>();

        listt.Add(song);

        MyViewModel.AddToPlaylist("Playlists", listt, MyViewModel.CurrentTqlQuery);
    }

    private void CloseNowPlayingQueue_Tap(object sender, HandledEventArgs e)
    {

        Debug.WriteLine(this.Parent.GetType());

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

        if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }

        //await this.AnimateFadeOutBack(600);
        //await this.CloseAsync();
    }
    private async void SongTitleChip_Tap(object sender, HandledEventArgs e)
    {
        //await CloseAsync();

        MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
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



    private void myPageSKAV_Closed(object sender, EventArgs e)
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.MyHomePage = null;
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

        MyViewModel.SeekTrackPosition(send.Value);


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

        MyViewModel.SearchSongForSearchResultHolder(txt);
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
        DXCollectionView? send = sender as DXCollectionView;
        if (send is null) return;
        var sel = send.SelectedItem;

        var ind = send.FindItemHandle(sel);
        send.ScrollTo(ind, DXScrollToPosition.End);

        //int itemHandle = AllLyricsColView.FindItemHandle(MyViewModel.cur);
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

    private void BtmBar_ToggleAdvanceFilters(object sender, EventArgs e)
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

    private void am_DoubleTap(object sender, HandledEventArgs e)
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
        MyViewModel.SelectedSong=song;
        await MyViewModel.SaveUserNoteToSong(song);
        //await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    protected override bool OnBackButtonPressed()
    {
        return SwitchUINowPlayingOrNot();
        //return base.OnBackButtonPressed();
    }

    private bool SwitchUINowPlayingOrNot()
    {
        switch (MainViewTabView.SelectedItemIndex)
        {
            case 0:
                MainViewTabView.SelectedItemIndex=1;
                break;
                
            case 1:
                MainViewTabView.SelectedItemIndex=0;
                break;

            default:
                break;
        }



        return true;
    }

    private async void MoreaIcon_Clicked(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        SongModelView song = send.BindingContext as SongModelView;
        MyViewModel.SelectedSong=song;
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    private void DXButton_Clicked_4(object sender, EventArgs e)
    {

    }
    private void SongsColView_FilteringUIFormShowing(object sender, FilteringUIFormShowingEventArgs e)
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
        var send = (View)sender;
        var song = send.BindingContext as SongModelView;
        MyViewModel.SelectedSong = song;
        MoreBtmSheet.State = BottomSheetState.HalfExpanded;
       
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
        await MyViewModel.PlaySong(song, CurrentPage.AllSongs);


    }

    private void Chip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ViewNPQ_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(MyViewModel.CurrentPlaybackQuery);
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
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("album", val));
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
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", selectedArtist));

    }


    private void MainViewExpander_ViewSongOnlyEvt(object sender, EventArgs e)
    {



        //var send = (Element)sender;
        //await MoreModal.ShowAsync(send);
    }


    //TopBeforeColView section
    private void TopBeforeColView_Loaded(object sender, EventArgs e)
    {

        var realm = MyViewModel.RealmFactory.GetRealmInstance();

        //MyViewModel

    }

    private void TopBeforeColView_Unloaded(object sender, EventArgs e)
    {

    }




    public ObservableCollection<string> _liveArtists;
    public ObservableCollection<string> _liveAlbums;
    public ObservableCollection<string> _liveGenres;



    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;

    private void BtmBar_RequestFocusOnMainView(object sender, EventArgs e)
    {
        //if (!TopBeforeColView.IsExpanded)
        //{
        //    TopBeforeColView.IsExpanded= !TopBeforeColView.IsExpanded;

        //    SearchBy.Focus();
        //    await OpenedKeyboardToolbar.DimmInCompletelyAndShow();
        //}
        //else
        //{
        //    TopBeforeColView.IsExpanded=false;
        //}
    }

 

    private void SearchBy_Focused(object sender, FocusEventArgs e)
    {
        MainViewTabView.SelectedItemIndex = 0;
    }
    private void ScrollToCurrSong_Tap(object sender, HandledEventArgs e)
    {

        if (MyViewModel.CurrentPlayingSongView.Title is not null)
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                int itemHandle = MyViewModel.SongsColView.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                MyViewModel.SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
            });
           


        }
    }
    private async void ArtistsChip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var songModel = send.DoubleTapCommandParameter as SongModelView;
        if (songModel != null) {
            if (send.Parent.Parent.Parent.GetChildrenInTree(true).First(x => x.GetType() == typeof(DXPopup)) is not DXPopup popUpChild) return;

            if (send.Parent.Parent.Parent.GetChildrenInTree(true).First(x => x.GetType() == typeof(SfEffectsView)) is not SfEffectsView imgEffView) return;


            var realm = MyViewModel.RealmFactory.GetRealmInstance();
            var song = realm.Find<SongModel>(songModel.Id);

            if (song == null) return;
            var artistsLinkedToSong = song.ArtistToSong.ToList();
            
            var artistsList = MyViewModel._mapper.Map<List<ArtistModelView>>(artistsLinkedToSong);
            
            var artColView = popUpChild.GetChildrenInTree(true).First(x => x.GetType() == typeof(DXCollectionView)) as DXCollectionView;
            artColView?.ItemsSource = null;
            artColView?.ItemsSource = artistsList;
            await Task.Delay(50);




            await popUpChild.ShowAsync(imgEffView);
            //popUpChild.ComputeDesiredSize
        }
    }
    private async Task ShowSmartPopup(View anchor, DXPopup popup)
    {
        if (popup?.Content == null)
            return;

        // Measure popup height dynamically
        var measured = popup.Content.Measure(double.PositiveInfinity, double.PositiveInfinity);
        var popupHeight = measured.Height;
        popup.HeightRequest = popupHeight;

        popup.WidthRequest = measured.Width > 0 ? measured.Width : 300;

        // Get window reference
        var window = Application.Current?.MainPage?.Window;
        if (window == null) return;

        // Get absolute position of the anchor in window coordinates
        var anchVisElt = anchor as VisualElement;

        var anchorBounds = ViewExts.GetAbsoluteBounds(anchor,window);
        
        double screenHeight = window.Height;
        double screenWidth = window.Width;

        // Center horizontally below the anchor
        double popupX = anchorBounds.X + (anchorBounds.Width / 2) - (popup.WidthRequest / 2);

        // Check if popup fits below the anchor
        bool fitsBelow = anchorBounds.Bottom + popup.HeightRequest <= screenHeight;
        double popupY = fitsBelow
            ? anchorBounds.Bottom          // show below
            : anchorBounds.Top - popup.HeightRequest; // show above

        // Clamp horizontally to screen edges
        popupX = Math.Max(0, Math.Min(popupX, screenWidth - popup.WidthRequest));
        popupY = Math.Max(0, Math.Min(popupY, screenHeight - popup.HeightRequest));
        
        await popup.ShowAtAsync(popupX, popupY, 0, anchor);
    }


    private void AlbumFilter_LongPress(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        SearchBy.Text=
        TQlStaticMethods.SetQuotedSearch("album", send.LongPressCommandParameter as string);
    }

    // The "Years" methods remain unchanged.
    private void QuickFilterYears_LongPress(object sender, HandledEventArgs e)
    {

        var send = (Chip)sender;
        SearchBy.Text=
        TQlStaticMethods.SetQuotedSearch("year", send.LongPressCommandParameter as string);
    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {
        if (MainViewTabView.SelectedItemIndex != 0)
        {
            MainViewTabView.SelectedItemIndex=0;
        }
        var send = (TextEdit)sender;
        EditText? nativeView = send.GetPlatformView() as EditText;
        if (nativeView != null)
        {
            MyViewModel.SearchSongForSearchResultHolder(send.Text);

            //check if it's native textfield so we can leverage full native power for selections/editing
        }


    }

    private void Settings_Tap(object sender, HandledEventArgs e)
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }



    public event EventHandler? ViewSongOnlyEvt;
    //private void ViewSongOnly_TouchDown(object sender, EventArgs e)
    //{
    //    var send = (SfEffectsView)sender;
    //    var song = (SongModelView)send.TouchDownCommandParameter;
    //    if (song is null)
    //    {
    //        return;
    //    }
    //    MyViewModel.SelectedSong = song;
    //    // raise event to notify the parent view to handle the touch down event
    //    ViewSongOnlyEvt?.Invoke(this, e);
    //}
    // MainViewExpander section
    private void MainViewExpander_Loaded(object sender, EventArgs e)
    {
    }
    private void MainViewExpander_Unloaded(object sender, EventArgs e)
    {
    }

    //private void ArtistsChip_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    //{

    //}

    //private void AlbumFilter_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    //{

    //}

    //private void MoreIcon_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    //{

    //}

    //private void MoreIcon_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    //{

    //}

    private void SongsColView_FilteringUIFormShowing_1(object sender, FilteringUIFormShowingEventArgs e)
    {

    }

    private void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {

    }



    //private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    //{
    //    //DXBorder send = (DXBorder)sender;


    //    await MyViewModel.PlayPauseToggle();

    //}

    private double _startX;
    private double _startY;
    private bool _isPanning;

    double btmBarHeight = 145;


    public event EventHandler RequestFocusOnMainView;
    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        View send = (View)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _startX = send.TranslationX;
                _startY = send.TranslationY;
                break;

            case GestureStatus.Running:
                if (!_isPanning)
                    return; // Safety check

                send.TranslationX = _startX + e.TotalX;
                send.TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
                _isPanning = false;

                double deltaX = send.TranslationX - _startX;
                double deltaY = send.TranslationY - _startY;
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

                                await MyViewModel.NextTrackAsync();

                                Task<bool> bounceTask = send.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                            else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                await MyViewModel.PreviousTrack();

                                Task<bool> bounceTask = send.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(bounceTask);
                            }
                        }
                        catch (Exception ex) // Handle exceptions
                        {
                            Debug.WriteLine($"Error: {ex.Message}"); // Log the error
                        }
                        finally
                        {
                            send.TranslationX = 0; // Reset translation
                            send.TranslationY = 0; // Reset translation

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

                            RequestFocusOnMainView?.Invoke(send, EventArgs.Empty);
                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  // Up
                    {
                        try
                        {
                            MainViewTabView.SelectedItemIndex = 1;
                            btmBarHeight=send.Height;

                            //if (MyViewModel.CurrentPlayingSongView.Title is not null)
                            //{

                            //    MainThread.BeginInvokeOnMainThread(() =>
                            //    {
                            //        int itemHandle = MyViewModel.SongsColView.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                            //        MyViewModel.SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
                            //    });
                            //    btmBarHeight=send.Height;



                            //}
                        }
                        catch { }
                    }

                }

                await send.TranslateTo(0, 0, 450, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:
                _isPanning = false;
                await send.TranslateTo(0, 0, 350, Easing.BounceOut); // Return to original position
                break;

        }
    }

    private void DurationAndSearchChip_LongPress(object sender, HandledEventArgs e)
    {
        this.ScrollToStart?.Invoke(this, e);
        //TextEdit SearchBy = this.Parent.FindByName<TextEdit>("SearchBy");
        //SearchBy.Focus();
    }
    public static DXCollectionView PageColView { get; set; }
    public event EventHandler RequestFocusNowPlayingUI;
    public event EventHandler ScrollToStart;
    public event EventHandler ToggleAdvanceFilters;

    private void DurationAndSearchChip_DoubleTap(object sender, HandledEventArgs e)
    {
        ScrollToStart?.Invoke(this, e);
    }

    private void DurationAndSearchChip_Tap(object sender, HandledEventArgs e)
    {
        ToggleAdvanceFilters?.Invoke(this, e);
    }

    private void BtmBarr_Loaded(object sender, EventArgs e)
    {

    }

    private void BtmBarr_Unloaded(object sender, EventArgs e)
    {

    }




    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var paramss = send.CommandParameter as SongModelView;
        if (paramss is null)
        {
            return;
        }
        selectedSongPopUp = paramss;
        MyViewModel.SetCurrentlyPickedSongForContext(paramss);

    }




    private async void DXButton_Clicked_3(object sender, EventArgs e)
    {

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
        //await this.CloseAsync();
    }

    private void ByAll()
    {

    }
    private void ByArtist()
    {

    }


    private void DXStackLayout_SizeChanged(object sender, EventArgs e)
    {

    }

    private void TextEdit_TextChanged(object sender, EventArgs e)
    {
        var send = (TextEdit)sender;

        MyViewModel.SearchSongForSearchResultHolder(send.Text);
    }

    private void AutoCompleteEdit_TextChanged(object sender, DevExpress.Maui.Editors.AutoCompleteEditTextChangedEventArgs e)
    {
        var send = (AutoCompleteEdit)sender;
        var cursorPosition = send.CursorPosition;
        // Get suggestions based on the current text fragment
        var suggestions = AutocompleteEngine.GetSuggestions(
            _liveArtists, _liveAlbums, _liveGenres, send.Text, cursorPosition);
        send.ItemsSource = suggestions;


        MyViewModel.SearchSongForSearchResultHolder(send.Text);
    }


    private void AutoCompleteEdit_SelectionChanged(object sender, EventArgs e)
    {

    }

    private void TextEdit_TextChanged_1(object sender, EventArgs e)
    {

    }

    private void SongsColView_Scrolled(object sender, DXCollectionViewScrolledEventArgs e)
    {

    }


    private void MainSongsColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.SongsColViewNPQ ??= SongsColView;

    }
    View SelectedSongView;
    private async void SongView_Loaded(object sender, EventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext as SongModelView;

        await MyViewModel.LoadSongDominantColorIfNotYetDoneAsync(song);

        TouchBehavior tch = new TouchBehavior()
        {

        };

        //var beh = send.Behaviors.Add(tch);


        MyViewModel.SelectedSongView=send;
    }

    private void PlaybackQueueColView_Loaded(object sender, EventArgs e)
    {
        //MyViewModel.PlaybackQueueColView = this.PlaybackQueueColView;

    }

    private void PlaybackQueueColView_Unloaded(object sender, EventArgs e)
    {
        MyViewModel.PlaybackQueueColView = null;

    }

    private void SearchBtn_Clicked(object sender, EventArgs e)
    {
        MainViewTabView.SelectedItemIndex = 0;
        SearchBy.Focus();
    }

    private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {

    }

    private async void NavToSong_Tapped(object sender, TappedEventArgs e)
    {
        var song = e.Parameter as SongModelView;
        if (song is null)
        {
            return;
        }
        MyViewModel.SelectedSong = song;
        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    private async void NavToArtist_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Chip)sender;
        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        {
            await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        }

    }

    private async void NavToAlbum_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Chip)sender;
        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        var val = song.AlbumName;
        if (string.IsNullOrWhiteSpace(val))
            return; // No album to show
        await Shell.Current.GoToAsync(nameof(AlbumPage), true);
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("album", val));
    }

    private void MainViewTabView_Loaded(object sender, EventArgs e)
    {
        
    }

    private void MainViewTabView_Unloaded(object sender, EventArgs e)
    {

    }


    private void SearchSongSB_Clicked(object sender, EventArgs e)
    {
       
    }

    private void MoreBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {

    }


    private void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

    }



    private void BtmBarTap_Tapped(object sender, TappedEventArgs e)
    {
        MainViewTabView.SelectedItemIndex = 1;
    }

    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, HandledEventArgs e)
    {
        try
        {

            var send = (Chip)sender;
            var song = send.TapCommandParameter as SongModelView;
            if (song is null) return;
            await MyViewModel.ShareSongDetailsAsText(song);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void OnAddQuickNoteClicked(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        // Prompt the user for a note


        await MyViewModel.SaveUserNoteToSong(song);
    }

    private async void QuickSearchArtist_Clicked(object sender, HandledEventArgs e)
    {

        var send = (Chip)sender;
        var song = send.BindingContext as SongModelView;
        if (song is null) return;
        var val = song.OtherArtistsName;
        char[] dividers = [',', ';', ':', '|', '-'];

        var namesList = val
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
            .Select(name => name.Trim())                           // Trim whitespace from each name
            .ToArray();                                             // Convert to a List

        if (namesList is null) return;
        if ( namesList.Length == 1)
        {
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", namesList[0]));

            return;
        }

        var selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

        if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
        {
            return;
        }

        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", selectedArtist));

        return;
    }

    private void SyncShare_Tap(object sender, HandledEventArgs e)
    {

    }

    private async void OnLabelClicked(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;
       
        var param = send.TapCommandParameter as string;

       

        switch (param)
        {
            case "DeleteSys":

                var listOfSongsToDelete = new List<SongModelView> { MyViewModel.SelectedSong! };

                await MyViewModel.DeleteSongs(listOfSongsToDelete);
                break;
            case "OpenFileExp":

                await MyViewModel.OpenFileInOtherApp(MyViewModel.SelectedSong);
                break;

            default:
                break;
        }
    }

    private void ViewGenreMFI_Clicked(object sender, HandledEventArgs e)
    {
        var s = (Chip)sender;
        var param = s.TapCommandParameter as string;
        if(!string.IsNullOrEmpty(param))
        {
 
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByGenre(param));

        }
    }

    private void QuickSearchAlbum_Clicked(object sender, HandledEventArgs e)
    {
        var s = (Chip)sender;
        var param = s.TapCommandParameter as string;
        if (!string.IsNullOrEmpty(param))
        {

            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(param));    

        }
    }

    private void ArtistsContextMenu_Loaded(object sender, EventArgs e)
    {
        var send = (DXPopup)sender;

        MyViewModel.ArtistContextMenu = send;
    }

    private void ArtistsContextMenu_Unloaded(object sender, EventArgs e)
    {

        MyViewModel.ArtistContextMenu = null;
    }

    DXCollectionView? ArtistNamesColView { get; set; }
    DXPopup? ArtistNamesPopUpView { get; set; }
    private void ArtistNamesColView_Loaded(object sender, EventArgs e)
    {
        DXCollectionView send = (DXCollectionView)sender;

        ArtistNamesColView = send;
    }

    private void ArtistsContextMenu_Opened(object sender, EventArgs e)
    {
        ArtistNamesPopUpView = (DXPopup)sender;
        
    }

    private void ArtistChipName_Tap(object sender, HandledEventArgs e)
    {
        var s = (Chip)sender;
        var param = s.Text as string;
        if (!string.IsNullOrEmpty(param))
        {
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(param));
            ArtistNamesPopUpView?.Close();
        }
    }

    private void ArtistsContextMenu_Loaded_1(object sender, EventArgs e)
    {

    }

    private void Chip_Tap_1(object sender, HandledEventArgs e)
    {

    }

    private void AddArtistToTQL_Tap(object sender, HandledEventArgs e)
    {
        var s = (Chip)sender;
        var param = s.TapCommandParameter as ArtistModelView;

        if (param is not null)
        {
            MyViewModel.AddArtistToTQLCommand.Execute(param.Name);
        }

    }

    private EditText? _nativeSearchBox;
    private void SearchBy_Loaded(object sender, EventArgs e)
    {
        if (sender is not TextEdit searchBox)
            return;

        // Get the native Android EditText
        _nativeSearchBox = searchBox.GetPlatformView() as EditText;
        if (_nativeSearchBox == null)
            return;

        // Avoid double-hooking
        if (_nativeSearchBox.Tag?.ToString() == "shuffle_hooked")
            return;

        _nativeSearchBox.AfterTextChanged += NativeSearchBox_AfterTextChanged;
        _nativeSearchBox.Tag = "shuffle_hooked";
    }

    private void NativeSearchBox_AfterTextChanged(object? sender, AfterTextChangedEventArgs e)
    {
        if (sender is not EditText nativeView)
            return;

        string text = nativeView.Text ?? "";
        string[] keywords = { "shuffle", "random" };

        foreach (var kw in keywords)
        {
            var match = Regex.Match(text, $@"\b{kw}\b", RegexOptions.IgnoreCase);
            if (match.Success && match.Index < text.Length - kw.Length)
            {
                text = Regex.Replace(text, $@"\b{kw}\b", "", RegexOptions.IgnoreCase).Trim();
                text = $"{text} {kw}".Trim();

                // Update text and caret without re-triggering chaos
                nativeView.Text = text;
                nativeView.SetSelection(text.Length);
                nativeView.PerformHapticFeedback(Android.Views.FeedbackConstants.TextHandleMove);
                break;
            }
        }

        // Run search logic
        MyViewModel.SearchSongForSearchResultHolder(text);
    }

    private void SearchBy_Unloaded(object sender, EventArgs e)
    {
        if (_nativeSearchBox != null)
        {
            _nativeSearchBox.AfterTextChanged -= NativeSearchBox_AfterTextChanged;
            _nativeSearchBox.Tag = null;
            _nativeSearchBox = null;
        }
    }

    private void RandomSearch_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.Shuffle());
    }

    private void ClearSearch_Tap(object sender, HandledEventArgs e)
    {
        //MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.SortByTitleAsc)
    }

    private  void QuickSearchArtist_Clicked(object sender, DXTapEventArgs e)
    {
        
    }

    private async void OnAddQuickNoteClicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        // Prompt the user for a note


        await MyViewModel.SaveUserNoteToSong(song);
    }

    private void SyncShare_Tap(object sender, EventArgs e)
    {
        
    }

    private async void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {

        try
        {

            var send = (Chip)sender;
            var song = send.BindingContext as SongModelView;
            if (song is null) return;
            var val = song.OtherArtistsName;
            char[] dividers = [',', ';', ':', '|', '-'];

            var namesList = val
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List
            if (namesList is not null && namesList.Length == 1)
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async void DeleteSysClicked(object sender, EventArgs e)
    {
        try
        {
            DXButton send = (DXButton)sender;
            var song = send.CommandParameter as SongModelView;
            if(song is not null)
                await MyViewModel.DeleteFileFromSystem(song);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void Slider_DragCompleted(object sender, EventArgs e)
    {

    }

    private void TitleChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistNameChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistNameChip_Loaded(object sender, EventArgs e)
    {
        var chipp = (Chip)sender;   
        var nativeView = chipp.Handler?.PlatformView as Android.Views.View;
        if (nativeView is null)
        {
            return;
        }

        nativeView.Click += ShowArtistPopup_Click;
    }

    private void ShowArtistPopup_Click(object? sender, EventArgs e)
    {
    }


    private void ShowArtistPopup(Android.Views.View anchor)
    {
        var song = MyViewModel?.CurrentPlayingSongView;
        var otherArtistsRaw = song?.OtherArtistsName ?? string.Empty;

        var dividers = new[] { ',', ';', ':', '|' };
        var namesList = otherArtistsRaw
            .Split(dividers, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (namesList.Length == 0)
            namesList = new[] { song?.ArtistName ?? "Unknown" };

        var popup = new PopupMenu(Platform.CurrentActivity, anchor);
        if (popup is null) return;
        popup.Menu.Clear();

        // Top info
        popup.Menu.Add($"♪ {song?.Title ?? "(Unknown song)"}").SetEnabled(false);
        popup.Menu.Add($"💿 {song?.AlbumName ?? "(Unknown album)"}").SetEnabled(false);
        popup.Menu.Add($"👤 {string.Join(", ", namesList)}").SetEnabled(false);
        popup.Menu.AddSubMenu("──────────────");

        // For each artist
        foreach (var artist in namesList)
        {
            var artistMenu = popup.Menu.AddSubMenu($"Artist: {artist}");
            if (artistMenu is null) return;
            artistMenu.Add("Quick View").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.QuickViewArtist(artist))));

            var viewBy = artistMenu.AddSubMenu("View By…");
            viewBy.Add("Albums").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.NavigateToArtistPage(artist))));
            viewBy.Add("Genres").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.NavigateToArtistPage(artist))));

            var play = artistMenu.AddSubMenu("Play / Queue");
            play.Add("Play Songs In This Album").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.PlaySongsByArtistInCurrentAlbum(artist))));
            play.Add("Play All by Artist").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.PlayAllSongsByArtist(artist))));
            play.Add("Queue All by Artist").SetOnMenuItemClickListener(new SimpleListener(() => TryVM(a => a.QueueAllSongsByArtist(artist))));

            var findOn = artistMenu.AddSubMenu("Find On…");
            AddExternal(findOn, "Spotify", $"https://open.spotify.com/search/{Uri.EscapeDataString(artist)}");
            AddExternal(findOn, "YouTube Music", $"https://music.youtube.com/search?q={Uri.EscapeDataString(artist)}");
            AddExternal(findOn, "Bandcamp", $"https://bandcamp.com/search?q={Uri.EscapeDataString(artist)}&item_type=b");

            artistMenu.Add("Copy Artist Name").SetOnMenuItemClickListener(new SimpleListener(() =>
            {
                var clipboard = Android.Content.ClipboardManager.FromContext(Platform.CurrentActivity);
                var clip = Android.Content.ClipData.NewPlainText("artist", artist);
                clipboard.Text = clip.Description.Label;
            }));
        }
    }
    static void AddExternal(ISubMenu sub, string label, string url)
    {
        sub.Add(label).SetOnMenuItemClickListener(new SimpleListener(async () =>
        {
            try
            {
                var intent = new Android.Content.Intent(Android.Content.Intent.ActionView, Android.Net.Uri.Parse(url));
                Platform.CurrentActivity.StartActivity(intent);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Launch failed: {ex.Message}"); }
        }));
    }
    private void ArtistNameChip_Unloaded(object sender, EventArgs e)
    {

    }

    class SimpleListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener
    {
        private readonly Action _action;
        public SimpleListener(Action action) => _action = action;
        public bool OnMenuItemClick(IMenuItem item) { _action(); return true; }
    }

    private void TryVM(Action<IArtistActions> a)
    {
        if (MyViewModel is IArtistActions vm) a(vm);
    }
}
