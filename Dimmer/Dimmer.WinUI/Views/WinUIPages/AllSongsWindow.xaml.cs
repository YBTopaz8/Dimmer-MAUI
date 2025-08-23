using Dimmer.Data.Models;
using Dimmer.DimmerSearch.TQL;
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
using System.Threading.Tasks;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;

using WinUI.TableView;

using Colors = Microsoft.UI.Colors;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using DataTemplateSelector = Microsoft.UI.Xaml.Controls.DataTemplateSelector;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
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

        //when window is activated, focus on the search box and scroll to currently playing song
        this.Activated += (s, e) =>
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }
            MyViewModel.CurrentWinUIPage = this;
            // Focus the search box
            SearchSongSB.Focus(FocusState.Programmatic);
            // Scroll to the currently playing song
            if (MyViewModel.CurrentPlayingSongView != null)
            {
                ScrollToSong(MyViewModel.CurrentPlayingSongView);
            }
        };

        // Initialize collections for live updates
        var realm = MyViewModel.RealmFactory.GetRealmInstance();
        _liveArtists = new ObservableCollection<string>(realm.All<ArtistModel>().AsEnumerable().Select(x => x.Name));
        _liveAlbums = new ObservableCollection<string>(realm.All<AlbumModel>().AsEnumerable().Select(x => x.Name));
        _liveGenres = new ObservableCollection<string>(realm.All<GenreModel>().AsEnumerable().Select(x => x.Name));

    }

    public ObservableCollection<string> _liveArtists;
    public ObservableCollection<string> _liveAlbums;
    public ObservableCollection<string> _liveGenres;

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

    private void TableView_CellContextFlyoutOpening(object sender, global::WinUI.TableView.TableViewCellContextFlyoutEventArgs e)
    {

    }

    private void TableView_ExportSelectedContent(object sender, global::WinUI.TableView.TableViewExportContentEventArgs e)
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

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.DeleteSongsCommand.Execute(MyViewModel.SearchResults);
    }

    private void AddToQueue_Click(object sender, RoutedEventArgs e)
    {

    }

    private void MySongsTableView_Tapped(object sender, TappedRoutedEventArgs e)
    {
    }

    private void SearchSongSB_TextChanged_1(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        var send = sender as TextBox;
        if (send == null)
            return;
        // This is the text changed event handler for the search box.
        // You can access the text like this:
        var text = send.Text;
        MyViewModel.SearchSongSB_TextChanged(text);
    }

    private void SearchSongSB_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {

        var box = sender as RichEditBox;
        if (box == null)
            return;

        // Get the full text from the box
        box.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string text);

        // Prevent recursive event firing
        box.TextChanged -= SearchSongSB_TextChanged;

        // Highlight the syntax
        HighlightSyntax(box.TextDocument, text);

        // Restore the event handler
        box.TextChanged += SearchSongSB_TextChanged;

    }
    private void HighlightSyntax(Microsoft.UI.Text.RichEditTextDocument document, string text)
    {
        // First, clear all previous formatting by setting the whole range to the default color
        document.GetRange(0, TextConstants.MaxUnitCount).CharacterFormat.ForegroundColor = Colors.White;

        // Use your existing, powerful Lexer
        var tokens = Lexer.Tokenize(text);

        foreach (var token in tokens)
        {
            if (string.IsNullOrEmpty(token.Text))
                continue;

            // Define colors for different token types
            var color = token.Type switch
            {
                TokenType.Identifier when FieldRegistry.FieldsByAlias.ContainsKey(token.Text) => Colors.CornflowerBlue,
                TokenType.And or TokenType.Or or TokenType.Not => Colors.HotPink,
                TokenType.Asc or TokenType.Desc or TokenType.First or TokenType.Last or TokenType.Random => Colors.MediumPurple,
                TokenType.StringLiteral => Colors.SandyBrown,
                TokenType.Number => Colors.LightGreen,
                TokenType.GreaterThan or TokenType.LessThan or TokenType.Equals => Colors.IndianRed,
                TokenType.Error => Colors.Red,
                _ => Colors.White // Default
            };

            // Apply the color to the specific range of the token
            var range = document.GetRange(token.Position, token.Position + token.Text.Length);
            if (range != null)
            {
                if (token.Type == TokenType.Identifier && FieldRegistry.FieldsByAlias.ContainsKey(token.Text))
                {
                    range.CharacterFormat.ForegroundColor = Colors.CornflowerBlue;
                    range.CharacterFormat.Bold =  Microsoft.UI.Text.FormatEffect.On; // Make keywords bold
                }
                else // Reset for other token types
                {
                    range.CharacterFormat.Bold = Microsoft.UI.Text.FormatEffect.Off;
                }
                range.CharacterFormat.ForegroundColor = color;
            }
        }
    }
    private void SearchSongSB_FocusEngaged(Control sender, FocusEngagedEventArgs args)
    {

    }

    private void SearchSongSB_FocusDisengaged(Control sender, FocusDisengagedEventArgs args)
    {

    }

    private void SearchSongSB_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {

    }

    private void SearchSongSB_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {

    }

    private void SearchSongSB_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {

    }

    private void SearchSongSB_KeyDown(object sender, KeyRoutedEventArgs e)
    {

    }

    private void SearchSongSB_Paste(object sender, TextControlPasteEventArgs e)
    {

    }

    private void SearchSongSB_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {

    }

    private void SearchSongSB_SelectionChanged(object sender, RoutedEventArgs e)
    {

    }




    private void SearchSongSB_TextCompositionStarted(TextBox sender, TextCompositionStartedEventArgs args)
    {

    }

    private void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Only get suggestions if the user is typing
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // Get the cursor position to provide context-aware suggestions
            var internalTextBox = VisualTreeHelpers.FindChildOfType<TextBox>(sender);
            if (internalTextBox == null)
                return;

            // Now we can get the caret position from the internal TextBox
            var cursorPosition = internalTextBox.SelectionStart;

            var suggestions = AutocompleteEngine.GetSuggestions(_liveArtists, _liveAlbums, _liveGenres, sender.Text, cursorPosition);
            sender.ItemsSource = suggestions;
        }

        MyViewModel.SearchSongSB_TextChanged(sender.Text);
    }

    private void SearchAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {  // --- MODIFIED: Use the helper here as well ---
        var internalTextBox = VisualTreeHelpers.FindChildOfType<TextBox>(sender);
        if (internalTextBox == null)
            return;

        string currentText = sender.Text;
        // We get the position BEFORE the text is changed by the suggestion being chosen.
        int cursorPosition = internalTextBox.SelectionStart;

        int wordStart;
        if (cursorPosition <= 0)
        {
            // Edge Case 1: Cursor is at the very beginning. The word must start at 0.
            wordStart = 0;
        }
        else
        {
            // Start searching from the character *before* the cursor.
            // We use Math.Min to ensure we don't go past the end of the string if the
            // cursor position is somehow invalid.
            int searchStartIndex = Math.Min(cursorPosition - 1, currentText.Length - 1);

            int separatorIndex = currentText.LastIndexOfAny(new char[] { ' ', ':', '(' }, searchStartIndex);

            if (separatorIndex == -1)
            {
                // Edge Case 2: No separator was found behind the cursor. The word starts at the beginning of the string.
                // Example: User typed "artist" and cursor is at the end.
                wordStart = 0;
            }
            else
            {
                // Normal Case: A separator was found. The word starts one character after it.
                // Example: User typed "fav:tr" and cursor is at the end. separatorIndex points to ':'.
                wordStart = separatorIndex + 1;
            }
        }
        // Build the new text string
        string newText = currentText.Substring(0, wordStart) + args.SelectedItem.ToString();

        // Set the text in the AutoSuggestBox
        sender.Text = newText;

        // --- IMPORTANT: Manually set the caret position to the end of the newly inserted text ---
        // This provides a much better user experience.
        internalTextBox.SelectionStart = newText.Length;
    }

    private void Grid_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        // Get the Grid that was right-clicked
        var grid = sender as Grid;
        if (grid == null)
            return;

        // Get the SongModelView associated with that Grid's row
        var song = grid.DataContext as SongModelView;
        if (song == null)
            return;

        // Get our predefined MenuFlyout from the page's resources
        var flyout = MyPageGrid.Resources["SongRowMenuFlyout"] as MenuFlyout;
        if (flyout == null)
            return;
        flyout.ShowAt(grid);
    }

    public string Format(string format, object arg)
    {
        return string.Format(format, arg);
    }

    private void SearchAutoSuggestBox_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {
        var charRec = args.Character;
        Debug.WriteLine($"Char received: {charRec}");
        //if the character is Enter, then execute the search
        if (charRec == '\r' || charRec == '\n')
        {

            var suggestBox = sender as AutoSuggestBox;

            if (suggestBox  == null)
                return;



            // 1. Get the current text from the search box.
            string queryText = suggestBox.Text;

            // 2. A simple way to create "chips": split the query by spaces.
            //    This handles simple cases like "fav:true desc played" very well.
            //    We filter out any empty strings that might result from multiple spaces.
            var newChips = queryText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // 3. Add these new string chips to our ViewModel's collection.
            foreach (var chip in newChips)
            {
                MyViewModel.QueryChips.Add(chip);
            }

            // 4. Reconstruct the full query string from ALL the chips (old and new).
            string fullQuery = string.Join(" ", MyViewModel.QueryChips);

            // 5. Trigger the search with the complete query.
            //    We don't call SearchSongSB_TextChanged here because that's for live updates.
            //    We use the _searchQuerySubject directly for the final, full query.
            MyViewModel.TriggerSearch(fullQuery); // We will add this helper to the ViewModel

            // 6. Clear the search box for the next input.
            suggestBox.Text = string.Empty;
        }
    }
    public class QueryComponentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FilterTemplate { get; set; }
        public DataTemplate? JoinerTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object? item, DependencyObject? container)
        {
            return item switch
            {
                ViewModel.ActiveFilterViewModel => FilterTemplate,
                ViewModel.LogicalJoinerViewModel => JoinerTemplate,
                _ => base.SelectTemplateCore(item, container)
            };
        }
    }


    public static class VisualTreeHelpers
    {
        /// <summary>
        /// Finds a child control of a specific type within the visual tree of a parent element.
        /// </summary>
        /// <typeparam name="T">The type of the child control to find.</typeparam>
        /// <param name="parent">The parent element to search within.</param>
        /// <returns>The first child control of the specified type, or null if not found.</returns>
        public static T? FindChildOfType<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindChildOfType<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }
    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var send = (sender as MenuFlyoutItem);
        if (send == null)
            return;
        // Handle the click event for the MenuFlyoutItem
        var selectedSongs = MySongsTableView.SelectedItems
            .OfType<SongModelView>()
            .ToList();
        if (selectedSongs.Count == 0)
            {
            // If no songs are selected, you might want to show a message or handle it accordingly
            Debug.WriteLine("No songs selected.");
            return;
        }
        // Perform the action based on the MenuFlyoutItem clicked
        switch (send.Name)
        {
            case "PlaySelected":
                // Play the selected songs
                await MyViewModel.PlayNextSongsImmediately(selectedSongs);
                break;
            case "AddToQueue":
                // Add the selected songs to the queue
                MyViewModel.AddToQueueEnd(selectedSongs);
                break;
            case "DeleteSelected":
                // Delete the selected songs
                MyViewModel.DeleteSongsCommand.Execute(selectedSongs);
                break;
                case "AddToPlaylist":
            // Add the selected songs to a playlist
                //await MyViewModel.AddToPlaylist(selectedSongs);
                break;
                case "AddToFavorites":
            // Add the selected songs to favorites
                //MyViewModel.AddToFavorites(selectedSongs);
                break;  
                case "RemoveFromFavorites":
            // Remove the selected songs from favorites
                //MyViewModel.RemoveFromFavorites(selectedSongs);
                break;
                case "AddNotes":
            // Add notes to the selected songs
                //await MyViewModel.AddNotesToSongs(selectedSongs);
                break;
                case "EditTags":
            // Edit tags for the selected songs
                //await MyViewModel.EditTagsForSongs(selectedSongs);
                break;
                case "OpenFileLocation":
            // Open the file location of the selected songs
                //MyViewModel.OpenFileLocationForSongs(selectedSongs);
                break;  
                case "CopyToClipboard":
            // Copy the selected songs to the clipboard
                
                break;

            default:
                Debug.WriteLine($"Unknown action: {send.Name}");
                break;
        }
        //MyViewModel.DeleteSongs()
    }

    private void MySongsTableView_CellContextFlyoutOpening(object sender, TableViewCellContextFlyoutEventArgs e)
    {

    }

    private void MySongsTableView_ClearSorting(object sender, TableViewClearSortingEventArgs e)
    {

    }

    private void MySongsTableView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {

    }

    private void MySongsTableView_Holding(object sender, HoldingRoutedEventArgs e)
    {

    }

    private void MySongsTableView_CellSelectionChanged(object sender,TableViewCellSelectionChangedEventArgs e)
    {

    }

    private void MySongsTableView_RowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
    {

    }

    private void MySongsTableView_Sorting(object sender, TableViewSortingEventArgs e)
    {

    }

    private void MySongsTableView_Loading(FrameworkElement sender, object args)
    {

    }

    private void MySongsTableView_ExportSelectedContent(object sender, TableViewExportContentEventArgs e)
    {

    }
}