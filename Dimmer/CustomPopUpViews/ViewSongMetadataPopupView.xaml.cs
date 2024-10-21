namespace Dimmer_MAUI.CustomPopUpViews;

public partial class ViewSongMetadataPopupView : Popup
{
	public ViewSongMetadataPopupView(HomePageVM homePageVM)
	{
		InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;
	}
    HomePageVM HomePageVM { get; set; }
    private void Button_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }

    private void SearchSongOn_Clicked(object sender, EventArgs e)
    {
        this.Close();
        var send = (ImageButton)sender;

        HomePageVM.CntxtMenuSearchCommand.Execute(send.CommandParameter);
    }

    private async void ShareSongToStoryButton_Clicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
        HomePageVM.NavigateToShareStoryPageCommand.Execute(null);
    }
}