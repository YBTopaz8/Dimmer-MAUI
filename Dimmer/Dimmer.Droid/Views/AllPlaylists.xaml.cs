using AndroidX.Lifecycle;

namespace Dimmer.Views;

public partial class AllPlaylists : ContentPage
{
	public AllPlaylists(BaseViewModelAnd vm)
	{
		InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }
    BaseViewModelAnd MyViewModel { get; }

    private void GlobalColView_PointerPressed(object sender, PointerEventArgs e)
    {

    }

    private void ViewSongDetails_Clicked(object sender, EventArgs e)
    {

    }

    private void OnAddQuickNoteClicked(object sender, EventArgs e)
    {

    }

    private void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {

    }

    private void QuickSearchAlbum_Clicked(object sender, EventArgs e)
    {

    }

    private void ViewGenreMFI_Clicked(object sender, EventArgs e)
    {

    }

    private void OnLabelClicked(object sender, EventArgs e)
    {

    }

    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(null);
    }

    private void PlaySongGestRec_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {

    }
}