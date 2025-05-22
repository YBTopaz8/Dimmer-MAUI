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

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
            await MyViewModel.PlaySong(song);
        }

    }

    public void SetTitle(SongModelView song)
    {
        this.Title = $"{song.AlbumName} by {song.ArtistName}";
    }

}