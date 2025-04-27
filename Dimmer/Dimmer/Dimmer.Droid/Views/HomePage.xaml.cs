using DevExpress.Maui.Core;
using DevExpress.Maui.Editors;
using Dimmer.Data.ModelView;

namespace Dimmer.Views;

public partial class HomePage : ContentPage
{

    public HomePageViewModel MyViewModel { get; internal set; }
    public HomePage(HomePageViewModel vm)
	{
		InitializeComponent();
        MyViewModel=vm;

        MyViewModel!.LoadPageViewModel();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentlySelectedPage = Utilities.Enums.CurrentPage.HomePage;

        MyViewModel.SetCollectionView(SongsColView);
        //MyViewModel.SetSongLyricsView(LyricsColView);


    }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        MyViewModel.SeekSongPosition(currPosPer: ProgressSlider.Value);
    }

    private static void CurrentlyPlayingSection_ChipLongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {
        Debug.WriteLine(sender.GetType());
        var send = (Chip)sender;
        var song = send.LongPressCommandParameter;
        Debug.WriteLine(song);
        Debug.WriteLine(song.GetType());

    }


    private void SongsColView_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {
        int itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
        bool isFullyVisible = e.FirstVisibleItemHandle <= itemHandle && itemHandle <= e.LastVisibleItemHandle;

    }
    private void ShowMoreBtn_Clicked(object sender, EventArgs e)
    {
        View s = (View)sender;
        SongModelView song = (SongModelView)s.BindingContext;
        MyViewModel.SetCurrentlyPickedSong(song);
        //SongsMenuPopup.Show();
    }
    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        MyViewModel.PlaySong(e.Item as SongModelView,Utilities.Enums.CurrentPage.HomePage);

    }
}