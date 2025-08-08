namespace Dimmer.Views.CustomViewsParts;

public partial class MainViewExpander : DXExpander
{
	public MainViewExpander()
	{
		InitializeComponent();
        var vm = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()??throw new NullReferenceException("BaseViewModelAnd is not registered in the service collection.");
        this.BindingContext =vm;

        this.MyViewModel =vm;
    }
    private void SongsColView_FilteringUIFormShowing(object sender, FilteringUIFormShowingEventArgs e)
    {

    }

    public BaseViewModelAnd MyViewModel { get; set; }

    public EventHandler? ViewSongOnly_TouchDownEvent;
    private void ViewSongOnly_TouchDown(object sender, EventArgs e)
    {
        // raise event to notify the parent view to handle the touch down event
        ViewSongOnly_TouchDownEvent?.Invoke(this, e);
    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {

    }

    private void PlaySongClicked(object sender, EventArgs e)
    {

    }

    private void ArtistsChip_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void AlbumFilter_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void MoreIcon_LongPress(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void MoreIcon_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }
}