using Syncfusion.Maui.Toolkit.Carousel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Views;

public partial class AlbumWindow : Window
{
	public AlbumWindow(HomeViewModel vm, IMapper mapper)
	{
		InitializeComponent();
        this.Height = 600;
        this.Width = 800;
        BindingContext = vm;
        MyViewModel=vm;
        Mapper=mapper;
    }

    public HomeViewModel MyViewModel { get; }
    public IMapper Mapper { get; }

    protected override void OnCreated()
    {
        base.OnCreated();
    }

    private static void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;

    }

    private void AlbumSongsColView_Loaded(object sender, EventArgs e)
    {
        AlbumSongsColView.ItemsSource =  Mapper.Map<ObservableCollection<SongModelView>>(MyViewModel.SelectedAlbumsSongs);
    }
    private async void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;
        var songs = AlbumSongsColView.ItemsSource as ObservableCollection<SongModelView>;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
            await MyViewModel.PlaySong(song, CurrentPage.AllAlbumsPage,songs);
        }

    }

    public void SetTitle(SongModelView song)
    {
        this.Title = $"Album: {song.AlbumName} by {song.ArtistName}";
        
    }

    //private async void AlbumArtistsGroup_ChipClicked(object sender, EventArgs e)
    //{
       
    //}

   

    private void AlbumArtistsGroup_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }

    private void AlbumsArtistsGroup_TouchUp(object sender, EventArgs e)
    {

    }
    private void ArtistsAlbumsGroup_SwipeEnded(object sender, EventArgs e)
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
        }

        //ArtistSongsColView.ItemsSource = MyViewModel.SelectedArtistSongs;
    }

    private void AlbumArtistsGroup_SwipeEnded(object sender, EventArgs e)
    {

        var carousel = (SfCarousel)sender;
        var item = carousel.SelectedIndex;
        var artist = carousel.ItemsSource as ObservableCollection<ArtistModelView>;
        var selectedAlbum = artist?[item];
        var artistDb = BaseAppFlow.MasterArtistList?.FirstOrDefault(x => x.Id == selectedAlbum?.Id);
        var songDb = artistDb?.Songs;

        var map = IPlatformApplication.Current?.Services.GetService<IMapper>();

        var songss = map?.Map<ObservableCollection<SongModelView>>(songDb);
        if (songss != null)
        {
            MyViewModel.SelectedArtistSongs = songss;
            AlbumSongsColView.ItemsSource =songss;
        }

        
    }
    private async void AlbumArtistsGroup_ChipClicked(object sender, EventArgs e)
    {
        var send = sender as SfChip;

        Debug.WriteLine(send.GetType());
        Debug.WriteLine(sender.GetType());
        var artistDb = BaseAppFlow.MasterArtistList.First(x => x.Name==send.Text);
        //var artist = send?.CommandParameter as ArtistModelView;
        //var song = MyViewModel.SelectedAlbumsSongs[0];
        //if (artist != null)
        //{
        //    await MyViewModel.OpenArtistPage(song, artist);
        //    PlatUtils.OpenArtistWindow(song);
        //}

    }
    private void AlbumArtistsGroup_ChipClicked_1(object sender, EventArgs e)
    {

    }
}