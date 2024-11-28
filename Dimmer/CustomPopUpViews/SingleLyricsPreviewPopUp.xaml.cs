namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SingleLyricsPreviewPopUp : Popup
{
	public SingleLyricsPreviewPopUp(Content cont,bool isPlain, HomePageVM homePageVM)
	{
		InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;

        ArtistNameLabel.Text = cont.ArtistName;
        SongTitleLabel.Text = " "+ cont.Name;
        if (isPlain )
        {
            LyricsView.Text = cont.PlainLyrics;
        }
        else
        {
            LyricsView.Text = cont.SyncedLyrics;
        }
    }

    public HomePageVM HomePageVM { get; }

    private void CloseButton_Clicked(object sender, EventArgs e) => Close(false);
    private void OkButton_Clicked(object sender, EventArgs e) => Close(true);
}