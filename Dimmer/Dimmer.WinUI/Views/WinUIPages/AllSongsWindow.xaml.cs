using Dimmer.WinUI.Utils.WinMgt;

using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Foundation;
using Windows.Foundation.Collections;

using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllSongsWindow : Window
{
    public AllSongsWindow(BaseViewModelWin vm)
    {
        InitializeComponent();


        MyPageGrid.DataContext=vm;
        MyViewModel= vm;
    }

    public BaseViewModelWin MyViewModel { get; internal set; }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void TableView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

    private void TableView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void TableView_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {

    }

    private async void TableView_CellContextFlyoutOpening(object sender, global::WinUI.TableView.TableViewCellContextFlyoutEventArgs e)
    {
        
    }

    private void TableView_ExportSelectedContent(object sender, global::WinUI.TableView.TableViewExportContentEventArgs e)
    {

    }

    private void TableView_BringIntoViewRequested_1(UIElement sender, BringIntoViewRequestedEventArgs args)
    {
    }

    private async void TableView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // e.OriginalSource is the specific UI element that received the tap 
        // (e.g., a TextBlock, an Image, a Grid, etc.).
        var element = e.OriginalSource as FrameworkElement;
        SongModelView? song = null;
        if (element == null)
            return;
        while (element != null && element != sender)
        {
            if (element.DataContext is SongModelView currentSong)
            {
                song = currentSong;
                break; // Found it!
            }
            element = element.Parent as FrameworkElement;
        }

        if (song != null)
        {
            // You found the song! Now you can call your ViewModel command.
            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            await MyViewModel.PlaySong(song);
        }
    }
    public void ScrollToSong(SongModelView songToFind)
    {
        if (songToFind == null)
            return;

        // The magic happens here. ScrollIntoView tells the list to find
        // the UI container for this data item and bring it into the viewport.
        //MySongsTableView.ScrollIntoView(songToFind);

        // For more control, you can specify the alignment.
        // This will try to position the item at the top of the list.
         MySongsTableView.ScrollIntoView(songToFind, ScrollIntoViewAlignment.Leading);
    }

    private void ScrollToSong_Click(object sender, RoutedEventArgs e)
    {
        ScrollToSong(MyViewModel.CurrentPlayingSongView);
    }


    private void SearchSongSB_TextCompositionChanged(RichEditBox sender, TextCompositionChangedEventArgs args)
    {

    }

    private void SearchSongSB_TextCompositionStarted(RichEditBox sender, TextCompositionStartedEventArgs args)
    {

    }

    private void SearchSongSB_TextCompositionEnded(RichEditBox sender, TextCompositionEndedEventArgs args)
    {

    }

    private void SearchSongSB_TextChanged(object sender, RoutedEventArgs e)
    {
        var send = sender as TextBox;
        if (send == null)
            return;
        // This is the text changed event handler for the search box.
        // You can access the text like this:
        var text = send.Text;
        MyViewModel.SearchSongSB_TextChanged(text);
        

    }

    
}
