using Android.App.AppSearch;
using Dimmer.Views.Settings;
using DynamicData.Binding;
using View = Microsoft.Maui.Controls.View;

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

    private async void OpenFolderScannerBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
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
}