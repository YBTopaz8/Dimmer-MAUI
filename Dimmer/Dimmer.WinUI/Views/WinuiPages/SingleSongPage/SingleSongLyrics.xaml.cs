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
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SingleSongLyrics : Page
{
    private Type pageType;
    private int previousSelectedIndex;

    public BaseViewModelWin? MyViewModel { get; private set; }

    private SongModelView? _storedSong;

    public SingleSongLyrics()
    {
        InitializeComponent();
        
    }
    

    private void SelectorBar2_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(LyricsEditorPage);
                break;
                case 1:
                break;
                case 2:
                pageType = typeof(LyricsManualSyncPage);
                break;
            case 3:
                    break;
            default:
                break;
        }
        MyViewModel = IPlatformApplication.Current.Services.GetService<BaseViewModelWin>();
        _storedSong = MyViewModel!.SelectedSong;
        var navParams = new SongDetailNavArgs
        {
            Song = _storedSong!,
            ViewModel = MyViewModel
        };


        var sliderNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0
            ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromBottom;

        ContentFrame.Navigate(pageType, navParams,
            new SlideNavigationTransitionInfo { Effect = sliderNavigationTransitionEffect });
    }
}
