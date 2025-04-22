namespace Dimmer.WinUI.Views;

public partial class AlbumWindow : Window
{
	public AlbumWindow(BaseAlbumViewModel vm)
	{
		InitializeComponent();
        MyViewModel=vm;
        this.Height = 600;
        this.Width = 800;
        BindingContext = vm;
    }

    public BaseAlbumViewModel MyViewModel { get; }



    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        send.BackgroundColor = Colors.DarkSlateBlue;

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
    }

}