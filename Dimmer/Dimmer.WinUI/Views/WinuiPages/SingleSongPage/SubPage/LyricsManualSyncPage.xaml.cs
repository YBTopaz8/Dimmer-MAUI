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

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LyricsManualSyncPage : Page
{
    public BaseViewModelWin MyViewModel { get; set; }

    public LyricsManualSyncPage()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is BaseViewModelWin vm)
        {
            MyViewModel = vm;

            // Auto-scroll logic: When the "CurrentLine" changes, scroll to it
            MyViewModel.LyricsInEditor.CollectionChanged += LyricsInEditor_CollectionChanged;
        }
        this.KeyDown += LyricsManualSyncPage_KeyDown;
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        this.KeyDown -= LyricsManualSyncPage_KeyDown;
        if (MyViewModel != null && MyViewModel.LyricsInEditor != null)
        {
            MyViewModel.LyricsInEditor.CollectionChanged -= LyricsInEditor_CollectionChanged;
        }
        this.KeyDown -= LyricsManualSyncPage_KeyDown;
    }

    private void LyricsInEditor_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Find the index of the line where IsCurrentLine == true
        var currentLine = MyViewModel.LyricsInEditor.FirstOrDefault(x => x.IsCurrentLine);
        if (currentLine != null)
        {
            LyricsListView.ScrollIntoView(currentLine);
        }
    }

    private void LyricsManualSyncPage_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Allow Spacebar to trigger sync, BUT NOT if the user is typing in a TextBox
        if (e.Key == Windows.System.VirtualKey.Space)
        {
            if (e.OriginalSource is TextBox) return; // Let them type spaces

            SyncNextLine();
            e.Handled = true;
        }
    }

    private void SyncButton_Click(object sender, RoutedEventArgs e)
    {
        SyncNextLine();
    }

    private void SyncNextLine()
    {
        // Find the line that is currently marked as "IsCurrentLine"
        var targetLine = MyViewModel.LyricsInEditor.FirstOrDefault(x => x.IsCurrentLine);
        if (targetLine != null)
        {
            MyViewModel.TimestampCurrentLyricLineCommand.Execute(targetLine);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }
}