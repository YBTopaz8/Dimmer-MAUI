using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class SingleSongDetailsPopupView : UserControl
{
    public SingleSongDetailsPopupView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked after popup is closed
    /// Bool argument represents whether we navigate after dismissal
    /// string argument represents where to/what
    /// </summary>
    public event EventHandler<PopupDismissedEventArgs> DismissedRequested;


    BaseViewModelWin MyViewModel { get; set; }

    public void SetBaseViewModelWin(BaseViewModelWin vm)
    {
        MyViewModel = vm;
    }

    private void ClosePopUp_Click(object sender, RoutedEventArgs e)
    {
        PopupDismissedEventArgs evt = new()
        {
            HasActionAfterDismissed = false
        };

        DismissedRequested?.Invoke(this,evt);
    }

    private void PathView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //DismissedRequested?.Invoke(this, false);
        MyViewModel.OpenAndSelectFileInExplorer(MyViewModel.SelectedSong!);
    }
}



