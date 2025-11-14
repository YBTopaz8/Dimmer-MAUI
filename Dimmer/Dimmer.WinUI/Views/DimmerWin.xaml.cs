using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml;

using Windows.Graphics;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerWin : Window
{
    private BaseViewModelWin baseViewModelWin;
    private AppUtil appUtil;
    IWinUIWindowMgrService? WinUIWindowsMgr;
    public DimmerWin()
    {
        InitializeComponent();
        MyViewModel= IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        WinUIWindowsMgr= IPlatformApplication.Current?.Services.GetService<IWinUIWindowMgrService>();
        
        
    }

    
    public async void NavigateToPage(Type pageType)
    {
        if (MyViewModel is not null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                ContentFrame.Navigate(pageType, MyViewModel);

            });
        }
    }
    public BaseViewModelWin? MyViewModel { get; internal set; }
    private void DimmerWindowClosed(object sender, WindowEventArgs args)
    {
        WinUIWindowsMgr?.UntrackWindow(this);
        this.Closed -= DimmerWindowClosed; 

    }
    public void LoadWindowAndPassVM(BaseViewModelWin baseViewModelWin, AppUtil appUtil)
    {
        this.baseViewModelWin = baseViewModelWin;
        this.appUtil = appUtil;
        
    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }
            if (MyViewModel is null)
                return;


        if (MyViewModel.IsLastFMNeedsToConfirm)
        {
            ContentDialog lastFMConfirmDialog = new ContentDialog
            {
                Title = "LAST FM Confirm",
                Content = "Is Authorization done?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = this.ContentFrame.XamlRoot

            };
            var isLastFMAuthorized = await lastFMConfirmDialog.ShowAsync() == ContentDialogResult.Primary;
            
            if (isLastFMAuthorized)
            {
                await MyViewModel.CompleteLastFMLoginAsync();
            }
            else
            {
                MyViewModel.IsLastFMNeedsToConfirm = false;
                ContentDialog cancelledDialog = new ContentDialog
                {
                    Title = "Action Cancelled",
                    Content = "Last FM Authorization Cancelled",
                    CloseButtonText = "OK",
                    XamlRoot = this.ContentFrame.XamlRoot
                };
                

            }
        }

        MyViewModel.CurrentWinUIPage = this;
        
        
        if (MyViewModel.IsLastFMNeedsToConfirm)
        {
            bool isLastFMAuthorized = await Shell.Current.DisplayAlert("LAST FM Confirm", "Is Authorization done?", "Yes", "No");
            if (isLastFMAuthorized)
            {
                await MyViewModel.CompleteLastFMLoginAsync();
            }
            else
            {
                MyViewModel.IsLastFMNeedsToConfirm = false;
                await Shell.Current.DisplayAlert("Action Cancelled", "Last FM Authorization Cancelled", "OK");

            }
        }
    }
}
