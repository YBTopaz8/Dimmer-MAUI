using Microsoft.Maui.ApplicationModel;
namespace Dimmer_MAUI.Views.Mobile;

public partial class SettingsPageM : ContentPage
{
	public SettingsPageM(HomePageVM vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }
}