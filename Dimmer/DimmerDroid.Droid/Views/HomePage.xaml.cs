
global using View = Microsoft.Maui.Controls.View;
using DevExpress.Maui.Core;
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

    private void ArtistChip_DoubleTap(object sender, HandledEventArgs e)
    {

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

    private void ViewPlaybackQueueBtn_Clicked(object sender, EventArgs e)
    {
        PBQueueBtmSheet.Show();
        
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
        var song = ((View)sender).BindingContext as SongModelView;
        MyViewModel.SelectedSong=song;
        await Shell.Current.GoToAsync(nameof(DetailsOverview), true);    
    }

    private void NPBottomBar_PanUpdated(object sender, PanUpdatedEventArgs e)
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
            // Handle swipe down action
            Debug.WriteLine("Swiped Down");
        }
        else if (IsSwipedLeft)
        {
            // Handle swipe left action
            Debug.WriteLine("Swiped Left");
        }
        else if (IsSwipedRight)
        {
            // Handle swipe right action
            Debug.WriteLine("Swiped Right");
        }

    }

    private void NPBtmSheet_StateChanged(object sender, DevExpress.Maui.Core.ValueChangedEventArgs<BottomSheetState> e)
    {

    }

    private void CurrentPlayingTitleChip_Tap(object sender, HandledEventArgs e)
    {

        NPBtmSheet.Show();
        NPBtmSheet.State = BottomSheetState.FullExpanded;
    }

    private void CurrentPlayingArtistChip_LongPress(object sender, HandledEventArgs e)
    {

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

        ArtistSongsExpander?.IsExpanded = !ArtistSongsExpander.IsExpanded;

    }

    private void DXToggleButton_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

    }

    private async void ToggleFavBtn_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
       await MyViewModel.AddFavoriteRatingToSongAsync(MyViewModel.SelectedSong!);
    }

    private void MoreBtn_Tap(object sender, HandledEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        MyViewModel.SelectedSong = song;
        SingleSongBtmSheet.Show();
    }

    private async void DeleteSongBtn_Tap(object sender, HandledEventArgs e)

    { 
        await MyViewModel.DeleteSongs(new List<SongModelView>(){MyViewModel.SelectedSong! });
    }

    private void CurrentPlayingTitleChip_LongPress(object sender, HandledEventArgs e)
    {
        var songHandle = SongsCV.FindItemHandle(MyViewModel.CurrentPlayingSongView);

        SongsCV.ScrollTo(songHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    }

    private void SongTitle_Loaded(object sender, EventArgs e)
    {
        SongTitle.Text = MyViewModel.CurrentPlayingSongView?.Title is null ? "" : $"❤️{ MyViewModel.CurrentPlayingSongView?.Title}"
        ;
    }

    private void PlayNextBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.AddToNext(new List<SongModelView>() { MyViewModel.SelectedSong! });
    }

    private void AlbumChip_Tap(object sender, HandledEventArgs e)
    {
        
    }
    DXExpander? ArtistSongsExpander;
    private void ArtistSongsExpander_Loaded(object sender, EventArgs e)
    {
        ArtistSongsExpander = (DXExpander)sender;
    }
    private void ArtistSongsExpander_Unloaded(object sender, EventArgs e)
    {
        ArtistSongsExpander = null;
    }
}