using Syncfusion.Maui.Toolkit.Carousel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;

public partial class ArtistWindow : Window
{
    public ArtistWindow(HomeViewModel ViewModel)
    {
        InitializeComponent();
    
        this.Height = 600;
        this.Width = 800;
        BindingContext = ViewModel;

        MyViewModel=ViewModel;
    }

    public HomeViewModel MyViewModel { get; }


    private static void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;

    }

    private void ArtistSongsColView_Loaded(object sender, EventArgs e)
    {
        ArtistSongsColView.ItemsSource = MyViewModel.SelectedArtistSongs;
    }
    private async void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;
        var songs = ArtistSongsColView.ItemsSource as ObservableCollection<SongModelView>;

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
            await MyViewModel.PlaySong(song, CurrentPage.AllArtistsPage, songs);
        }

    }

    public void SetTitle(SongModelView song)
    {
        this.Title = $"Artist : {song.AlbumName} by {song.ArtistName}";
        
    }

    private void ArtistsAlbumsGroup_ChipClicked(object sender, EventArgs e)
    {
        SfChip send = (sender as SfChip)!;

        var album = send.BindingContext as AlbumModelView;
        var song = MyViewModel.SelectedArtistSongs?[0]!;
        MyViewModel.OpenAlbumPage(song, album);
        PlatUtils.OpenAlbumWindow(song);
    }

    private void SfChip_Clicked(object sender, EventArgs e)
    {

    }

    private void ArtistsAlbumsGroup_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }

    private void ArtistsAlbumsGroup_TouchUp(object sender, EventArgs e)
    {

    }

    private void ArtistsAlbumsGroup_SwipeEnded(object sender, EventArgs e)
    {
        

    }

    private void ArtistsAlbumsGroup_SelectionChanged_1(object sender, Syncfusion.Maui.Toolkit.Carousel.SelectionChangedEventArgs e)
    {

        var carousel = (SfCarousel)sender;
        var item = carousel.SelectedIndex;
        var album = carousel.ItemsSource as ObservableCollection<AlbumModelView>;
        var selectedAlbum = album?[item];
        var albumDb = BaseAppFlow.MasterAlbumList?.FirstOrDefault(x => x.Id == selectedAlbum?.Id);
        var songDb = albumDb?.Songs;

        var map = IPlatformApplication.Current?.Services.GetService<IMapper>();

        var songss = map?.Map<ObservableCollection<SongModelView>>(songDb);
        if (songss != null)
        {
            MyViewModel.SelectedArtistSongs = songss;
            Debug.WriteLine(album?.Count);
            ArtistSongsColView.ItemsSource = songss;
        }

    }
}