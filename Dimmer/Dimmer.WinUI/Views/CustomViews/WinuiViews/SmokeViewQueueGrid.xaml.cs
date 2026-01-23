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
using Windows.Foundation.Metadata;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class SmokeViewQueueGrid : UserControl
{
    public SmokeViewQueueGrid()
    {
        InitializeComponent();

    }

    public BaseViewModelWin MyViewModel { get; set; }

    public void SetBaseViewModelWin(BaseViewModelWin vm)
    {
        MyViewModel=vm;
    }

    private void SmokeGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props != null)
        {
            if (props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed)
            {
                PopUpBackButton_Click(sender, e);
            }
        }
    }
    private void Animation_Completed(ConnectedAnimation sender, object args)
    {
        this.Visibility = WinUIVisibility.Collapsed;
    }
    private async void PopUpBackButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardAnimation", this.ViewQueueGrid);

        // Collapse the smoke when the animation completes.
        animation.Completed += Animation_Completed;


        // Use the Direct configuration to go back (if the API is available).
        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
        {
            animation.Configuration = new GravityConnectedAnimationConfiguration();

        }

        DismissRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void SaveQueueAsPlaylist_Click(object sender, RoutedEventArgs e)
    {
        // Show a dialog to get the playlist name
        var dialog = new ContentDialog
        {
            Title = "Save Queue as Playlist",
            PrimaryButtonText = "Save",

            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var textBox = new TextBox
        {

            Width = 300
        };
        var helperText = new TextBlock
        {
            Text = "Please enter a name for the new playlist.",
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8)
        };
        var stackPanel = new StackPanel();
        stackPanel.Children.Add(textBox);
        stackPanel.Children.Add(helperText);
        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            throw new NotImplementedException();
            //MyViewModel.SaveQueueAsPlaylistCommand.Execute(textBox.Text);

            // Show confirmation
            var confirmDialog = new ContentDialog
            {
                Title = "Success",
                Content = $"Queue saved as playlist '{textBox.Text}'",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await confirmDialog.ShowAsync();
        }
    }

    private void NowPlayingPBQueue_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        // The ListView automatically reorders items in the UI, but we need to update the underlying queue
        // Get the current order from the ListView
        var currentOrder = this.NowPlayingPBQueue.Items.Cast<SongModelView>().ToList();

        // Find what was moved
        if (args.Items.Count > 0)
        {
            var movedItem = args.Items[0] as SongModelView;
            if (movedItem != null)
            {
                var newIndex = currentOrder.IndexOf(movedItem);
                var oldIndex = MyViewModel.PlaybackQueue.IndexOf(movedItem);

                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    // Update the ViewModel's queue to match the new order
                    MyViewModel.MoveSongInQueue(oldIndex, newIndex);
                }
            }
        }
    }

    private async void InsertSongsBefore_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuFlyoutItem)sender;
        var targetSong = menuItem.DataContext as SongModelView;

        if (targetSong == null) return;

        // TODO: Implement proper song picker dialog
        // For now, use the search results as the songs to insert
        // This is a placeholder implementation for demonstration purposes
        var songsToInsert = MyViewModel.SearchResults.Take(5).ToList();

        if (songsToInsert.Any())
        {
            var param = new Tuple<SongModelView, IEnumerable<SongModelView>>(targetSong, songsToInsert);
            throw new NotImplementedException();// MyViewModel.InsertSongsBeforeInQueueCommand.Execute(param);
        }
    }

    private async void InsertSongsAfter_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = (MenuFlyoutItem)sender;
        var targetSong = menuItem.DataContext as SongModelView;

        if (targetSong == null) return;

        // TODO: Implement proper song picker dialog
        // For now, use the search results as the songs to insert
        // This is a placeholder implementation for demonstration purposes
        var songsToInsert = MyViewModel.SearchResults.Take(5).ToList();

        if (songsToInsert.Any())
        {
            var param = new Tuple<SongModelView, IEnumerable<SongModelView>>(targetSong, songsToInsert);
            throw new NotImplementedException();
            //MyViewModel.InsertSongsAfterInQueueCommand.Execute(param);
        }
    }
    private async void RemoveSongFromQueue_Click(object sender, RoutedEventArgs e)
    {
        var send = (FrameworkElement)sender;
        var song = send.DataContext as SongModelView;
        await MyViewModel.RemoveFromQueue(song);
    }


    private async void PlaySongBtn_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var song = send.DataContext as SongModelView;

        await MyViewModel.PlaySongAsync(song, CurrentPage.NowPlayingPage, MyViewModel.PlaybackQueue);
    }

    private async void NowPlayingSongImg_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

        var npqIndex = MyViewModel.PlaybackQueue.IndexOf(MyViewModel.CurrentPlayingSongView);
        await this.NowPlayingPBQueue.SmoothScrollIntoViewWithIndexAsync(npqIndex,
            ScrollItemPlacement.Top, false,
            true);
    }

    public event EventHandler DismissRequested;
}
