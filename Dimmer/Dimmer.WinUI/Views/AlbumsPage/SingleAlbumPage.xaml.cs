namespace Dimmer.WinUI.Views.AlbumsPage;

public partial class SingleAlbumPage : ContentPage
{
	public SingleAlbumPage(BaseViewModelWin viewModelWin)
	{
		InitializeComponent();
		BindingContext = viewModelWin;
        BaseViewModel = viewModelWin;
    }

    BaseViewModelWin BaseViewModel { get; }
}