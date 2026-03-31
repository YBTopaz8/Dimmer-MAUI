
global using View = Microsoft.Maui.Controls.View;
using Android.Views.InputMethods;
using DevExpress.Maui.CollectionView;
using DevExpress.Maui.Core;
using DevExpress.Utils;
using Dimmer.DimmerSearch;
using Dimmer.Views.Settings;
using DynamicData.Binding;

namespace Dimmer.Views;

public partial class HomePage : ContentPage
{
	public HomePage(BaseViewModelAnd viewModelAnd)
	{
		InitializeComponent();
		BindingContext = viewModelAnd;
		MyViewModel = viewModelAnd;
	}

    BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

		Debug.WriteLine("HomePage OnAppearing" + MyViewModel.AppTitle +" "+BaseViewModel.CurrentAppStage);
		Debug.WriteLine("HomePage OnAppearing" + MyViewModel.SearchResults.Count);
    }


    private void PlayButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void MiddleGridSection_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.PlaySongAsync(song,CurrentPage.HomePage,MyViewModel.SearchResults);
    }

    private async void TitleChip_Tap(object sender, HandledEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);

    }

    private void ArtistChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private async void ArtistChip_DoubleTap(object sender, HandledEventArgs e)
    {

        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        MyViewModel.SetSelectedArtist(song.Artist);

        await Shell.Current.GoToAsync(nameof(ArtistPage), true);

    }

    private void ArtistChip_LongPress(object sender, HandledEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        //var artInDb = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(song.Id)?.Artist.ToArtistModelView();
        //if(artInDb == null)
        //    return;
        //MyViewModel.SetSelectedArtist(artInDb);
        MyViewModel.SelectedSong= song;
        ArtistsChoiceBtmSheet.Show();
    }

    private void PlaybackQueueBtmSheet_Loaded(object sender, EventArgs e)
    {
        
    }


    private void PBQueueBtmSheet_StateChanged(object sender, DevExpress.Maui.Core.ValueChangedEventArgs<BottomSheetState> e)
    {
        
    }

    private void SearchBar_TextChanged(object sender, EventArgs e)
    {
        string? txt = SearchBarTextEdit.Text;
        if(txt is not null)
        {
            MyViewModel.SearchToTQL(txt);
        }
    }

    private void MenuBtn_Tap(object sender, HandledEventArgs e)
    {
        Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        
    }

    private void SearchBar_Unloaded(object sender, EventArgs e)
    {
        MyViewModel.ClearSubscriptionToSearchBar();
    }

    private void SearchBar_Loaded(object sender, EventArgs e)
    {
       
        

        MyViewModel.SubscribeToPlayCount(SearchBarTextEdit);

     

    }

    private async void CurrentPlayingCoverTapGesture_Tapped(object sender, TappedEventArgs e)
    {

        MyViewModel.SelectedSong=MyViewModel.CurrentPlayingSongView;

        if(Shell.Current.CurrentPage.GetType() != typeof(DetailsOverview))
            await Shell.Current.GoToAsync(nameof(DetailsOverview), true);    
    }

    private async void NPBottomBar_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        bool IsSwipedUp = e.TotalY < -100; // Adjust the threshold as needed
        bool IsSwipedDown = e.TotalY > 100; // Adjust the threshold as needed
        bool IsSwipedLeft = e.TotalX < -100; // Adjust the threshold as needed
        bool IsSwipedRight = e.TotalX > 100; // Adjust the threshold as needed

        if (IsSwipedUp)
        {
            NPBtmSheet.Show();
            // Handle swipe up action
            Debug.WriteLine("Swiped Up");
        }
        else if (IsSwipedDown)
        {
            //SearchBarTextEdit.Focus();
     
        InputMethodManager? imm = (InputMethodManager?)MainApplication.Context.GetSystemService(Activity.InputMethodService);
            var view = SearchBarTextEdit.Handler?.PlatformView as Android.Views.View;
            imm?.ShowSoftInput(view, ShowFlags.Implicit);
    

    // Handle swipe down action
    Debug.WriteLine("Swiped Down");
        }
        else if (IsSwipedLeft)
        {
            await MyViewModel.PreviousTrackAsync();
            // Handle swipe left action
            Debug.WriteLine("Swiped Left");
        }
        else if (IsSwipedRight)
        {
            await MyViewModel.NextTrackAsync();
            // Handle swipe right action
            Debug.WriteLine("Swiped Right");
        }

    }

    private void NPBtmSheet_StateChanged(object sender, DevExpress.Maui.Core.ValueChangedEventArgs<BottomSheetState> e)
    {

    }



    private void CurrentPlayingArtistChip_LongPress(object sender, HandledEventArgs e)
    {

        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    }

    private async void SettingsBtn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage), true);
    }

    private void ArtistChip_Tap_1(object sender, HandledEventArgs e)
    {

    }

    private async void ArtistBtmChip_Tap(object sender, HandledEventArgs e)
    {
        var artChip = (DevExpress.Maui.Editors.Chip)sender;
        var art = artChip.LongPressCommandParameter as ArtistModelView;

        MyViewModel.SetSelectedArtist(art);

        await Shell.Current.GoToAsync(nameof(ArtistPage), true);    
    }

    private void ArtistBtmChip_LongPress(object sender, HandledEventArgs e)
    {
        var artChip = (DevExpress.Maui.Editors.Chip)sender;
        var art = artChip.LongPressCommandParameter as ArtistModelView;

        if(art != null && !string.IsNullOrEmpty(art.Name))
        {
            MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.ByArtist(art.Name));
        }
    }

    private void ArtistGrid_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void SearchArtistBtnTQL_Tap(object sender, HandledEventArgs e)
    {

        var artChip = (DevExpress.Maui.Editors.Chip)sender;
        var art = artChip.LongPressCommandParameter as string;

        MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.ByArtist(art));
    }

    private async void ViewArtistBtn_Tap(object sender, HandledEventArgs e)
    {
        var artChip = (DevExpress.Maui.Editors.Chip)sender;
        var art = artChip.LongPressCommandParameter as ArtistModelView;

        MyViewModel.SetSelectedArtist(art);

        await ArtistsChoiceBtmSheet.CloseAsync();
        await Shell.Current.GoToAsync(nameof(ArtistPage), true);
    }

    private void DXToggleButton_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

    }

    private async void ToggleFavBtn_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
       await MyViewModel.AddFavoriteRatingToSongAsync(MyViewModel.SelectedSong!);
    }


    private async void DeleteSongBtn_Tap(object sender, HandledEventArgs e)

    { 
        await MyViewModel.DeleteSongs(new List<SongModelView>(){MyViewModel.SelectedSong! });
    }

    private void CurrentPlayingTitleChip_LongPress(object sender, HandledEventArgs e)
    {
    }

    private void SongTitle_Loaded(object sender, EventArgs e)
    {
        SongTitle.Text = MyViewModel.SelectedSong?.Title is null ? "" : $"❤️{ MyViewModel.SelectedSong?.Title}"
        ;
    }

    private void PlayNextBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddToNext(new List<SongModelView>() { MyViewModel.SelectedSong! });
    }

    private void AlbumChip_Tap(object sender, HandledEventArgs e)
    {
        
    }

    private void ArtistsSongs_Loaded(object sender, EventArgs e)
    {
        DXCollectionView cv = (DXCollectionView)sender;
        cv.ItemsSource = MyViewModel.SelectedArtist?.SongsByArtist;


    }

    private void DXButton_Loaded(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        MyViewModel.WhenPropertyChange(MyViewModel.CurrentTqlQuery, v => (MyViewModel.CurrentTqlQuery))
            .Subscribe(
                e =>
                {
                   if(string.IsNullOrEmpty(e) || string.IsNullOrWhiteSpace(e))
                    {
                       send.IsVisible = false;
                       return;
                    }
                    send.IsVisible = true;

                });

    }

    private void NPMiddleGridSection_Tapped(object sender, TappedEventArgs e)
    {
        CurrentPlayingTitleChip_Tap(sender, new DXTapEventArgs(new Point()));
    }

    private void SwipeGestureRecog_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var isSwipeUp = e.StatusType == GestureStatus.Running && e.TotalY < -100; // Adjust the threshold as needed
    }

    private void ViewPlaybackQueueBtn_Clicked(object sender, HandledEventArgs e)
    {
        NPBtmSheet.ShowAndOpenPlaybackQueue();
    }

    private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {

    }

    private void TouchBehavior_LongPressCompleted(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
    {

        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    }

    private void SongsCV_PullToRefresh(object sender, EventArgs e)
    {

    }

    private void CurrentPlayingTitleChip_Tap(object sender, DXTapEventArgs e)
    {

        NPBtmSheet.Show();
        NPBtmSheet.State = BottomSheetState.FullExpanded;
    }

    private void MoreBtn_Tap(object sender, DXTapEventArgs e)
    {

        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        MyViewModel.SelectedSong = song;
        SingleSongBtmSheet.Show();
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext as SongModelView;
        MyViewModel.SelectedSong = song;

        if (Shell.Current.CurrentPage.GetType() != typeof(DetailsOverview))
            await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
    }

    private void BtmbarImg_Loaded(object sender, EventArgs e)
    {
        DXImage send = (DXImage)sender;
        var pltView = send.Handler?.PlatformView as Android.Views.View;
        pltView?.LongClickable = true;
        pltView?.LongClick += (s,  args) =>
        {
            var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);
            SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
        };
    }
}