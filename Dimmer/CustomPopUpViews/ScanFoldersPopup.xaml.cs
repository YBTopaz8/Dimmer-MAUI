namespace Dimmer_MAUI.CustomPopUpViews;

public partial class ScanFoldersPopup : Popup
{
	public ScanFoldersPopup(HomePageVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	private void Button_Clicked(object sender, EventArgs e) => Close();
}