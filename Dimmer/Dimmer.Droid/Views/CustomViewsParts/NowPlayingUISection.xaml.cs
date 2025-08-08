namespace Dimmer.Views.CustomViewsParts;

public partial class NowPlayingUISection : DXExpander
{
	public NowPlayingUISection()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()??throw new NullReferenceException("BaseViewModelAnd is not registered in the service collection.");
        this.BindingContext =vm;

        this.MyViewModel =vm;
    }
    public BaseViewModelAnd MyViewModel { get; set; }

    private void PingGest_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {

    }

    private void DXCollectionView_SelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {

    }

    private void ArtistChip_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void SongTitleChip_DoubleTap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void SongTitleChip_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void SongTitleChip_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {

    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {

    }

    private void CloseNowPlayingQueue_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }
}