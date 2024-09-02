namespace Dimmer_MAUI.Views.CustomViews;

public partial class EditableSongTagsView : ContentView
{
	public HomePageVM? HomePageVM {  get; set; }
	public EditableSongTagsView()
	{		
		InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        BindingContext = IPlatformApplication.Current.Services.GetService<HomePageVM>();
	}

    private void StampLyricLine_Clicked(object sender, EventArgs e)
    {
		var s = (ImageButton)sender;
		var se = (LyricPhraseModel)s.BindingContext;
		HomePageVM!.CaptureTimestampCommand.Execute(se);
    }

    private void DeleteLyricLine_Clicked(object sender, EventArgs e)
    {
        var s = (ImageButton)sender;
        var se = (LyricPhraseModel)s.BindingContext;
		HomePageVM!.DeleteLyricLineCommand.Execute(se);
    }

    private void StartSyncingBtn_Clicked(object sender, EventArgs e)
    {
        UnSyncLyricsView.IsVisible = !UnSyncLyricsView.IsVisible;
        SyncingLyricView.IsVisible = !SyncingLyricView.IsVisible;
        HomePageVM!.IsOnLyricsSyncMode = SyncingLyricView.IsVisible;
        if (SyncedLyricsCV.IsVisible)
        {
            StartSyncingBtn.Text = "Stop Syncing";
            return;
        }
        StartSyncingBtn.Text = "Start Syncing";
    }
}