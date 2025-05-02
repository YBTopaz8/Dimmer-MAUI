namespace Dimmer.WinUI.Views;

public partial class ArtistWindow : Window
{
    public ArtistWindow()
    {
        InitializeComponent();
    
        this.Height = 600;
        this.Width = 800;
        //BindingContext = vm;


    }

    public BaseAlbumViewModel MyViewModel { get; }


    private static void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;

    }

    private void AlbumSongsColView_Loaded(object sender, EventArgs e)
    {
        AlbumSongsColView.ItemsSource = MyViewModel.SelectedAlbumsSongs;
    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
            MyViewModel.PlaySong(song);
        }

    }

    public void SetTitle(SongModelView song)
    {
        this.Title = $"{song.AlbumName} by {song.ArtistName}";
        MyViewModel.AlbumsMgtFlow.GetAlbumsBySongId(song.LocalDeviceId!);
    }

}