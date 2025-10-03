using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices.WindowsRuntime;
using Dimmer.DimmerSearch.TQL;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.Text;

using WinUI.TableView;

using Colors = Microsoft.UI.Colors;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using DataTemplateSelector = Microsoft.UI.Xaml.Controls.DataTemplateSelector;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Image = Microsoft.UI.Xaml.Controls.Image;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;

using Page = Microsoft.UI.Xaml.Controls.Page;
using CheckBox = Microsoft.UI.Xaml.Controls.CheckBox;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllSongsListPage : Page
{
    
    public AllSongsListPage( ) 
    {
        InitializeComponent();

        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
    }
    BaseViewModelWin MyViewModel { get; set; }

    private TableViewCellSlot _lastActiveCellSlot;
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void TableView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        return;
        var isCtlrKeyPressed = e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
            (Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control) &
             Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        if (isCtlrKeyPressed)
            ProcessCellClick(isExclusion: false);
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
        var songs =  MySongsTableView.Items;
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



    private void SearchSongSB_Text
        (object sender, RoutedEventArgs e)
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
        return;
        var isCtlrKeyPressed = e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
            (Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control) &
             Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        if (isCtlrKeyPressed)
            ProcessCellClick(isExclusion: true);

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
                MyViewModel.AddListOfSongsToQueueEnd(selectedSongs);
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

    private async void MySongsTableView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        // e.Items contains the SongModelView objects being dragged.
        var songsToDrag = e.Items.OfType<SongModelView>().ToList();

        // Package the songs' IDs or other identifiers.
        e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.Items.Clear(); // Prevent default drag UI
        e.Items.Add(songsToDrag);

        var storageItems = songsToDrag
            .Select(s => Windows.Storage.StorageFile.GetFileFromPathAsync(s.FilePath).AsTask())
            .ToArray();
        var files = await Task.WhenAll(storageItems);
        // pass song ids as text as well so that we can identify them in the drop target

        var songIds = string.Join(",", songsToDrag.Select(s => s.Id));
        e.Data.SetText($"songids:{songIds}");


        e.Data.SetStorageItems(files);
        e.Data.ShareCompleted += (s, args) =>
        {
            // Handle post-drag actions here if needed
            Debug.WriteLine("Drag operation completed.");
        };
        e.Data.ShareCanceled += (s, args) =>
        {
            // Handle drag cancellation here if needed
            Debug.WriteLine("Drag operation canceled.");
        };

    }

    private void MySongsTableView_Holding(object sender, HoldingRoutedEventArgs e)
    {

    }

    private void MySongsTableView_CellSelectionChanged(object sender, TableViewCellSelectionChangedEventArgs e)
    {

    }

    private void MySongsTableView_RowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
    {

    }

    private void MySongsTableView_Sorting(object sender, TableViewSortingEventArgs e)
    {
        Debug.WriteLine(e.Column?.Header);
        Debug.WriteLine(e.Column?.Order);
        Debug.WriteLine(e.Handled);
        // latter, log it in vm
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
      
        // Get the full text from the box
        var text = box.Text;
        //Debug.WriteLine($"Character received: {xterPressed}");
        if (xterPressed == '\r' || xterPressed == '\n')
        {

            MyViewModel.SearchSongSB_TextChanged(text);
            // Handle Enter key press
            Debug.WriteLine("Enter key pressed.");
            return;
            // You can trigger your search or any other action here

        }
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

    private void ViewSong_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine(MySongsTableView.ItemsSource?.GetType());
    }

    private void MySongsTableView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine(e.Pointer.PointerId);
        Debug.WriteLine(e.OriginalSource.GetType());

        // if it is a middle click, exclude in tql explictly
        if (e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed)
        {
            var currentTql = MyViewModel.CurrentTqlQuery;



        }
    }

    private void MySongsTableView_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

    }

    private void MySongsTableView_PointerEntered_1(object sender, PointerRoutedEventArgs e)
    {

    }

    private void MySongsTableView_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    private void MySongsTableView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void MySongsTableView_CurrentCellChanged(object sender, DependencyPropertyChangedEventArgs e)
     {
        //var OldValue = e.OldValue;
        //TableViewCellSlot newValue = (TableViewCellSlot)e.NewValue;
        //var propValue = e.Property;
        //Debug.WriteLine(propValue?.GetType());
        //Debug.WriteLine(OldValue?.GetType());

        //List<TableViewCellSlot> cells = new List<TableViewCellSlot>();
        //cells.Add(newValue);

        //var we=  MySongsTableView.GetCellsContent(cells, true);
        //var ee =  MySongsTableView.GetSelectedContent(true);

        // This event tells us exactly which cell the user just moved to or clicked on.
        // We just store its location for the Tapped/RightTapped event to use.
        if (e.NewValue is TableViewCellSlot newSlot)
        {
            _lastActiveCellSlot = newSlot;
        }
        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        if (nativeElement == null)
            return;
        // figure out if it is right click

        //var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(nativeElement).Properties;

        //if (properties.IsRightButtonPressed)
        //{
        //    MyViewModel.AddToNext();
        //    return;


        //}
    }

    private void MySongsTableView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }

    private void MySongsTableView_DragItemsStarting_1(object sender, DragItemsStartingEventArgs e)
    {

    }

    private void MySongsTableView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {

    }

    private void MySongsTableView_ChoosingGroupHeaderContainer(ListViewBase sender, ChoosingGroupHeaderContainerEventArgs args)
    {

    }

    private void MySongsTableView_ChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    {

    }

    private void MySongsTableView_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        //var oldElementFOcused = args.OldFocusedElement as TextBox; for example!

    }

    private void MySongsTableView_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {

    }

    private void MySongsTableView_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    private void MySongsTableView_LostFocus(object sender, RoutedEventArgs e)
    {

    }

    private void MySongsTableView_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void MySongsTableView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }

    private async void CheckBox_Click(object sender, RoutedEventArgs e)
    {
        try
        {

            var ee = (CheckBox)e.OriginalSource;
            var song = (SongModelView)ee.DataContext;
            if (song == null)
            {

                return;
            }
            await MyViewModel.AddFavoriteRatingToSong(song);
        }
        catch (Exception ex)
        {
            
        }
    }

    public static T? FindVisualChild<T>(DependencyObject? parent, string? childName) where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // Check if the current child is the target control.
            if (child is T frameworkElement && frameworkElement.Name == childName)
            {
                return frameworkElement;
            }

            // If not, recursively search in the children of the current child.
            T? childOfChild = FindVisualChild<T>(child, childName);
            if (childOfChild != null)
            {
                return childOfChild;
            }
        }
        return null;
    }

    private void MySongsTableView_DragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {

    }


    private SongModelView? _storedSong;

    private void MyPageGrid_Loaded(object sender, RoutedEventArgs e)
    {

        if (_storedSong != null)
        {
            // --- THE FIX ---
            // 1. Capture the song to animate into a local variable.
            var songToAnimate = _storedSong;

            // 2. Clear the instance field immediately. This is good practice
            //    to ensure the page state is clean for future events.
            _storedSong = null;

            // Ensure the item is visible
            MySongsTableView.ScrollIntoView(songToAnimate, ScrollIntoViewAlignment.Default);

            // 3. Queue the animation logic using the LOCAL variable.
            DispatcherQueue.TryEnqueue( () =>
            {
                ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
                if (animation != null)
                {
                // Use the captured 'songToAnimate' variable, which is guaranteed to be non-null.
                // The extra null check is no longer needed here.
                if (animation == null)
                {
                    // If there's no animation prepared, do nothing.
                    return;
                }

                // --- THE ROBUST SOLUTION ---
                // Manually find the target element to ensure it's ready.
                var row = MySongsTableView.ContainerFromItem(songToAnimate) as FrameworkElement;
                    if (row != null)
                    {
                        // The row container has been realized, now find the image inside it.
                        var image = FindVisualChild<Image>(row, "coverArtImage");
                        if (image != null)
                        {
                     
                            animation.TryStart(image);
                        }
                        else
                        {
                            // Fallback: The row exists, but the image inside is not ready.
                            // The animation is implicitly cancelled. This prevents the crash.
                        }
                    }
                    else
                    {
                        // Fallback: The entire row container could not be found in time.
                        // The animation is implicitly cancelled. This prevents the crash.
                    }
                }
            });
        }
    }

    private void CoverImageGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

        var selectedSong = (sender as FrameworkElement)?.DataContext as SongModelView;

        // Store the item for the return trip
        _storedSong = selectedSong;

        // Find the specific UI element (the Image) that was clicked on
        var row = MySongsTableView.ContainerFromItem(selectedSong) as FrameworkElement;
        if (row != null)
        {
            var image = FindVisualChild<Image>(row, "coverArtImage");
            if (image != null)
            {
                // Prepare the animation, linking the key "ForwardConnectedAnimation" to our image
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", image);
            }
        }

        // Navigate to the detail page, passing the selected song object.
        // Suppress the default page transition to let ours take over.
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        if (Frame != null)
        {

            Frame.Navigate(songDetailType, _storedSong, supNavTransInfo);

        }
    }

    private void MyPageGrid_Unloaded(object sender, RoutedEventArgs e)
    {

    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var vm = e.Parameter as BaseViewModelWin;
        // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel == null)
        {
            if (e.Parameter == null)
            {
                throw new InvalidOperationException("Navigation parameter is null");
            }

            if (vm == null)
            {
                throw new InvalidOperationException("Navigation parameter is not of type BaseViewModelWin");
            }

            MyViewModel = vm;
            MyViewModel.MySongsTableView = MySongsTableView;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = MyViewModel;
        }
    }

    private void SearchAutoSuggestBox_TextChanged_1(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {

    }

    private void SearchAutoSuggestBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {        
        MyViewModel.SearchSongSB_TextChanged(SearchTetxBox.Text);
    }
   
    private void MySongsTableView_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {

    }

    private void ProcessCellClick(bool isExclusion)
    {
        return;
        // 1. Check if we have a valid cell location from the CurrentCellChanged event.
        if (_lastActiveCellSlot.Equals(default(TableViewCellSlot)))
        {
            Debug.WriteLine("[ProcessCellClick] Aborted: _lastActiveCellSlot is not set.");
            return;
        }

        // 2. Use the TableView's own API to get the content.
        string tableViewContent = MySongsTableView.GetCellsContent(
            slots: new[] { _lastActiveCellSlot },
            includeHeaders: true
        );

        Debug.WriteLine($"[ProcessCellClick] GetCellsContent returned: \"{tableViewContent?.Replace("\n", "\\n")}\"");

        // 3. --- NEW, MORE ROBUST VALIDATION ---
        // First, check if the string is fundamentally empty.
        if (string.IsNullOrWhiteSpace(tableViewContent))
        {
            Debug.WriteLine("[ProcessCellClick] Aborted: tableViewContent is null or whitespace.");
            return;
        }

        // Second, split the content to ensure we have BOTH a header and a value.
        var parts = tableViewContent.Split(new[] { '\n' }, 2);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            // This is the critical check. If parts[1] is empty, it means the cell
            // had no value, and we should not proceed.
            Debug.WriteLine("[ProcessCellClick] Aborted: Cell value is empty. No clause generated.");
            return;
        }
        // --- END OF NEW VALIDATION ---

        // 4. Use your existing TQL converter.
        string tqlClause = TqlConverter.ConvertTableViewContentToTql(tableViewContent);
        if (string.IsNullOrEmpty(tqlClause))
        {
            Debug.WriteLine($"[ProcessCellClick] Aborted: TqlConverter failed to convert content.");
            return;
        }

        Debug.WriteLine($"[ProcessCellClick] Generated TQL Clause: \"{tqlClause}\" | IsExclusion: {isExclusion}");

        // 5. Call the ViewModel to update the query.
        MyViewModel?.UpdateQueryWithClause(tqlClause, isExclusion);
    }

    private void OpenFileExplorer_Click(object sender, RoutedEventArgs e)
    {
        //MyViewModel.OpenAndSelectFileInExplorer()
    }

    private void AddToEnd_Click(object sender, RoutedEventArgs e)
    {
        //Command = "{x:Bind MyViewModel.AddListOfSongsToQueueEndCommand}"
        //                CommandParameter = "{x:Bind MySongsTableView.ItemsSource}"
        Debug.WriteLine(MySongsTableView.Items.GetType());
        var firstTen = MySongsTableView.Items.Take(10) as IEnumerable<SongModelView>;

        Debug.WriteLine(firstTen is null);
        //MyViewModel.AddListOfSongsToQueueEnd();
    }

    private void AddToNext_Click(object sender, RoutedEventArgs e)
    {

        //MyViewModel.AddToNext()
    }
}
