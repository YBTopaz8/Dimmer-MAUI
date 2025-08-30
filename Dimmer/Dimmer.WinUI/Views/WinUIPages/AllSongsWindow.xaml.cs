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
using System.Text.RegularExpressions;
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
            var removeCOmmandFromLastSaved = MyViewModel.CurrentTqlQuery;
            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addto:\d+!", "", RegexOptions.IgnoreCase);
            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addto:end!", "", RegexOptions.IgnoreCase);
            removeCOmmandFromLastSaved = Regex.Replace(removeCOmmandFromLastSaved, @">>addnext!", "", RegexOptions.IgnoreCase);


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

        this.Closed +=AllSongsWindow_Closed;
    }

    private void AllSongsWindow_Closed(object sender, WindowEventArgs args)
    {

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
        var songs = MySongsTableView.Items ;
        Debug.WriteLine(songs.Count);


        // now we need items as enumerable of SongModelView

        var SongsEnumerable = songs.OfType<SongModelView>();

        Debug.WriteLine(SongsEnumerable.Count());


        if (song != null)
        {
            // You found the song! Now you can call your ViewModel command.
            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            await MyViewModel.PlaySong(song, songs: SongsEnumerable);
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
            var internalTextBox = VisualTreeHelpers.FindChildOfType<TextBox>(sender);
            if (internalTextBox == null)
                return;

            var cursorPosition = internalTextBox.SelectionStart;

            // Get suggestions based on the current text fragment
            var suggestions = AutocompleteEngine.GetSuggestions(
                _liveArtists, _liveAlbums, _liveGenres, sender.Text, cursorPosition);
            sender.ItemsSource = suggestions;
        }

            MyViewModel.SearchSongSB_TextChanged(sender.Text);
    }

    private void SearchAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {  // --- MODIFIED: Use the helper here as well ---
       // 1. Find the start of the word the user is currently typing.
        string currentText = sender.Text;
        int wordStart = FindWordStart(currentText);

        // 2. Extract the prefix (e.g., "artist:", "title:"). The prefix is everything
        //    from the start of the word up to the last separator (like ':').
        string currentWordFragment = currentText.Substring(wordStart);
        int separatorIndex = currentWordFragment.LastIndexOf(':');

        string fullChip;
        if (separatorIndex != -1)
        {
            // Case: A prefix exists, like "artist:".
            string prefix = currentWordFragment.Substring(0, separatorIndex + 1);
            // Combine prefix with the chosen suggestion.
            fullChip = prefix + args.SelectedItem.ToString();
        }
        else
        {
            // Case: No prefix, it's a simple word.
            fullChip = args.SelectedItem.ToString();
        }

    }
    private void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        {
            string queryText = "";

            if (args.ChosenSuggestion != null)
            {
                // This case is already handled by SuggestionChosen, but we can leave it
                // and just let that handler do its work. No extra code needed here for this case.
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                // User typed text and pressed Enter without choosing a suggestion.
                // Treat the text as a new chip.
                queryText = args.QueryText;
                MyViewModel.QueryChips.Add(queryText);

                // Clear the box and trigger the search
                sender.Text = string.Empty;
                MyViewModel.TriggerSearch(string.Empty);
            }
        }
    }


    // --- HELPER METHOD to find the start of the current word ---
    // This is a more robust version of the logic you had.
    private int FindWordStart(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // We look for a space, which separates our query "chips".
        int lastSeparator = text.LastIndexOf(' ');

        if (lastSeparator == -1)
        {
            // No space found, the word starts at the beginning.
            return 0;
        }
        else
        {
            // The word starts one character after the last space.
            return lastSeparator + 1;
        }
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

    private void SearchAutoSuggestBox_QuerySubmitted_1(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }

    private void RemoveChipButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is string chipToRemove)
        {
            MyViewModel.QueryChips.Remove(chipToRemove);
            // The CollectionChanged event in the ViewModel will automatically trigger a new search.
        }
        //MyViewModel.QueryChips.Remove(e.);
    }

    private void MySongsTableView_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {

    }

    private void SearchAutoSuggestBox_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
    {

    }

    private void SearchAutoSuggestBox_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {
        var xterPressed = args.Character;
        var box = sender as TextBox;
        if (box == null)
            return;
        // Get the full text from the box
        var text = box.Text;
        //Debug.WriteLine($"Character received: {xterPressed}");
        //if (xterPressed == '\r' || xterPressed == '\n')
        //{
        //    // Handle Enter key press
        //    Debug.WriteLine("Enter key pressed.");
        //    // You can trigger your search or any other action here

        //}
        MyViewModel.SearchSongSB_TextChanged(text);
    }

    private void SearchAutoSuggestBox_CopyingToClipboard(TextBox sender, TextControlCopyingToClipboardEventArgs args)
    {

    }

    private void SearchAutoSuggestBox_CuttingToClipboard(TextBox sender, TextControlCuttingToClipboardEventArgs args)
    {

    }

    private void SearchAutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (args.FocusState == FocusState.Programmatic)
        { 
            return;
        }
    }

    private void SearchAutoSuggestBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {

    }
}