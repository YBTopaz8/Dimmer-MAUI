namespace Dimmer.Views.DimmerCloud;

public partial class RemoteDeviceViewPage : ContentPage
{
	public RemoteDeviceViewPage(SessionManagementViewModel sessVM)
	{
		InitializeComponent();

		MyViewModel = sessVM;
		this.BindingContext = sessVM;
	}

    public SessionManagementViewModel MyViewModel { get; }

    private void GetAllSongs_Click(object sender, EventArgs e)
    {

    }

    private void GetPlayBackQueue_Click(object sender, EventArgs e)
    {

    }

    private async void GetFavs_Click(object sender, EventArgs e)
    {
        DXButton send = (DXButton)sender;
        var comParam = send.AutomationId;

        await MyViewModel.SendDeviceCommand(comParam);
    }
}