

namespace Dimmer.WinUI.Views;

public partial class HomePage : ContentPage
{
    public HomeViewModel MyViewModel { get; internal set; }
    public HomePage(HomeViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

        this.Loaded += SongsColView_Loaded;
    }


    private void SongsColView_Loaded(object? sender, EventArgs e)
    {
       
        try
        {
            //SongsColView.ItemsSource = MyViewModel.BaseVM.DisplayedSongs;
            MyViewModel.SetCollectionView(SongsColView);
            
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {

        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySongOnDoubleTap(song!);
    }
    private void UserHoverOnSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        MyViewModel.PointerEntered((SongModelView)send.BindingContext, send);
    }
    
    private void UserHoverOutSongInColView(object sender, PointerEventArgs e)
    {
        View send = (View)sender;

        MyViewModel.PointerExited(send);
    }

}