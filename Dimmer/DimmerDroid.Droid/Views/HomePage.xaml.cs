
global using Dimmer.Views.CustomViews;
global using View = Microsoft.Maui.Controls.View;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Editors;
using Dimmer.DimmerSearch.TQL;
using Dimmer.Utilities;
using Syncfusion.Maui.Toolkit.Internals;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;


namespace Dimmer.Views;

public partial class HomePage : ContentPage
{
    public HomePage(BaseViewModelAnd viewModelAnd,  LastFMViewModel lastFMVM, LoginViewModel loginVM)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
        MyLastFMViewModel = lastFMVM;
        MyLoginVM = loginVM;
        compDisp = new();
       
      
        MyLastFMViewModel.LoadBaseViewModel(viewModelAnd);
        _ = Task.Run(() => loginVM.InitializeAsync());
     
    }
    
    BaseViewModelAnd MyViewModel { get; }
    public LastFMViewModel MyLastFMViewModel { get; }
    public LoginViewModel MyLoginVM { get; }


    protected override void OnDisappearing()
    {
        compDisp.Dispose();
        base.OnDisappearing();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        compDisp = new();






    }
    private async void MyPage_Loaded(object sender, EventArgs e)
    {




        if (!MyViewModel.IsInitialized)
        {
            InitializeAppLogic();

        }

        MyViewModel.StartTQLPipeLine();
    }
    CompositeDisposable compDisp;

    private void InitializeAppLogic()
    {
        _= Task.Run(async () =>
           {
               try
               {

                   var startTime = Java.Lang.JavaSystem.CurrentTimeMillis();

                   await MyViewModel.InitializeAllVMCoreComponents();

                   var duration = Java.Lang.JavaSystem.CurrentTimeMillis() - startTime;
                   Console.WriteLine($"InitializeAppLogic took {duration}ms");
                   if (duration > 2000)
                       Android.Util.Log.Warn("ANR_WARNING", $"InitializeAppLogic took {duration}ms - ANR risk!");
               }
               catch (Exception ex)
               {
                   await Shell.Current.DisplayAlertAsync("Fatal Error Init Logic", ex.Message, "ok");
                   Console.WriteLine($"VM INIT CRASH: {ex}");
                   Android.Util.Log.Error("DIMMER_INIT", ex.ToString());
               }
           });
    }

    private async void TapToPlaySongGestRecog_Tapped(object sender, TappedEventArgs e)
    {

        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        //var songsInCV = SongsCV.ItemsSource;

        if (song.TitleDurationKey == MyViewModel.CurrentPlayingSongView.TitleDurationKey) return;

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
        platView.Click -= PlatView_Click;
        platView.Click += PlatView_Click;
        if (platView is null)
            return;
      
    }

    private void PlatView_Click(object? sender, EventArgs e)
    {
        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
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
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.PlaybackQueue), v => MyViewModel.PlaybackQueue)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(pbQueue =>
            {
                if (pbQueue is null )
                {
                    return;
                }
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
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
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



 

    private void CloseSortPopupBtn_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Close();
    }
    int currentSelectedSortIndex;
    private bool isTQLBtmSheetOpened;

    public void ApplyTqlSortsToDevExpress(List<TQLSortDescription> sortDescriptions)
    {
        SongsCV.SortDescriptions.Clear();

        foreach (var sort in sortDescriptions)
        {
            SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription
            {
                FieldName = sort.PropertyName,
                SortOrder = sort.Direction == TQLSortDirection.Ascending
                    ? DataSortOrder.Ascending
                    : DataSortOrder.Descending
            });
        }
    }
    public void ApplySingleSort(string propertyName, DataSortOrder sortOrder)
    {
        SongsCV.SortDescriptions.Clear();

        if (string.IsNullOrEmpty(propertyName) || sortOrder == DataSortOrder.None)
            return;

        SongsCV.SortDescriptions.Add(new DevExpress.Maui.CollectionView.SortDescription
        {
            FieldName = propertyName,
            SortOrder = sortOrder
        });
    }
    private void ConfirmSortAndClosePopupBtn_Clicked(object sender, EventArgs e)
    {
        SortPopUp.Close();

        // Safe property name mapping using nameof() to prevent typos
        string propertyName = currentSelectedSortIndex switch
        {
            1 => nameof(SongModelView.Title),
            2 => nameof(SongModelView.OtherArtistsName),
            3 => nameof(SongModelView.AlbumName),
            4 => nameof(SongModelView.GenreName), 
            5 => nameof(SongModelView.DurationInSeconds),
            6 => nameof(SongModelView.PlayCompletedCount),
            7 => nameof(SongModelView.LastPlayed),
            8 => nameof(SongModelView.DateCreated),
            _ => string.Empty
        };

        var order = (DataSortOrder)MyViewModel.CurrentSortOrderInt;
        ApplySingleSort(propertyName, order);
    }

    private void SortDownBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var field = (send.BindingContext as string)!;
        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf(field);
        MyViewModel.CurrentSortDisplay = field;
        MyViewModel.CurrentSortOrder = SortOrder.Desc;
        MyViewModel.CurrentSortOrderInt = (int)DataSortOrder.Descending;
    }
    private void SortUpBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var field = (send.BindingContext as string)!;
        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf(field);
        MyViewModel.CurrentSortDisplay = field;
        MyViewModel.CurrentSortOrder = SortOrder.Asc;
        MyViewModel.CurrentSortOrderInt = (int)DataSortOrder.Ascending;
    }
    private void SortFieldBtn_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var selectedField = send.BindingContext as string;
        if (string.IsNullOrEmpty(selectedField)) return;

        currentSelectedSortIndex = MyViewModel.SortByFieldNameCollection.IndexOf(selectedField);

        if (currentSelectedSortIndex == 0)
        {
            MyViewModel.CurrentSortOrder = SortOrder.None;
            MyViewModel.CurrentSortOrderInt = (int)DataSortOrder.None;
            return;
        }

        if (MyViewModel.CurrentSortDisplay == selectedField)
        {
            MyViewModel.CurrentSortOrder = MyViewModel.CurrentSortOrder == SortOrder.Asc ? SortOrder.Desc : SortOrder.Asc;
            MyViewModel.CurrentSortOrderInt = (int)MyViewModel.CurrentSortOrder;
        }

        MyViewModel.CurrentSortDisplay = selectedField;
    }
    private void HasSyncLyricsFilter_Loaded(object sender, EventArgs e)
    {
        var send = (FilterCheckItem)sender;
        send.Context = SongsCV.FilteringContext;
        send.FieldName = "HasSyncedLyrics";

    }

    private async void SelectedSongArtistBtn_Clicked(object sender, EventArgs e)
    {
        if(MyViewModel.SelectedSong?.ArtistToSong.Count >1)
        {
            ArtistsMgtBtmSheet.Show();
        }
        else
        {
            MyViewModel.SetSelectedArtist(MyViewModel.SelectedSong.Artist);

            await  Shell.Current.GoToAsync(nameof(ArtistPage));
        }
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

    private void ScrollToFirstSongs_Clicked(object sender, EventArgs e)
    {
        
        var songHandle = SongsCV.GetItemHandleByVisibleIndex(0);
        SongsCV.ScrollTo(songHandle, DXScrollToPosition.Start);
        SortPopUp.Close();
    }


    private void ScrollToLastSongs_Clicked(object sender, EventArgs e)
    {

        var songHandle = SongsCV.GetItemHandleByVisibleIndex(SongsCV.VisibleItemCount-1);
        SongsCV.ScrollTo(songHandle, DXScrollToPosition.Start);
        SortPopUp.Close();
    }

    private void OtherArtistsName_Clicked(object sender, EventArgs e)
    {
        var dxBtn = (DXButton)sender;
        var song = dxBtn.CommandParameter as SongModelView;
        if (song is null) return;
        MyViewModel.SelectedSong = song;
        var songHandle = SongsCV.FindItemHandle(song);

        SongsCV.ScrollTo(songHandle, DXScrollToPosition.Start);
        ArtistsMgtBtmSheet.Show(BottomSheetState.HalfExpanded);
    }

    private async void SavePlayBackQueue_Clicked(object sender, EventArgs e)
    {
        var res = await Shell.Current.DisplayPromptAsync("Save Playlist", "Enter Playlist Name",
            keyboard: Keyboard.Text, placeholder: "ex; happy few ");
        if (string.IsNullOrEmpty(res)) return;

        List<SongModelView> songsInCV = new();
        for (int i = 0; i < PlaybackQueueCV.VisibleItemCount; i++)
        {
            var itemHandle = PlaybackQueueCV.GetItemHandleByVisibleIndex(i);

            if (PlaybackQueueCV.GetItem(itemHandle) is not SongModelView songByItemHandle) continue;
            songsInCV.Add(songByItemHandle);
        }
      await  MyViewModel.AddToPlaylistAsync("testPlayList", songsInCV, "testPL");
    }

    private async void AddFolderInSettings_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private void ApplyShuffleOnSongs_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(MyViewModel.CurrentTqlQueryUI))
        {
            MyViewModel.SearchToTQL("shuffle");

            return;

        }

        MyViewModel.SearchToTQL(MyViewModel.CurrentTqlQueryUI + " shuffle");    



    }

    private void ShowTQLShortBTMSheet_Clicked(object sender, EventArgs e)
    {
        TQLSearchGrid.IsVisible = true;
        SearchText.Focus();
        SearchText.CursorPosition = SearchText.Text?.Length is null ? 0 : SearchText.Text.Length;
        //TQLSearchBottomSheet.Show(BottomSheetState.FullExpanded);
    }

    private void TQLSearchBottomSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        switch (e.NewValue)
        {
            case BottomSheetState.FullExpanded:
                isTQLBtmSheetOpened = true;
                break;
            case BottomSheetState.HalfExpanded:
                break;
            case BottomSheetState.Hidden:
                isTQLBtmSheetOpened = false;
                break;
            default:
                break;
        }
    }

    private void DXButton_Clicked_1(object sender, EventArgs e)
    {

    }

    private void CloseTQLBtmSheet_Clicked(object sender, EventArgs e)
    {
        TQLSearchGrid.IsVisible = false;
    }

    private async void ViewArtistPage_Clicked(object sender, EventArgs e)
    {
        ArtistsMgtBtmSheet.Close();
        var art = ((DXButton)sender).CommandParameter as ArtistModelView;
        MyViewModel.SetSelectedArtist(art);

        await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    }

    private void ViewArtistSongs_Clicked(object sender, EventArgs e)
    {
        var send = ((DXToggleButton)sender);
        var art = send.CommandParameter as ArtistModelView;
        switch (send.IsChecked)
        {
            case true:
                MyViewModel.SetSelectedArtist(art);
                break;
                
            case false:

                break;

            default:
                break;
        }
    }


    private void SearchText_Loaded(object sender, EventArgs e)
    {
      
    }

    private bool _wasKeyboardShowing = false;
    private Android.Views.View _rootView;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // 1. If Handler is null, the view is being disconnected. We MUST clean up the listener to prevent memory leaks.
        if (Handler == null)
        {
            if (_rootView?.ViewTreeObserver?.IsAlive == true)
            {
                _rootView.ViewTreeObserver.GlobalLayout -= OnGlobalLayout;
            }
            _rootView = null;
            return;
        }

        // 2. Setup the listener when the Handler is attached
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        _rootView = activity?.Window?.DecorView?.FindViewById(Android.Resource.Id.Content);

        if (_rootView?.ViewTreeObserver != null)
        {
            _rootView.ViewTreeObserver.GlobalLayout += OnGlobalLayout;
        }
    }

    private void OnGlobalLayout(object? sender, EventArgs e)
    {
        if (_rootView == null) return;

        Android.Graphics.Rect rect = new Android.Graphics.Rect();
        _rootView.GetWindowVisibleDisplayFrame(rect);

        // Calculate the difference between total height and visible height
        int? screenHeight = _rootView.RootView?.Height;
        int visibleHeight = rect.Bottom - rect.Top;
        int? heightDiff = screenHeight - visibleHeight;

        // Using 15% of the screen height as a threshold is much safer than 200px 
        // across different device screen densities.
        bool isKeyboardShowing = heightDiff > (screenHeight * 0.15);

        if (!isKeyboardShowing && _wasKeyboardShowing)
        {
            // Keyboard Just Collapsed!
            RxSchedulers.UI.ScheduleTo(() => 
            {

                // Call your portable MAUI method here
                System.Diagnostics.Debug.WriteLine("Keyboard collapsed!");
            });
        }

        // Save state for the next time this event fires
        _wasKeyboardShowing = isKeyboardShowing;
    }

    


    private void SingleSongBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        switch (e.NewValue)
        {
            case BottomSheetState.FullExpanded:
                break;
            case BottomSheetState.HalfExpanded:
                break;
            case BottomSheetState.Hidden:
                return;

            default:
                break;
        }

        var PosInQueue = MyViewModel.PlaybackQueue.IndexOf(MyViewModel.SelectedSong);
        if (PosInQueue == - 1)
            return;

        var currentPos = MyViewModel.PlaybackQueue.IndexOf(MyViewModel.CurrentPlayingSongView);
        var diffInPositions = PosInQueue - currentPos;
        if(diffInPositions > 0)
        {
            //NextInQueueCounter.Text = $""
        }
        else
        {

        }
    }



    private async void DeleteSongBtn_Tap(object sender, HandledEventArgs e)
    {
        await MyViewModel.DeleteFileFromSystem(MyViewModel.SelectedSong);
    }

    private void ShowTQLShortBTMSheet_Loaded(object sender, EventArgs e)
    {
        var nativeView = ShowTQLShortBTMSheet.Handler?.PlatformView as Android.Views.View;
        if (nativeView != null)
        {
            // Unsubscribe first to guarantee we never double-subscribe!
            nativeView.LongClick -= NativeView_LongClick;
            nativeView.LongClick += NativeView_LongClick;
        }
    }

    private void NativeView_LongClick(object? sender, Android.Views.View.LongClickEventArgs e)
    {
        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    }
}