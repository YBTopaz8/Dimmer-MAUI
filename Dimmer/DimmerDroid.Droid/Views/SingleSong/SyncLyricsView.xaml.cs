namespace Dimmer.Views.SingleSong;

public partial class SyncLyricsView : ContentView
{
	public SyncLyricsView(BaseViewModelAnd viewModelAnd, LastFMViewModel lastFMVM)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
        MyLastFMViewModel = lastFMVM;

    }

    BaseViewModelAnd MyViewModel { get; }
    LastFMViewModel MyLastFMViewModel { get; }

}