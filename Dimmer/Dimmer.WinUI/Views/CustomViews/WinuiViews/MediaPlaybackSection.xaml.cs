// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class MediaPlaybackSection : UserControl
{
    public MediaPlaybackSection()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
    }
    public BaseViewModelWin? MyViewModel { get; internal set; }


    private void PlayPauseBtn_Checked(object sender, RoutedEventArgs e)
    {
        string uri = "ms-appx:///Assets/Images/pausecircle.svg";


        //PlayPauseImg.Source = new SvgImageSource(new Uri(uri));

    }

    private void PlayPauseBtn_Unchecked(object sender, RoutedEventArgs e)
    {
        string uri = "ms-appx:///Assets/Images/playcircle.svg";
        //PlayPauseImg.Source = new SvgImageSource(new Uri(uri));

    }

    private void TopPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props is null) return;
        if (props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased)
        {
            MyViewModel?.ScrollToSpecificSongCommand.Execute(MyViewModel.CurrentPlayingSongView);
        }
    }

    private void PrevBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void PlayBtn_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel == null) return;
        
    }

    private void PlayBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void NextBtn_AccessKeyDisplayRequested(UIElement sender, AccessKeyDisplayRequestedEventArgs args)
    {
        
    }

    private void NextBtn_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
    {

    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        return;
        MyViewModel?.MainWindow.ContentFrame.GoBack();
    }
}
