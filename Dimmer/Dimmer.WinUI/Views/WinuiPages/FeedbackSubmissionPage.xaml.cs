using Dimmer.DimmerLive.Models;
using Dimmer.ViewModel.DimmerLiveVM;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class FeedbackSubmissionPage : Page
{
    public FeedbackSubmissionViewModel ViewModel { get; }

    public FeedbackSubmissionPage()
    {
        this.InitializeComponent();
        ViewModel = IPlatformApplication.Current!.Services.GetRequiredService<FeedbackSubmissionViewModel>();
        DataContext = ViewModel;
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Title) || string.IsNullOrWhiteSpace(ViewModel.Description))
        {
            var dialog = new ContentDialog
            {
                Title = "Missing Information",
                Content = "Please provide both a title and description for your feedback.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        await ViewModel.SubmitFeedbackCommand.ExecuteAsync(null);
        
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Trigger duplicate check through ViewModel
    }

    private void SimilarIssue_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is FeedbackIssue issue)
        {
            Frame.Navigate(typeof(FeedbackDetailPage), issue.ObjectId);
        }
    }
}
