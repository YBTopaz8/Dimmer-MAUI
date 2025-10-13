using DevExpress.Maui.Controls;

namespace Dimmer.Views.CustomViewsParts;

public partial class NowPlayingbtmsheet : BottomSheet
{
    public NowPlayingbtmsheet()
    {
        InitializeComponent();

        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>();
        this.BindingContext =vm;
        this.MyViewModel =vm;
    }

    public BaseViewModelAnd MyViewModel { get; set; }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        var send = (DXSlider)sender;


        MyViewModel.SeekTrackPosition(ProgressSlider.Value);
    }

    private async void NowPlayingBtmSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        if (e.NewValue != BottomSheetState.FullExpanded)
        {
            await this.AnimateFadeOutBack(600);
        }
        //if (MyViewModel.IsPlaying)
        //{
        //    SongPicture.StartHeartbeat();
        //}
    }

    private async void ArtistChip_Tap(object sender, HandledEventArgs e)
    {
        var send = (Chip)sender;

        var song = send.TapCommandParameter as SongModelView;
        if (song is null)
        {
            return;
        }
        await MyViewModel.SelectedArtistAndNavtoPage(song);

        await Shell.Current.GoToAsync(nameof(ArtistsPage), true);

        await this.AnimateFadeOutBack(600);
        await this.CloseAsync();
    }

    private void NowPlayingBtmSheet_Unloaded(object sender, EventArgs e)
    {
        //SongPicture.StopHeartbeat();

    }

    private async void SongTitleChip_Tap(object sender, HandledEventArgs e)
    {
        await CloseAsync();

        MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
        await this.AnimateFadeOutBack(600);

        await Shell.Current.GoToAsync(nameof(SingleSongPage));
    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {
        BottomExpander.IsExpanded = !BottomExpander.IsExpanded;


    }
    private void CloseNowPlayingQueue_Tap(object sender, HandledEventArgs e)
    {

        Debug.WriteLine(this.Parent.GetType());
        BottomExpanderTwo.IsExpanded = !BottomExpanderTwo.IsExpanded;
        this.AllowDismiss = !BottomExpanderTwo.IsExpanded;
    }

    private void SongTitleChip_LongPress(object sender, HandledEventArgs e)
    {
        var parent = this.Parent.Parent as HomePage;
        if (parent.GetType() == typeof(HomePage))
        {
            
        }
    }

    private void NowPlayingBtmSheet_Loaded(object sender, EventArgs e)
    {
        
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ToggleNowPlayingUI?.Invoke(this, EventArgs.Empty);
    }
    public event EventHandler? ToggleNowPlayingUI;
}