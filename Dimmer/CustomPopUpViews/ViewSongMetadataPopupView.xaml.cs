namespace Dimmer_MAUI.CustomPopUpViews;

public partial class ViewSongMetadataPopupView : Popup
{
	public ViewSongMetadataPopupView(HomePageVM homePageVM)
	{
		InitializeComponent();
        BindingContext = homePageVM;
        MyViewModel = homePageVM;
	}
    HomePageVM MyViewModel { get; set; }
    private void Button_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }

    private void SearchSongOn_Clicked(object sender, EventArgs e)
    {
        this.Close();
        ImageButton send = (ImageButton)sender;

        MyViewModel.CntxtMenuSearchCommand.Execute(send.CommandParameter);
    }

    private async void ShareSongToStoryButton_Clicked(object sender, EventArgs e)
    {
        await this.CloseAsync();
        MyViewModel.NavigateToShareStoryPageCommand.Execute(null);
    }
}