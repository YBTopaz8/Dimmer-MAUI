namespace Dimmer_MAUI.CustomPopUpViews;

public partial class QuickSettingsPopupView : Popup
{
	public QuickSettingsPopupView(HomePageVM VM)
	{
		InitializeComponent();
        this.VM=VM;
        BindingContext = VM;
    }

    public HomePageVM VM { get; }

    private void CloseQuickSettingPopup_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }

}