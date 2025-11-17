using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

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
        MyViewModel.ScrollToCurrentPlayingSongCommand.Execute(null);
    }
}
