using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class FeedbackDetailPage : Page
{
    public FeedbackDetailViewModel ViewModel { get; }

    public FeedbackDetailPage()
    {
        this.InitializeComponent();
        ViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackDetailViewModel>();
        DataContext = ViewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is string issueId)
        {
            var queryParams = new Dictionary<string, object>
            {
                { "IssueId", issueId }
            };
            ViewModel.ApplyQueryAttributes(queryParams);
        }
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void UpvoteButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ToggleUpvoteCommand.ExecuteAsync(null);
    }

    private async void AddCommentButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddCommentCommand.ExecuteAsync(null);
    }

    private async void DeleteCommentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is FeedbackComment comment)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Comment",
                Content = "Are you sure you want to delete this comment?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteCommentCommand.ExecuteAsync(comment);
            }
        }
    }

    private async void DeleteIssueButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete Issue",
            Content = "Are you sure you want to delete this issue? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteIssueCommand.ExecuteAsync(null);
        }
    }

    private async void OpenGitHubButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.OpenGitHubIssueCommand.ExecuteAsync(null);
    }

    private async void NotificationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        await ViewModel.UpdateNotificationPreferencesCommand.ExecuteAsync(null);
    }
}
