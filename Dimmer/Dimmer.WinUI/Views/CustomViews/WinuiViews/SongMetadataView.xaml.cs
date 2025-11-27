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

public sealed partial class SongMetadataView : UserControl
{
    public SongMetadataView()
    {
        InitializeComponent();
    }
    BaseViewModelWin ViewModel => DataContext as BaseViewModelWin ?? throw new InvalidOperationException("DataContext is not a valid BaseViewModelWin.");

    private void AddNoteBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void LoadAllArtists_Click(object sender, RoutedEventArgs e)
    {

    }
}
