namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SingleLyricsPreviewPopUp : Popup
{
	public SingleLyricsPreviewPopUp(Content cont,bool isPlain, HomePageVM homePageVM)
	{
		InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;

        ArtistNameLabel.Text = cont.artistName;
        SongTitleLabel.Text = " "+ cont.name;
        if (isPlain )
        {
            LyricsView.Text = cont.plainLyrics;
        }
        else
        {
            LyricsView.Text = cont.syncedLyrics;
        }
    }

    public HomePageVM HomePageVM { get; }

    private void CloseButton_Clicked(object sender, EventArgs e) => Close(false);
    private void OkButton_Clicked(object sender, EventArgs e) => Close(true);
}