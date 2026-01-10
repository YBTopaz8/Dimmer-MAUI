using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class FeedbackBoardPage : Page
{
    public FeedbackBoardViewModel ViewModel { get; }

    public FeedbackBoardPage()
    {
        this.InitializeComponent();
        ViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackBoardViewModel>();
        DataContext = ViewModel;
        
        Loaded += FeedbackBoardPage_Loaded;
    }

    private async void FeedbackBoardPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadIssuesCommand.ExecuteAsync(null);
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void SubmitFeedbackButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsAuthenticated)
        {
            // Show message and open GitHub
            var dialog = new ContentDialog
            {
                Title = "Authentication Required",
                Content = "You need a Dimmer account to submit feedback in-app.\n\nWould you like to open GitHub Issues instead?",
                PrimaryButtonText = "Open GitHub Issues",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.OpenGitHubIssuesCommand.ExecuteAsync(null);
            }
            return;
        }

        Frame.Navigate(typeof(FeedbackSubmissionPage));
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SearchIssuesCommand.ExecuteAsync(null);
    }

    private async void SearchBox_EnterKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await ViewModel.SearchIssuesCommand.ExecuteAsync(null);
    }

    private async void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TypeFilter.SelectedItem is ComboBoxItem item)
        {
            ViewModel.SelectedType = item.Content.ToString() ?? "All";
        }
    }

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilter.SelectedItem is ComboBoxItem item)
        {
            ViewModel.SelectedStatus = item.Content.ToString() ?? "All";
        }
    }

    private async void SortFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortFilter.SelectedItem is ComboBoxItem item)
        {
            ViewModel.SortBy = item.Content.ToString() ?? "recent";
        }
    }

    private void IssueCard_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is FeedbackIssue issue)
        {
            Frame.Navigate(typeof(FeedbackDetailPage), issue.ObjectId);
        }
    }
}
