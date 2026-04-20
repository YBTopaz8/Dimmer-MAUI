using DevExpress.Maui.Core;

namespace Dimmer.Views.Artist;

public partial class AllArtistsPage : ContentPage
{
	public AllArtistsPage(BaseViewModelAnd myViewModel)
	{
		InitializeComponent();
		MyViewModel = myViewModel;
		BindingContext = myViewModel;
	}
    public BaseViewModelAnd MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

	
    }



    private async void NavigateToArtistDetailsButton_Tapped(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        
        DXButton send= (DXButton)sender;
        var artist = send.CommandParameter as ArtistModelView;

        MyViewModel.SetSelectedArtist(artist);
        await Shell.Current.GoToAsync(nameof(ArtistPage));


    }

    private void ArtistSongsCountChip_Tap(object sender, HandledEventArgs e)
    {
        var artist = ((Chip)sender).BindingContext as ArtistModelView;
        if(artist != null)
        {
            MyViewModel.SetSelectedArtist(artist);
            ArtistSongsCV.ItemsSource = artist.SongsByArtist;
            ArtistSongsBtmSheet.Show();
        }
    }

    private async void ArtistSongsCV_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var song = e.Item as SongModelView;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);

    }

    private void AddSongToNextInPlaylist_Tap(object sender, DXTapEventArgs e)
    {
        DXButton send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if(song is null)
            return;
        MyViewModel.AddToNext(new List<SongModelView> { song });
    }
}