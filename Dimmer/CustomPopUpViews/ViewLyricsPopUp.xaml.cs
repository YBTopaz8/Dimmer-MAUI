namespace Dimmer_MAUI.CustomPopUpViews;

public partial class ViewLyricsPopUp : Popup
{
	HomePageVM VM { get; set; }
	string btnText { get; set; }
	public ViewLyricsPopUp(Content cont, string buttonText)
	{
		InitializeComponent();
		btnText = buttonText;
        HomePageVM? vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
		VM = vm;
		
	}

	private void Button_Clicked(object sender, EventArgs e)
	{
		//if (lyrics.Title == "View Sync")
		//{

		//}
		//else
		//{

		//}
	}
	
}