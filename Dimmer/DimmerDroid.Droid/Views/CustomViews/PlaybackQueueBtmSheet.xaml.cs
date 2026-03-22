global using DevExpress.Maui.Controls;

namespace Dimmer.Views.CustomViews;

public partial class PlaybackQueueBtmSheet : BottomSheet
{
	public PlaybackQueueBtmSheet()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()!;
	BindingContext = vm;

        MyViewModel = vm;
	}
    BaseViewModelAnd MyViewModel { get;}

    private async void MiddleGridSection_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);
    }

    private void TitleChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistChip_Tap(object sender, HandledEventArgs e)
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
        MyViewModel.SelectedSong = song;
        ArtistsChoiceBtmSheet.Show();
    }

}