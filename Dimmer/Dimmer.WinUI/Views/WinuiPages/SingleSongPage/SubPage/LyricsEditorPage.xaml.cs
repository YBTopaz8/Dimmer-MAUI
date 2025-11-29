using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

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

using Button = Microsoft.UI.Xaml.Controls.Button;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using ScrollBarVisibility = Microsoft.UI.Xaml.Controls.ScrollBarVisibility;
using Thickness = Microsoft.UI.Xaml.Thickness;

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
                MyViewModel.CurrentWinUIPage = this;
            }
        }
    }
    private void LyricsTextBox_Loaded(object sender, RoutedEventArgs e)
    {

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
    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BackBtnClick(object sender, RoutedEventArgs e)
    {
        if(Frame.CanGoBack)
        {
            // Prepare the animation, linking the key "ForwardConnectedAnimation" to our image
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", EditLyricsTxt);

            Frame.GoBack();
        }
    }
    private async void ViewLyrics_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not LrcLibLyrics lyricData)
            return;

        LyricsPreviewDialog.DataContext = lyricData;

        LyricsPreviewText.Text = lyricData.PlainLyrics;

        LyricsPreviewDialog.XamlRoot = this.Content.XamlRoot;

        await LyricsPreviewDialog.ShowAsync(ContentDialogPlacement.Popup);
    }

    private async void PasteFromTitleFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
        if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
        {
            var clipboardText = await dataPackageView.GetTextAsync();
            

            EditLyricsTxt.Text = clipboardText;
        }
        else
        {
            // No text found in clipboard
            EditLyricsTxt.Text = string.Empty;
        }
        }

    private void ReadySearchViewAndProduceSearchText_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.ReadySearchViewAndProduceSearchText();
    }
}
