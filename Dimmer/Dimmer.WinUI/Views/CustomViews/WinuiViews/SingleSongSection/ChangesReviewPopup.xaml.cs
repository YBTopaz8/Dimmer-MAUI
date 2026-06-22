using Dimmer.WinUI.ViewModel.SingleSongVMSection;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews.SingleSongSection;

public sealed partial class ChangesReviewPopup : UserControl
{
    public EditSongViewModel ViewModel { get; set; }

    public ChangesReviewPopup()
    {
        this.InitializeComponent();

        // Set default buttons
        //this.PrimaryButtonText = "Save Selected";
        //this.SecondaryButtonText = "Cancel";
        //this.CloseButtonText = "Close";

        //// Handle button clicks
        //this.PrimaryButtonClick += OnPrimaryButtonClick;
        //this.SecondaryButtonClick += OnSecondaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Save is handled in the calling page
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Cancel/discard is handled in the calling page
    }

    private void AcceptAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AcceptAllChanges();
    }

    private void RejectAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RejectAllChanges();
    }

    private void SaveSelected_Click(object sender, RoutedEventArgs e)
    {
        //this.Hide();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        //this.Hide();
    }
}