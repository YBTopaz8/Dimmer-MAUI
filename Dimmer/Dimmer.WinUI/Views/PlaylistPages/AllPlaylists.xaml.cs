using Microsoft.Maui.Platform;

using Syncfusion.Maui.Toolkit.NavigationDrawer;

namespace Dimmer.WinUI.Views.PlaylistPages;

public partial class AllPlaylists : ContentPage
{
	public AllPlaylists(BaseViewModelWin baseViewModel)
	{
		InitializeComponent();
		BindingContext = baseViewModel;
		ViewModel = baseViewModel;
    }

	public BaseViewModelWin ViewModel{ get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ViewModel.LoadLastHundredPlayEventsCommand.Execute(null);
    }

    private void collectionView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        
    }


    private void navigationDrawer_DrawerOpening(object sender, CancelEventArgs e)
    {

    }

    private void navigationDrawer_DrawerClosing(object sender, CancelEventArgs e)
    {

    }

    private void navigationDrawer_DrawerClosed(object sender, EventArgs e)
    {

    }

    private void navigationDrawer_DrawerOpened(object sender, EventArgs e)
    {

    }

    private void navigationDrawer_DrawerToggled(object sender, Syncfusion.Maui.Toolkit.NavigationDrawer.ToggledEventArgs e)
    {

    }

    private void GlobalColView_PointerPressed(object sender, PointerEventArgs e)
    {

    }

    private void ViewSongDetails_Clicked(object sender, EventArgs e)
    {

    }

    private void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

    }

    private void OnAddQuickNoteClicked(object sender, EventArgs e)
    {

    }

    private void QuickSearchArtist_Clicked(object sender, EventArgs e)
    {

    }

    private void ViewGenreMFI_Clicked(object sender, EventArgs e)
    {

    }

    private void QuickSearchAlbum_Clicked(object sender, EventArgs e)
    {

    }

    private void OnLabelClicked(object sender, EventArgs e)
    {

    }

    private void PlaySongGestRec_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {

    }
}