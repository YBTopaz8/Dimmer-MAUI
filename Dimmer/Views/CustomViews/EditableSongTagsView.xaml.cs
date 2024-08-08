namespace Dimmer_MAUI.Views.CustomViews;

public partial class EditableSongTagsView : ContentView
{
	public HomePageVM homePageVM {  get; set; }
	public EditableSongTagsView()
	{
		
		InitializeComponent();
	}

    private void StampLyricLine_Clicked(object sender, EventArgs e)
    {
		var s = (ImageButton)sender;
		var se = (LyricPhraseModel)s.BindingContext;
		homePageVM.CaptureTimestampCommand.Execute(se);
    }

    private void DeleteLyricLine_Clicked(object sender, EventArgs e)
    {
        var s = (ImageButton)sender;
        var se = (LyricPhraseModel)s.BindingContext;
		homePageVM.DeleteLyricLineCommand.Execute(se);
    }

    private void StartSyncingBtn_Clicked(object sender, EventArgs e)
    {
        UnSyncLyricsView.IsVisible = !UnSyncLyricsView.IsVisible;
        SyncingLyricView.IsVisible = !SyncingLyricView.IsVisible;
        homePageVM.IsOnLyricsSyncMode = SyncingLyricView.IsVisible;
    }
}