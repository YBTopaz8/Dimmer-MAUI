using System.Runtime.InteropServices.WindowsRuntime;

using CommunityToolkit.WinUI;

using Microsoft.Graphics.Canvas.Effects;

using Windows.UI.Text;

using static Dimmer.DimmerSearch.TQlStaticMethods;

using AnimationStopBehavior = Microsoft.UI.Composition.AnimationStopBehavior;
using Border = Microsoft.UI.Xaml.Controls.Border;
using Button = Microsoft.UI.Xaml.Controls.Button;
using CheckBox = Microsoft.UI.Xaml.Controls.CheckBox;
using Colors = Microsoft.UI.Colors;
using DragStartingEventArgs = Microsoft.UI.Xaml.DragStartingEventArgs;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Image = Microsoft.UI.Xaml.Controls.Image;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using ScalarKeyFrameAnimation = Microsoft.UI.Composition.ScalarKeyFrameAnimation;
using Visibility = Microsoft.UI.Xaml.Visibility;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllSongsListPage : Page
{

    public AllSongsListPage()
    {
        InitializeComponent();

        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        // TODO: load from user settings or defaults
        _userPrefAnim = SongTransitionAnimation.Spring;
    }
    BaseViewModelWin MyViewModel { get; set; }

    private TableViewCellSlot _lastActiveCellSlot;



    private void MyPageGrid_Loaded(object sender, RoutedEventArgs e)
    {

        if (_storedSong != null)
        {
            var songToAnimate = _storedSong;


            _storedSong = null;

            MySongsTableView.ScrollIntoView(songToAnimate, ScrollIntoViewAlignment.Default);
            var myTableViewUIElem = MySongsTableView as UIElement;
            myTableViewUIElem.UpdateLayout();


            DispatcherQueue.TryEnqueue(() =>
            {


                var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");

                if (animation is null)
                    return;

                var row = MySongsTableView.ContainerFromItem(songToAnimate) as FrameworkElement;
                if (row is null)
                    return;

                var image = PlatUtils.FindVisualChild<Image>(row, "coverArtImage");
                if (image is null)
                    return;

                var animConf = new Microsoft.UI.Xaml.Media.Animation.GravityConnectedAnimationConfiguration();
                
                animConf.IsShadowEnabled = true;
                
                animation.Configuration = animConf;

                animation.TryStart(image);



            });
        }
    }


    private void CoverImageGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

      
        //Frame?.Navigate(songDetailType, _storedSong, supNavTransInfo);
    }

    private void MyPageGrid_Unloaded(object sender, RoutedEventArgs e)
    {

    }




    private readonly Microsoft.UI.Composition.Visual _rootVisual;

    private void ButtonHover_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        
     UIControlsAnims.AnimateBtnPointerEntered((Button)sender, _compositor);
    }

    private void ButtonHover_PointerExited(object sender, PointerRoutedEventArgs e)
    {
     UIControlsAnims.AnimateBtnPointerExited((Button)sender, _compositor);
     
    }
    private void CardBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        cardBorder = (sender as Border)!;
        cardBorder.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(15);
        StartHoverDelay();
    }

    private void CardBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (cardBorder is null) return;
        cardBorder.CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12);

        CancelHover();
    }

    private bool _isHovered;
    private bool _isAnimating;
    Border cardBorder;

    private void StartHoverDelay()
    {

        AnimateExpand(cardBorder);

    }



    private void CancelHover()
    {
        AnimateCollapse();
        Debug.WriteLine("Hover exited!");
    }

    private async void AnimateExpand(Border card)
    {
        try
        {
            await card.DispatcherQueue.EnqueueAsync(() => { });
            var compositor = ElementCompositionPreview.GetElementVisual(card).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(card);

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, new Vector3(1.05f));
            scale.Duration = TimeSpan.FromMilliseconds(250);
            rootVisual.CenterPoint = new Vector3((float)card.ActualWidth / 2, (float)card.ActualHeight / 2, 0);
            rootVisual.StartAnimation("Scale", scale);

            var extraPanel = card.FindName("ExtraPanel") as StackPanel
                        ?? PlatUtils.FindChildOfType<StackPanel>(card);

            if (extraPanel == null)
            {
                Debug.WriteLine("ExtraPanel not found yet – skipping animation");
                return;
            }


            var extraPanelVisual = ElementCompositionPreview.GetElementVisual(extraPanel);
            extraPanelVisual.Opacity = 0f;



            extraPanelVisual.StopAnimation("Opacity");
            extraPanelVisual.StopAnimation("Offset");

            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 1f);
            fade.Duration = TimeSpan.FromMilliseconds(200);

            var slide = compositor.CreateVector3KeyFrameAnimation();
            slide.InsertKeyFrame(0f, new Vector3(0, 20, 0));
            slide.InsertKeyFrame(1f, Vector3.Zero);
            slide.Duration = TimeSpan.FromMilliseconds(200);

            extraPanelVisual.StartAnimation("Opacity", fade);
            extraPanelVisual.StartAnimation("Offset", slide);

            await Task.Delay(250);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnimateExpand Exception: {ex.Message}");
        }
    }


    private void ApplyDepthZoomEffect(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // Build the effect graph (Win2D-based)
        var blurEffect = new GaussianBlurEffect
        {
            Name = "Blur",
            BlurAmount = 15f,
            BorderMode = EffectBorderMode.Hard,
            Source = new Windows.UI.Composition.CompositionEffectSourceParameter("Backdrop")
        };

        // Create the brush
        var effectFactory = compositor.CreateEffectFactory(blurEffect);
        var backdropBrush = compositor.CreateBackdropBrush();
        var effectBrush = effectFactory.CreateBrush();
        effectBrush.SetSourceParameter("Backdrop", backdropBrush);

        // Apply to a sprite visual
        var sprite = compositor.CreateSpriteVisual();
        sprite.Brush = effectBrush;
        sprite.Size = new Vector2((float)element.RenderSize.Width, (float)element.RenderSize.Height);
        ElementCompositionPreview.SetElementChildVisual(element, sprite);

        // Add zoom effect
        visual.CenterPoint = new Vector3((float)element.RenderSize.Width / 2, (float)element.RenderSize.Height / 2, 0);
        visual.Scale = new Vector3(0.85f);

        var zoom = compositor.CreateVector3KeyFrameAnimation();
        zoom.InsertKeyFrame(1f, Vector3.One);
        zoom.Duration = TimeSpan.FromMilliseconds(400);
        visual.StartAnimation("Scale", zoom);
    }



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
        var songs = MySongsTableView.Items;
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
        var isSongInList = MySongsTableView.CollectionView;

       
        MySongsTableView.ScrollIntoView(songToFind, ScrollIntoViewAlignment.Leading);
    }

    public Microsoft.UI.Xaml.Data.ICollectionView? GetCurrentVisibleItems()
    {
        Microsoft.UI.Xaml.Data.ICollectionView? visibleItems = MySongsTableView.CollectionView;
        return visibleItems;
    }

    private void ScrollToSong_Click(object sender, RoutedEventArgs e)
    {
        ScrollToSong(MyViewModel.CurrentPlayingSongView);
    }

    private void MySongsTableView_Sorting(object sender, TableViewSortingEventArgs e)
    {
        Debug.WriteLine(e.Column?.Header);
        Debug.WriteLine(e.Column?.Order);
        // later, log it in vm

        

        Debug.WriteLine($"SORT: {e.Column?.Header} → {e.Column?.Order}");
    }
    private void OnSorting(object? sender, TableViewSortingEventArgs e)
    {
        string tqlSort = e.Column.Order switch
        {
            0 => $"asc {e.Column.Header.ToString()?.ToLower()}",
            1 => $"desc {e.Column.Header.ToString()?.ToLower()}",
            _ => string.Empty
        };
        Debug.WriteLine($"TQL from sort: {tqlSort}");
        ApplyTql(tqlSort);

    }
    private void ApplyTql(string tql)
    {
        var currentQuery = MyViewModel.CurrentTqlQuery ?? "";

        // Combine old + new
        var combined = string.Join(" ", currentQuery, tql).Trim();

        // Regex patterns for sorting/shuffling directives
        var sortPattern = @"\b(asc|desc)\s+\w+\b";
        var firstLastPattern = @"\b(first|last)\s+\d+\b";
        var shufflePattern = @"\b(shuffle|random)(\s+\d+)?\b";

        // Collect all matches
        var allMatches = System.Text.RegularExpressions.Regex.Matches(combined,
            $"{sortPattern}|{firstLastPattern}|{shufflePattern}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        string? lastDirective = null;

        if (allMatches.Count > 0)
        {
            // Keep only the last one
            lastDirective = allMatches[allMatches.Count - 1].Value;
            // Remove all directives from combined string
            combined = System.Text.RegularExpressions.Regex.Replace(combined,
                $"{sortPattern}|{firstLastPattern}|{shufflePattern}", "").Trim();
        }

        // Append last directive to end if exists
        if (!string.IsNullOrWhiteSpace(lastDirective))
            combined = $"{combined} {lastDirective}".Trim();

        MyViewModel.CurrentTqlQuery = combined;

        // Re-run TQL query
        MyViewModel.SearchSongForSearchResultHolder(combined);
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
        MyViewModel.SearchSongForSearchResultHolder(text);
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
                    range.CharacterFormat.Bold = Microsoft.UI.Text.FormatEffect.On; // Make keywords bold
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




    private void MySongsTableView_CellContextFlyoutOpening(object sender, TableViewCellContextFlyoutEventArgs e)
    {
        e.Handled = true;
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

        //    var properties = e.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement).Properties;


        //    if (properties.IsXButton1Pressed)
        //    {
        //    }
        //    else if (properties.IsXButton2Pressed)
        //    {

        //    }

        //var isCtlrKeyPressed = e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
        //       (Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control) &
        //        Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        //    if (isCtlrKeyPressed)
        //        ProcessCellClick(isExclusion: false);

        switch (MySongsTableView.SelectionMode)
        {
            case Microsoft.UI.Xaml.Controls.ListViewSelectionMode.None:
                break;
            case Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Single:

                if (e.AddedCells.Count > 0)
                {
                    var currSelection = e.AddedCells[0];
                    var selectedColumnField = MySongsTableView.Columns[currSelection.Column].Header as string;

                    var currentCellValue = MySongsTableView.GetCellsContent(e.AddedCells, false);
                    // now i have the field and value, if it's album/artist then we want to update query to find all of said album/artist
                    if (!string.IsNullOrEmpty(selectedColumnField) && !string.IsNullOrEmpty(currentCellValue))
                    {
                        string tqlQuery = string.Empty;
                        switch (selectedColumnField)
                        {
                            case "Album":
                                tqlQuery = PresetQueries.ByAlbum(currentCellValue);
                                MyViewModel.SearchSongForSearchResultHolder(tqlQuery);
                                break;
                            case "Artist":
                                tqlQuery = PresetQueries.ByArtist(currentCellValue);
                                MyViewModel.SearchSongForSearchResultHolder(tqlQuery);
                                break;
                            case "Genre":
                                tqlQuery = PresetQueries.ByGenre(currentCellValue);
                                MyViewModel.SearchSongForSearchResultHolder(tqlQuery);
                                break;
                            default:
                                break;
                        }
                    }
                }
                break;
            case Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Multiple:
                break;
            case Microsoft.UI.Xaml.Controls.ListViewSelectionMode.Extended:
                break;
            default:
                break;
        }

    }

    private void MySongsTableView_RowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
    {

    }





    private void MySongsTableView_Loading(FrameworkElement sender, object args)
    {

    }

    private void MySongsTableView_Loaded(object sender, RoutedEventArgs e)
    {

        MyViewModel.MySongsTableView = MySongsTableView;

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

            MyViewModel.SearchSongForSearchResultHolder(text);
            // Handle Enter key press
            Debug.WriteLine("Enter key pressed.");
            return;
            // You can trigger your search or any other action here

        }
        MyViewModel.SearchSongForSearchResultHolder(text);
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
        var selectedSong = (sender as FrameworkElement)?.DataContext as SongModelView;

        // Store the item for the return trip
        _storedSong = selectedSong;

        // Find the specific UI element (the Image) that was clicked on
        var row = MySongsTableView.ContainerFromItem(selectedSong) as FrameworkElement;
        var image = PlatUtils.FindVisualChild<Image>(row, "coverArtImage");
        if (image == null) return;

        // Prepare the animation, linking the key "ForwardConnectedAnimation" to our image
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", image);


        // Small visual feedback before navigation
        var visual = ElementCompositionPreview.GetElementVisual(image);
        switch (SongTransitionAnimation.Slide)
        {
            case SongTransitionAnimation.Fade:
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(0f, 0.3f);
                fade.InsertKeyFrame(1f, 0f);
                fade.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
                visual.CenterPoint = new Vector3(
                    (float)(image.ActualWidth / 2),
                    (float)(image.ActualHeight / 2),
                    0);
                scaleAnim.InsertKeyFrame(0f, new Vector3(1f));
                scaleAnim.InsertKeyFrame(1f, new Vector3(1.15f));
                scaleAnim.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Scale", scaleAnim);
                break;

            case SongTransitionAnimation.Slide:
                var offsetAnim = _compositor.CreateVector3KeyFrameAnimation();
                offsetAnim.InsertKeyFrame(0f, Vector3.Zero);
                offsetAnim.InsertKeyFrame(1f, new Vector3(30f, 0f, 0f));
                offsetAnim.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Offset", offsetAnim);
                break;

            case SongTransitionAnimation.Spring:
            default:
                var spring = _compositor.CreateSpringVector3Animation();
                spring.DampingRatio = 0.7f;
                spring.Period = TimeSpan.FromMilliseconds(200);
                spring.FinalValue = new Vector3(0, -25, 0);
                visual.StartAnimation("Offset", spring);
                break;
        }


        // Navigate to the detail page, passing the selected song object.
        // Suppress the default page transition to let ours take over.
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        var navParams = new SongDetailNavArgs
        {
            Song = _storedSong!,
            ViewModel = MyViewModel
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);
    }

    private async void MySongsTableView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        Debug.WriteLine(e.Pointer.PointerId);
        Microsoft.UI.Input.PointerPointProperties? pointerProps = e.GetCurrentPoint(null).Properties;

        if (pointerProps == null)
        {
            return;
        }
        var updateKind = pointerProps.PointerUpdateKind;
        // if it is a middle click, exclude in tql explictly
        
        if (updateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased
            || updateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonPressed)
        {

            await MyViewModel.ScrollToCurrentPlayingSongCommand.ExecuteAsync(null);


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

    private void MySongsTableView_DragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {

    }


    private readonly Microsoft.UI.Composition.Compositor _compositor;
    private readonly SongTransitionAnimation _userPrefAnim;
    private SongModelView? _storedSong;


    string CurrentPageTQL = string.Empty;
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

            MyViewModel.IsBackButtonVisible = WinUIVisibility.Collapsed;


            MyViewModel.CurrentWinUIPage = this;
            MyViewModel.MySongsTableView = MySongsTableView;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = MyViewModel;
        }

        if (CurrentPageTQL != MyViewModel.CurrentTqlQuery)
        {
            MyViewModel.SearchSongForSearchResultHolder(CurrentPageTQL);
        }
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);


        CurrentPageTQL = MyViewModel.CurrentTqlQuery;
    }




    private void ApplyReturnFloatie(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // set starting offset slightly below
        visual.Offset = new Vector3(0, 40, 0);

        // spring animation toward neutral position
        var spring = compositor.CreateSpringVector3Animation();
        spring.FinalValue = Vector3.Zero;
        spring.DampingRatio = 0.55f;                     // how “bouncy” it feels
        spring.Period = TimeSpan.FromMilliseconds(280); // shorter = snappier
        spring.InitialValue = new Vector3(0, 40, 0);     // make sure start matches
        spring.StopBehavior = AnimationStopBehavior.SetToFinalValue;

        visual.StartAnimation("Offset", spring);
    }

    private void SearchAutoSuggestBox_TextChanged_1(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {

    }

    private void SearchAutoSuggestBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(SearchTextBox.Text);
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

    private void ExtraPanel_Loaded(object sender, RoutedEventArgs e)
    {
        _ = ElementCompositionPreview.GetElementVisual((UIElement)sender);

    }


    private void ViewSongBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedSong = (sender as FrameworkElement)?.DataContext as SongModelView;

        // Store the item for the return trip
        _storedSong = selectedSong;

        // Find the specific UI element (the Image) that was clicked on
        var row = MySongsTableView.ContainerFromItem(selectedSong) as FrameworkElement;
        var image = PlatUtils.FindVisualChild<Image>(row, "coverArtImage");
        if (image == null) return;

        // Prepare the animation, linking the key "ForwardConnectedAnimation" to our image
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ForwardConnectedAnimation", image);


        // Small visual feedback before navigation
        var visual = ElementCompositionPreview.GetElementVisual(image);
        switch (SongTransitionAnimation.Slide)
        {
            case SongTransitionAnimation.Fade:
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(0f, 0.3f);
                fade.InsertKeyFrame(1f, 0f);
                fade.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
                visual.CenterPoint = new Vector3(
                    (float)(image.ActualWidth / 2),
                    (float)(image.ActualHeight / 2),
                    0);
                scaleAnim.InsertKeyFrame(0f, new Vector3(1f));
                scaleAnim.InsertKeyFrame(1f, new Vector3(1.15f));
                scaleAnim.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Scale", scaleAnim);
                break;

            case SongTransitionAnimation.Slide:
                var offsetAnim = _compositor.CreateVector3KeyFrameAnimation();
                offsetAnim.InsertKeyFrame(0f, Vector3.Zero);
                offsetAnim.InsertKeyFrame(1f, new Vector3(30f, 0f, 0f));
                offsetAnim.Duration = TimeSpan.FromMilliseconds(150);
                visual.StartAnimation("Offset", offsetAnim);
                break;

            case SongTransitionAnimation.Spring:
            default:
                var spring = _compositor.CreateSpringVector3Animation();
                spring.DampingRatio = 0.7f;
                spring.Period = TimeSpan.FromMilliseconds(200);
                spring.FinalValue = new Vector3(0, -25, 0);
                visual.StartAnimation("Offset", spring);
                break;
        }


        // Navigate to the detail page, passing the selected song object.
        // Suppress the default page transition to let ours take over.
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        var navParams = new SongDetailNavArgs
        {
            Song = _storedSong!,
            ViewModel = MyViewModel
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);
    }
    private void ViewOtherBtn_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel.SelectedSong = (SongModelView)((Button)sender).DataContext;

    }



    private ScalarKeyFrameAnimation _fadeInAnim;
    private ScalarKeyFrameAnimation _fadeOutAnim;
    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {

    }


    private void ViewOtherBtn_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

    }

    private void QuickTQLBtn_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

    }

    private void QuickTQLBtn_Click(object sender, RoutedEventArgs e)
    {

    }


    private ConnectedAnimationConfiguration? GetConfiguration()
    {
        var listOfNames = new List<string>
        {
            "Gravity",
            "Direct",
            "Basic",
        };
        var randomNameFromList = new Random();
        var selectedName = listOfNames[randomNameFromList.Next(listOfNames.Count)];


        switch (selectedName)
        {
            case "Gravity":
                return new GravityConnectedAnimationConfiguration();
            case "Direct":
                return new DirectConnectedAnimationConfiguration();
            case "Basic":
                return new BasicConnectedAnimationConfiguration();
            default:
                return null;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {

    }


    private async void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

        try
        {

            var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
            var properties = e.GetCurrentPoint(nativeElement).Properties;



            var point = e.GetCurrentPoint(nativeElement);
            MyViewModel.SelectedSong = ((Grid)sender).DataContext as SongModelView;
            _storedSong = ((Grid)sender).DataContext as SongModelView;


            // Navigate to the detail page, passing the selected song object.
            // Suppress the default page transition to let ours take over.
            var supNavTransInfo = new SuppressNavigationTransitionInfo();
            Type pageType = typeof(ArtistPage);
            var navParams = new SongDetailNavArgs
            {
                Song = _storedSong!,
                ExtraParam = MyViewModel,
                ViewModel = MyViewModel
            };
            var contextMenuFlyout = new MenuFlyout();

            var namesOfartists = _storedSong.ArtistToSong.Select(a => a.Name).ToList();

            bool isSingular = namesOfartists.Count > 1 ? true : false;
            string artistText = string.Empty;
            if (isSingular)
            {
                artistText = "artist";
            }
            else
            {
                artistText = "artists";
            }
            contextMenuFlyout.Items.Add(
                new MenuFlyoutItem
                {
                    Text = $"{namesOfartists.Count} {artistText} linked",
                    IsTapEnabled = false

                });

            foreach (var artistName in namesOfartists)
            {
                var root = new MenuFlyoutItem { Text = artistName };

                root.Click += async (obj, routedEv) =>
                {

                    var songContext = ((MenuFlyoutItem)obj).Text;

                    ArtistModelView? selectedArtist = _storedSong.ArtistToSong.First(x => x.Name == songContext);


                    var nativeElementMenuFlyout = (Microsoft.UI.Xaml.UIElement)obj;
                    var propertiesMenuFlyout = e.GetCurrentPoint(nativeElementMenuFlyout).Properties;
                    if (propertiesMenuFlyout.IsRightButtonPressed)
                    {
                        MyViewModel.SearchSongForSearchResultHolder(PresetQueries.ByArtist(selectedArtist.Name))
                        ;
                    }
                    await MyViewModel.SetSelectedArtist(selectedArtist);


                    FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                    {
                        TransitionInfoOverride = supNavTransInfo,
                        IsNavigationStackEnabled = true

                    };
                    // prepare the animation BEFORE navigation
                    var ArtistNameTxt = PlatUtils.FindVisualChild<TextBlock>((UIElement)sender, "ArtistNameTxt");
                    if (ArtistNameTxt != null)
                    {
                        ConnectedAnimationService.GetForCurrentView()
                            .PrepareToAnimate("ForwardConnectedAnimation", ArtistNameTxt);
                    }

                    Frame?.NavigateToType(pageType, navParams, navigationOptions);
                };

                contextMenuFlyout.Items.Add(root);
            }


            try
            {
                if (namesOfartists.Count > 1)
                {
                    contextMenuFlyout.ShowAt(nativeElement, point.Position);
                }
                else
                {
                    await MyViewModel.SetSelectedArtist(_storedSong.ArtistToSong.FirstOrDefault());


                    if (properties.IsRightButtonPressed)
                    {
                        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(_storedSong.ArtistToSong.First()!.Name!));
                        return;
                    }

                    FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                    {
                        TransitionInfoOverride = supNavTransInfo,
                        IsNavigationStackEnabled = true

                    };
                    // prepare the animation BEFORE navigation
                    var ArtistNameTxt = PlatUtils.FindVisualChild<TextBlock>((UIElement)sender, "ArtistNameTxt");
                    if (ArtistNameTxt != null)
                    {
                        ConnectedAnimationService.GetForCurrentView()
                            .PrepareToAnimate("ForwardConnectedAnimation", ArtistNameTxt);
                    }

                    Frame?.NavigateToType(pageType, navParams, navigationOptions);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuFlyout.ShowAt failed: {ex.Message}");
                // fallback: anchor without position
                //flyout.ShowAt(nativeElement);
            }


        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    public async void RunBtn_Click(object sender, RoutedEventArgs e)
    {

        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(ArtistPage);
        var navParams = new SongDetailNavArgs
        {
            Song = MyViewModel.CurrentPlayingSongView!,
            ViewModel = MyViewModel
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };


        await DispatcherQueue.EnqueueAsync(() =>
        {

            Frame?.NavigateToType(songDetailType, navParams, navigationOptions);
        });
    }

    private void MySongsTableView_Loaded_1()
    {

    }


    private void NowPlayingQueueExpander_Loaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void Animated_ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {

    }

    private async void PlaySongBtn_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        var send = (FrameworkElement)sender;
        var song = send.DataContext as SongModelView;
        if (MyViewModel.PlaybackQueue.Count < 1)
        {
            //MyViewModel.SearchSongForSearchResultHolder(">>addnext!");
        }
        await MyViewModel.PlaySong(song, CurrentPage.HomePage);
    }

    private void NowPlayingQueueExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        SidePanelForSelectedSong.Visibility= Visibility.Visible;
        SidePanelForSelectedSong.Width = 600;


        //PlayPauseImg.Source = new SvgImageSource(new Uri(uri));

    }

    private void AnimatedScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {
        //Button SelectedItem = GetSelectedItemFromViewport() as Button;
        
        //var song = SelectedItem.DataContext as SongModelView;

    }

    private async void AnimateScaleControlUp(FrameworkElement btn)
    {
        try
        {
            var song= btn.DataContext as SongModelView;
            if(song is null) return;
            

            await btn.DispatcherQueue.EnqueueAsync(() => { });
            var compositor = ElementCompositionPreview.GetElementVisual(btn).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(btn);

            var scale = compositor.CreateVector3KeyFrameAnimation();
            scale.InsertKeyFrame(1f, new Vector3(1.05f));
            scale.Duration = TimeSpan.FromMilliseconds(350);
            rootVisual.CenterPoint = new Vector3((float)btn.ActualWidth / 2, (float)btn.ActualHeight / 2, 0);
            rootVisual.StartAnimation("Scale", scale);

            if (song.CoverImagePath is not null)
            {

                var img = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(song.CoverImagePath, UriKind.Absolute));
                FocusedSongImage.Source = img;
            }
            FocusedSongTextBlockTitle.Content = song.Title;
            FocusedSongTextBlockArtistName.Content = song.ArtistName;
            FocusedSongTextBlockAlbumName.Content = song.AlbumName;
            FocusedSongTextBlockGenre.Content = song.GenreName;
            FocusedSongTextBlockHasSyncLyrics.Content = song.HasSyncedLyrics ? "Has Synced Lyrics" : "No Synced Lyrics";
            FocusedSongTextBlockIsFav.Content = song.IsFavorite ? "Favorite Song" : "Not Favorite";
            FocusedSongTextBlockIsFav.Visibility = song.IsFavorite ? Visibility.Visible : Visibility.Collapsed;
            FocusedSongTextBlockLastTimePlayed.Content = song.LastPlayed != null ? $"Last Played: {song.LastPlayed.Value.ToLocalTime().ToString("g")}" : "Never Played";

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AnimateExpand Exception: {ex.Message}");
        }
    }

    private void AnimateCollapseControlDown(FrameworkElement framework)
    {
        try
        {

            // collapse animation (optional)
            var compositor = ElementCompositionPreview.GetElementVisual(framework).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(framework);
            var scaleBack = compositor.CreateVector3KeyFrameAnimation();
            scaleBack.InsertKeyFrame(1f, new Vector3(1f));
            scaleBack.Duration = TimeSpan.FromMilliseconds(300);
            rootVisual.StartAnimation("Scale", scaleBack);

        }
        catch (Exception ex)
        {

            Debug.WriteLine($"AnimateCollapse Exception: {ex.Message}");
        }
    }
    private void ApplyEntranceEffect(FrameworkElement frameElt, SongTransitionAnimation defAnim = SongTransitionAnimation.Spring)
    {
        
        var visual = ElementCompositionPreview.GetElementVisual(frameElt);

        
        switch (defAnim)
        {
            case SongTransitionAnimation.Fade:
                visual.Opacity = 0f;
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(1f, 1f);
                fade.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                visual.CenterPoint = new Vector3((float)frameElt.ActualWidth / 2,
                                                 (float)frameElt.ActualHeight / 2, 0);
                visual.Scale = new Vector3(0.8f);
                var scale = _compositor.CreateVector3KeyFrameAnimation();
                scale.InsertKeyFrame(1f, Vector3.One);
                scale.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Scale", scale);
                break;

            case SongTransitionAnimation.Slide:
                visual.Offset = new Vector3(80f, 0, 0);
                var slide = _compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(1f, Vector3.Zero);
                slide.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Offset", slide);
                break;

            case SongTransitionAnimation.Spring:
            default:
                var spring = _compositor.CreateSpringVector3Animation();
                spring.FinalValue = new Vector3(0, 0, 0);
                spring.DampingRatio = 0.5f;
                spring.Period = TimeSpan.FromMilliseconds(350);
                visual.Offset = new Vector3(0, 40, 0);//c matching
                visual.StartAnimation("Offset", spring);
                break;
        }
    }

    private void AnimateCollapse()
    {
        try
        {

            // collapse animation (optional)
            var compositor = ElementCompositionPreview.GetElementVisual(cardBorder).Compositor;
            var rootVisual = ElementCompositionPreview.GetElementVisual(cardBorder);
            var scaleBack = compositor.CreateVector3KeyFrameAnimation();
            scaleBack.InsertKeyFrame(1f, new Vector3(1f));
            scaleBack.Duration = TimeSpan.FromMilliseconds(200);
            rootVisual.StartAnimation("Scale", scaleBack);

            var extraPanel = (StackPanel)cardBorder.FindName("ExtraPanel");
            var visual = ElementCompositionPreview.GetElementVisual(extraPanel);
            var fade = compositor.CreateScalarKeyFrameAnimation();
            fade.InsertKeyFrame(1f, 0f);
            fade.Duration = TimeSpan.FromMilliseconds(200);
            visual.StartAnimation("Opacity", fade);

        }
        catch (Exception ex)
        {

            Debug.WriteLine($"AnimateCollapse Exception: {ex.Message}");
        }
    }

    private void PlaySongBtn_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Button send = (Button)sender;
        if (!_isHovered) return;
        _isHovered = false;
        AnimateCollapseControlDown(send);
    }

 

    private void NowPlayingQueueExpander_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Debug.WriteLine(e.GetType());
        Debug.WriteLine(e.OldValue);
        Debug.WriteLine(e.OldValue.GetType());
        Debug.WriteLine(e.NewValue);
        Debug.WriteLine(e.NewValue.GetType());
    }

    private async void coverArtImage_DragStarting(UIElement sender, DragStartingEventArgs e)
    {
        var img = (Image)sender;
        var song = img.DataContext as SongModelView;
        if (song == null || string.IsNullOrWhiteSpace(song.CoverImagePath))
            return;

        e.Data.Properties["drag-origin"] = "dimmer-app";
        e.Data.Properties["drag-type"] = "song-item"; // identify item


        e.Data.Properties["drag-origin"] = "dimmer-app";
        e.Data.Properties["drag-type"] = "cover-image";
        e.Data.Properties["cover-path"] = ((SongModelView)((FrameworkElement)sender).DataContext).Id;

    }

    private void coverArtImage_DropCompleted(UIElement sender, Microsoft.UI.Xaml.DropCompletedEventArgs e)
    {
        //var data = e.DropResult;
        
        //Debug.WriteLine(data.GetType());
        //Debug.WriteLine(e.OriginalSource.GetType());
        //Debug.WriteLine(e.OriginalSource);
        //Debug.WriteLine(e.GetType());
        //// 1. Check if internal drag
        //if (data.Properties.TryGetValue("drag-origin", out var originObj) &&
        //    originObj is string origin &&
        //    origin == "dimmer-app")
        //{
        //    // INTERNAL drag
        //    data.Properties.TryGetValue("drag-type", out var dragTypeObj);
        //    var dragType = dragTypeObj as string;

        //    switch (dragType)
        //    {
        //        case "song-item":
        //            // REORDER QUEUE
        //            data.Properties.TryGetValue("item-id", out var idObj);
        //            var songId = (int)idObj;
        //            QueueService.Reorder(songId, e.GetPosition((UIElement)sender));
        //            return;

        //        case "cover-image":
        //            // ASSIGN COVER (internal)
        //            data.Properties.TryGetValue("cover-path", out var pathObj);
        //            AssignCoverToHoveredSong(pathObj as string);
        //            return;
        //    }

        //    return;
        //}

        //// 2. If external → check if files
        //if (data.Contains(StandardDataFormats.StorageItems))
        //{
        //    var items = await data.GetStorageItemsAsync();
        //    var file = items.FirstOrDefault() as StorageFile;
        //    if (file != null)
        //    {
        //        if (IsImageFile(file))
        //        {
        //            // EXTERNAL image → assign cover
        //            var newCoverPath = await SaveExternalCover(file);
        //            AssignCoverToHoveredSong(newCoverPath);
        //            return;
        //        }
        //    }
        //}

        //// 3. External stuff you don’t want → OS drop
        //OSHandleExternalDrop(e);
    }

    private void StackPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var prop = e.GetCurrentPoint((UIElement)sender).Properties;
        if(prop != null)
        {
            if(prop.IsMiddleButtonPressed)
            {
                MyViewModel.ScrollToCurrentPlayingSongCommand.Execute(null);
                
            }
        }
    }

    private void CardBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }

    private void ViewSongBtn_Click_1(object sender, RoutedEventArgs e)
    {

    }

    private void ViewQueueStackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props= e.GetCurrentPoint((UIElement)sender).Properties;
        if(props.IsRightButtonPressed)
        {
            NowPlayingQueueExpander.IsExpanded = false;
            ViewQueueStackPanel.Visibility = Visibility.Collapsed;
            ViewQueue.Content = "View Queue";
        }
    }

    private void BorderOfSongInPBQueue_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var btn = (FrameworkElement)sender;

        AnimateScaleControlUp(btn);

    }

    private void BorderOfSongInPBQueue_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        Border send = (Border)sender;
        UIControlsAnims.AnimateBorderPointerExited(send, _compositor);
    }

    private void CardBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ViewSongBtn_Click(sender, e);
    }

    private void animatedScrollRepeater_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
       
        var currentList = e.AddedItems as IList<object>;
        var current = currentList?.FirstOrDefault() as Dimmer.Data.ModelView.LyricPhraseModelView;
        if (current != null)
        {
            var pastList = e.RemovedItems as IReadOnlyList<object>;
            if (pastList is not null)
            {
                if (pastList.Count > 0 && pastList?[0] is Dimmer.Data.ModelView.LyricPhraseModelView past)
                {
                    past?.NowPlayingLyricsFontSize = 19;
                    past?.HighlightColor = Microsoft.Maui.Graphics.Colors.White;
                    past?.IsHighlighted = false;
                }
            }
            current?.NowPlayingLyricsFontSize = 29;
            current?.IsHighlighted = true;
            current?.HighlightColor = Microsoft.Maui.Graphics.Colors.SlateBlue;

            DispatcherQueue.TryEnqueue(() =>
            {
                var container = animatedScrollRepeater.ContainerFromItem(current) as ListViewItem;
                if (container != null)
                {
                    container.UpdateLayout();
                    container.StartBringIntoView(new BringIntoViewOptions
                    {
                        AnimationDesired = true,
                        VerticalAlignmentRatio = 0.5 // 0 = top, 0.5 = center
                    });
                }

            });

        }

    }
}
