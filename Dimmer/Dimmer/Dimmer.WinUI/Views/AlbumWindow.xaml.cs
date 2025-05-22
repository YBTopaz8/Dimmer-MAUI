namespace Dimmer.WinUI.Views;

public partial class AlbumWindow : Window
{
	public AlbumWindow(HomeViewModel vm)
	{
		InitializeComponent();
        this.Height = 600;
        this.Width = 800;
        BindingContext = vm;
        MyViewModel=vm;

    }

    public HomeViewModel MyViewModel { get; }

    protected override void OnCreated()
    {
        base.OnCreated();

        MyViewModel.LoadAlbum();
    }

    private static void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;

    }

    private void AlbumSongsColView_Loaded(object sender, EventArgs e)
    {
        AlbumSongsColView.ItemsSource = MyViewModel.SelectedAlbumsSongs;
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