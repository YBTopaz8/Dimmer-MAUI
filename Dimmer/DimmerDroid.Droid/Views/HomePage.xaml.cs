using Android.App.AppSearch;
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

    private void SearchBar_UnLoaded(object sender, EventArgs e)
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
}