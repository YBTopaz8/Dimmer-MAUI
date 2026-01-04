using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Dimmer.Data.Models.LyricsModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using ScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LyricsEditorPage : Page
{
    public LyricsEditorPage()
    {
        InitializeComponent();
    }

    BaseViewModelWin MyViewModel { get; set; }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {

        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(detailedImage) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", detailedImage);
            }
        }
        base.OnNavigatingFrom(e);

    }


    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is SongDetailNavArgs args)
        {
            var vm = args.ExtraParam is null ? args.ViewModel as BaseViewModelWin : args.ExtraParam as BaseViewModelWin;

            if (vm != null)
            {
                MyViewModel = vm;
                this.DataContext = MyViewModel;
                EditLyricsTxt.Loaded += (_, _) =>
                {
            
            DispatcherQueue.TryEnqueue(() =>
            {
                var animation = ConnectedAnimationService.GetForCurrentView()
               .GetAnimation("MoveViewToLyricsPageFromSongDetailPage");



                if (animation != null)
                {
                    var animConf = new Microsoft.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration();



                    animation.Configuration = animConf;

                    animation.TryStart(EditLyricsTxt);
                }
                EditLyricsTxt.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                EditLyricsTxt.Opacity = 1;
                MyViewModel.ReadySearchViewAndProduceSearchText();
            });
                };
                MyViewModel.AutoFillSearchFields();
                MyViewModel.CurrentWinUIPage = this;
            }
        }
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props.IsXButton1Pressed)
        {
            if (Frame.CanGoBack)
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", EditLyricsTxt);


                Frame.GoBack();
            }
        }
    }

    private void BackBtnClick(object sender, RoutedEventArgs e)
    {
        
        if (Frame.CanGoBack)
        {
            // Prepare the animation, linking the key "ForwardConnectedAnimation" to our image
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", EditLyricsTxt);
            Frame.GoBack();
        }
    }
    private LrcLibLyrics? _currentPreviewLyrics;

    private async void ViewLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not LrcLibLyrics lyricData)
            return;

        _currentPreviewLyrics = lyricData;
        LyricsPreviewDialog.DataContext = lyricData;

        // Populate the lyrics content in both tabs
        bool hasSyncedLyrics = !string.IsNullOrWhiteSpace(lyricData.SyncedLyrics);
        bool hasPlainLyrics = !string.IsNullOrWhiteSpace(lyricData.PlainLyrics);

        SyncedLyricsText.Text = hasSyncedLyrics ? lyricData.SyncedLyrics : "No synced lyrics available";
        PlainLyricsText.Text = hasPlainLyrics ? lyricData.PlainLyrics : "No plain lyrics available";

        // Set metadata
        LyricsTypeText.Text = hasSyncedLyrics ? "Synced" : "Plain";
        LyricsDurationText.Text = TimeSpan.FromSeconds(lyricData.Duration).ToString(@"mm\:ss");
        InstrumentalIndicator.Visibility = lyricData.Instrumental ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        // Set the default tab based on what's available
        if (hasSyncedLyrics)
        {
            LyricsTabView.SelectedItem = SyncedTab;
            SyncedTab.IsEnabled = true;
        }
        else
        {
            SyncedTab.IsEnabled = false;
        }

        if (hasPlainLyrics)
        {
            PlainTab.IsEnabled = true;
            if (!hasSyncedLyrics)
            {
                LyricsTabView.SelectedItem = PlainTab;
            }
        }
        else
        {
            PlainTab.IsEnabled = false;
        }

        // Show/hide the timestamp button based on lyrics type
        // Only show timestamp button if we have plain lyrics but no synced lyrics
        TimestampButton.Visibility = (hasPlainLyrics && !hasSyncedLyrics) 
            ? Microsoft.UI.Xaml.Visibility.Visible 
            : Microsoft.UI.Xaml.Visibility.Collapsed;

        LyricsPreviewDialog.XamlRoot = this.Content.XamlRoot;

        await LyricsPreviewDialog.ShowAsync(ContentDialogPlacement.Popup);
    }

    private void LyricsPreviewDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Apply button - use the existing SelectLyricsCommand
        if (_currentPreviewLyrics != null && MyViewModel != null)
        {
            MyViewModel.SelectLyricsCommand.Execute(_currentPreviewLyrics);
            
            // Navigate back after applying
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }

    private void LyricsPreviewDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Edit button - load lyrics for editing
        if (_currentPreviewLyrics != null && MyViewModel?.LoadLyricsForEditingCommand != null)
        {
            MyViewModel.LoadLyricsForEditingCommand.Execute(_currentPreviewLyrics);
            
            // Close the dialog and potentially navigate to the editor
            // The navigation would happen based on the app's flow
        }
    }

    private void TimestampButton_Click(object sender, RoutedEventArgs e)
    {
        // Timestamp button - prepare plain lyrics for timestamping
        if (_currentPreviewLyrics != null && MyViewModel?.StartLyricsEditingSessionCommand != null)
        {
            // Load plain lyrics into the timestamping editor (preferred) or synced lyrics as fallback
            string lyricsToTimestamp = !string.IsNullOrWhiteSpace(_currentPreviewLyrics.PlainLyrics) 
                ? _currentPreviewLyrics.PlainLyrics 
                : _currentPreviewLyrics.SyncedLyrics ?? string.Empty;
            
            if (!string.IsNullOrWhiteSpace(lyricsToTimestamp))
            {
                MyViewModel.StartLyricsEditingSessionCommand.Execute(lyricsToTimestamp);
                
                // Close the dialog
                LyricsPreviewDialog.Hide();
                
                // Navigate to the timestamping page
                // This would typically be done through the SingleSongLyrics parent page
                // For now, we'll rely on the ViewModel state change to trigger UI updates
            }
        }
    }

    private void ReadySearchViewAndProduceSearchText_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.ReadySearchViewAndProduceSearchText();
    }

    private void ApplyLyric_Click(object sender, RoutedEventArgs e)
    {
        if(Frame.CanGoBack)
        {
            Frame.GoBack(); 
        }
    }

    private async void GoogleItBtn_Click(object sender, RoutedEventArgs e)
    {
        await MyViewModel.SearchSongPlainLyricsnOnlineSearch(null);
    }
}
