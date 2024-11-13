using Microsoft.Maui.ApplicationModel;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SettingsPageD : ContentPage
{
	public SettingsPageD(HomePageVM vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    bool IsUserInLastFM;
    protected override void OnAppearing()
    {
        base.OnAppearing();

      
    }
    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void ShowHidePreferredFoldersExpander_Tapped(object sender, TappedEventArgs e)
    {
        ShowHidePreferredFoldersExpander.IsExpanded = !ShowHidePreferredFoldersExpander.IsExpanded;
    }
}
