using Syncfusion.Maui.Toolkit.EffectsView;

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

    public event EventHandler? ViewSongOnlyEvt;
    private void ViewSongOnly_TouchDown(object sender, EventArgs e)
    {
        var send = (SfEffectsView)sender;
        var song = (SongModelView)send.TouchDownCommandParameter;
        if (song is null)
        {
            return;
        }
        MyViewModel.BaseVM.SelectedSong = song;
        // raise event to notify the parent view to handle the touch down event
        ViewSongOnlyEvt?.Invoke(this, e);
    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.SongsColView= SongsColView;
    }

    private async void PlaySongClicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var song = (SongModelView)send.BindingContext;
        await MyViewModel.BaseVM.PlaySong(song);
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