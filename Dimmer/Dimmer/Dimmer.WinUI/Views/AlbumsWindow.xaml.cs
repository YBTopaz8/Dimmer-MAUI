namespace Dimmer.WinUI.Views;

public partial class AlbumsWindow : Window
{
	public AlbumsWindow(BaseAlbumViewModel vm)
	{
		InitializeComponent();
        MyViewModel=vm;
    }

    public BaseAlbumViewModel MyViewModel { get; }
}