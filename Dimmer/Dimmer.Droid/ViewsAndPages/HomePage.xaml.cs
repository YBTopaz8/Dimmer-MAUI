using AndroidX.Lifecycle;

namespace Dimmer.ViewsAndPages;

public partial class HomePage : ContentPage
{
	public HomePage(BaseViewModelAnd baseViewModel)
	{
		InitializeComponent();
		MyViewModel = baseViewModel;
		this.BindingContext = MyViewModel;
    }
	BaseViewModelAnd MyViewModel;

    private void myPageSKAV_Closed(object sender, EventArgs e)
    {

    }

    private void SyncShare_Tap(object sender, HandledEventArgs e)
    {

    }

    private void SearchBy_Focused(object sender, FocusEventArgs e)
    {
        MainViewTabView.SelectedItemIndex = 0;

    }

    private void SearchBy_Loaded(object sender, EventArgs e)
    {

    }

    private void ScrollToCurrSong_Tap(object sender, HandledEventArgs e)
    {
        if (MyViewModel.CurrentPlayingSongView.Title is not null)
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                int itemHandle = MyViewModel.SongsColView.FindItemHandle(MyViewModel.CurrentPlayingSongView);
                MyViewModel.SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
            });



        }

    }

    private void SongsColView_FilteringUIFormShowing(object sender, FilteringUIFormShowingEventArgs e)
    {

    }

    private void ArtistChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void TitleChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ArtistNameChip_Loaded(object sender, EventArgs e)
    {

    }

    private void ArtistNameChip_Unloaded(object sender, EventArgs e)
    {

    }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {

    }

    private void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void BtmBarr_Loaded(object sender, EventArgs e)
    {

    }

    private void BtmBarr_Unloaded(object sender, EventArgs e)
    {

    }

    private void DurationAndSearchChip_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void MoreBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {

    }

    private void OnAddQuickNoteClicked(object sender, EventArgs e)
    {

    }

    private void QuickSearchArtist_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void QuickSearchAlbum_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void ViewGenreMFI_Clicked(object sender, HandledEventArgs e)
    {

    }

    private void OnLabelClicked(object sender, HandledEventArgs e)
    {

    }

    private void SearchBy_TextChanged(object sender, EventArgs e)
    {

    }

    private void SearchBy_Unloaded(object sender, EventArgs e)
    {

    }

    private void Settings_Tap(object sender, HandledEventArgs e)
    {
        Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {

        MyViewModel.SongsColViewNPQ ??= SongsColView;
    }

    private void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {

    }

    private void BtmBarTap_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void PlaySongClicked(object sender, EventArgs e)
    {

    }

    private void ArtistsChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void AlbumFilter_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void MoreIcon_LongPress(object sender, HandledEventArgs e)
    {

    }

    private void ArtistsContextMenu_Opened(object sender, EventArgs e)
    {

    }

    private void ArtistNamesColView_Loaded(object sender, EventArgs e)
    {

    }

    private void ArtistChipName_Tap(object sender, HandledEventArgs e)
    {

    }

    private void AddArtistToTQL_Tap(object sender, HandledEventArgs e)
    {

    }

    private void MoreIcon_Tap(object sender, HandledEventArgs e)
    {

    }

    private void RandomSearch_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ClearSearch_Tap(object sender, HandledEventArgs e)
    {

    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {

        MyViewModel.InitializeAllVMCoreComponents();
    }
}