using Dimmer.Interfaces.Services.Interfaces;

using Syncfusion.Maui.Toolkit.Carousel;

using static Vanara.PInvoke.User32;

namespace Dimmer.WinUI.Views.ArtistsSpace.MAUI;

public partial class ArtistsPage : ContentPage
{
    public ArtistsPage(BaseViewModelWin viewModel)
    {
        InitializeComponent();

        //= IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;
        MyViewModel=viewModel;
        BindingContext=MyViewModel;
    }

    public BaseViewModelWin MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadArtists();
    }
    public void LoadArtists()
    {
        var s = DeviceStaticUtils.SelectedArtistOne;
        MyViewModel.ViewArtistDetails(s);

    }
    private void NavHome_Clicked(object sender, EventArgs e)
    {

    }
    private async void TapGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Grid)sender;
        var song = send.BindingContext as SongModelView;
        await MyViewModel.PlaySongFromListAsync(song, ArtistSongsColView.ItemsSource as IEnumerable<SongModelView>);
    }

}