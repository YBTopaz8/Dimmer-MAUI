using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using CommunityToolkit.WinUI;

using Dimmer.Utilities.Extensions;
using Dimmer.WinUI.Views.CustomViews.WinuiViews;
using DynamicData.Binding;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Controls.Primitives;

using Windows.Foundation.Metadata;
using Windows.UI.Text;

using static Dimmer.DimmerSearch.TQlStaticMethods;

using AnimationStopBehavior = Microsoft.UI.Composition.AnimationStopBehavior;
using Border = Microsoft.UI.Xaml.Controls.Border;
using CheckBox = Microsoft.UI.Xaml.Controls.CheckBox;
using DragStartingEventArgs = Microsoft.UI.Xaml.DragStartingEventArgs;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Panel = Microsoft.UI.Xaml.Controls.Panel;
using ScalarKeyFrameAnimation = Microsoft.UI.Composition.ScalarKeyFrameAnimation;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;
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
            
            MySongsTableView.SmoothScrollIntoViewWithItemAsync(songToAnimate, (ScrollItemPlacement)ScrollIntoViewAlignment.Default);
            var myTableViewUIElem = MySongsTableView as UIElement;
            myTableViewUIElem.UpdateLayout();


            AnimationHelper.PrepareFromList(
    MySongsTableView, 
    _storedSong, 
    "coverArtImage", 
    AnimationHelper.Key_Forward
);
        }
    }


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

    Border cardBorder;

    private void StartHoverDelay()
    {

        AnimateExpand(cardBorder);

    }



    private void CancelHover()
    {
        AnimateCollapse();
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

    private void TableView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        
    }
    private void TableView_PointerPressed(object sender, PointerRoutedEventArgs e)
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
        FrameworkElement element = (e.OriginalSource as FrameworkElement)!;
        SongModelView? song = null;
        if (element == null)
            return;



        while (element != null && element != (FrameworkElement)sender)
        {
            if (element.DataContext is SongModelView currentSong)
            {
                song = currentSong;
                break; // Found it!
            }
            element = (FrameworkElement)element.Parent;
        }
        var songs = MySongsTableView.Items;
        Debug.WriteLine(songs.Count);


        // now we need items as enumerable of SongModelView

        var SongsEnumerable = songs.OfType<SongModelView>();

        Debug.WriteLine(SongsEnumerable.Count());


        if (song != null)
        {
            // Default behavior: Add to play next (non-interrupting)
            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            if (MyViewModel.CurrentPlayingSongView.TitleDurationKey is null)
            {
                await MyViewModel.PlaySongWithActionAsync(song, PlaybackAction.PlayNow
                    , SongsEnumerable);
            }
            else
            {
                await MyViewModel.PlaySongWithActionAsync(song, Dimmer.Utilities.Enums.PlaybackAction.PlayNext, SongsEnumerable);
            }
        }
    }
    public void ScrollToSong(SongModelView songToFind)
    {
        if (songToFind == null)
            return;
       
        MySongsTableView.SmoothScrollIntoViewWithItemAsync(songToFind, (ScrollItemPlacement)ScrollIntoViewAlignment.Leading);
    }

    public Microsoft.UI.Xaml.Data.ICollectionView? GetCurrentVisibleItems()
    {
        Microsoft.UI.Xaml.Data.ICollectionView? visibleItems = MySongsTableView.CollectionView;
        return visibleItems;
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

    private void MySongsTableView_Tapped(object sender, TappedRoutedEventArgs e)
    {
        return;
        var isCtlrKeyPressed = e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
            (Windows.UI.Core.CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control) &
             Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;
        if (isCtlrKeyPressed)
            ProcessCellClick(isExclusion: true);

    }

    public string Format(string format, object arg)
    {
        return string.Format(format, arg);
    }




    private void MySongsTableView_CellContextFlyoutOpening(object sender, TableViewCellContextFlyoutEventArgs e)
    {
        e.Handled = true;
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

    


    private void MySongsTableView_RowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
    {

    }






    private void MySongsTableView_Loaded(object sender, RoutedEventArgs e)
    {

        MyViewModel.MySongsTableView = MySongsTableView;

    }

    private void MySongsTableView_ExportSelectedContent(object sender, TableViewExportContentEventArgs e)
    {

    }

    private void SearchAutoSuggestBox_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
    {

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

    private void MySongsTableView_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    private void MySongsTableView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {

    }

    private void MySongsTableView_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {

    }


    private void MySongsTableView_LostFocus(object sender, RoutedEventArgs e)
    {

    }

   

    private void MySongsTableView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

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
            
            // Subscribe to playback feedback events
            MyViewModel.OnSongAddedToQueue += async (sender, message) =>
            {
                await ShowNotification(message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
            };
            
            MyViewModel.OnSongPlayingNow += async (sender, message) =>
            {
                await ShowNotification(message, Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational);
            };
            
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = MyViewModel;
        }

        //if (CurrentPageTQL != MyViewModel.CurrentTqlQuery)
        //{
        //    MyViewModel.SearchSongForSearchResultHolder(CurrentPageTQL);
        //}
        MyViewModel.CurrentWinUIPage = this;
    }

    protected override void OnNavigatedFrom(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);


        CurrentPageTQL = MyViewModel.CurrentTqlQuery;
    }

    private void SearchAutoSuggestBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(SearchTextBox.Text);
        PreviewText.Text = NaturalLanguageProcessor.Process(SearchTextBox.Text);
   
        //var text = SearchTextBox.Text.ToLower();

        //var matches = FieldRegistry.AllFields
        //    .Where(f => f.PrimaryName.StartsWith(text) || f.Aliases.Any(a => a.StartsWith(text)))
        //    .Select(f => $"{f.PrimaryName} ({string.Join(",", f.Aliases)})")
        //    .Take(10)
        //    .ToList();

        //SuggestList.ItemsSource = matches;
        //SuggestList.Visibility = matches.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void SortClick(object sender, RoutedEventArgs e)
    {
        var key = (sender as MenuFlyoutItem)?.Tag.ToString();
        //SortSongs(key);
    }

    private async void OpenHelp(object sender, RoutedEventArgs e)
    {
        var dlg = new SearchHelpDialog();
        await dlg.ShowAsync();
    }
 
    private void MySongsTableView_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {

    }

    private void ProcessCellClick(bool isExclusion, bool isAdditive = false) // Added isAdditive flag
    {
        // 1. Validation (Same as yours)
        if (_lastActiveCellSlot.Equals(default(TableViewCellSlot))) return;

        // 2. Get Content
        string tableViewContent = MySongsTableView.GetCellsContent(
            slots: new[] { _lastActiveCellSlot },
            includeHeaders: true
        );

        if (string.IsNullOrWhiteSpace(tableViewContent)) return;

        // 3. Robust Split & Empty Value Check
        var parts = tableViewContent.Split(new[] { '\n' }, 2);
        if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
        {
            // Important: If user clicks an empty "Genre" cell, we probably don't want to search "Genre:''"
            // Unless your TQL supports "Genre:null" or "Genre:empty" explicitly.
            Debug.WriteLine("[ProcessCellClick] Ignored empty cell.");
            return;
        }

        // 4. Convert to TQL
        string tqlClause = TqlConverter.ConvertTableViewContentToTql(tableViewContent);
        if (string.IsNullOrEmpty(tqlClause)) return;

        // 5. ViewModel Integration
        // Pass the intent: Are we excluding? Are we adding to existing filters?
        //MyViewModel?.UpdateQueryWithClause(tqlClause, isExclusion, isAdditive);
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
        var visualImage = ElementCompositionPreview.GetElementVisual(image);
        //PlatUtils.ApplyEntranceEffect(visualImage, row, SongTransitionAnimation.Slide,_compositor);

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
        var UIElt  = (UIElement)sender;

        var selectedSong = (SongModelView)((FrameworkElement)sender).DataContext;
        var menuFlyout = new MenuFlyout();
        var addNoteToSongMFItem = new MenuFlyoutItem { Text = "Add Note to Song" };
            addNoteToSongMFItem.Click += async (s, args) =>
            {
                await MyViewModel.AddNoteToSongAsync();
            };

        FontIcon iconNote = new FontIcon();
        iconNote.Glyph = "\uF7BB";
        addNoteToSongMFItem.Icon = iconNote;
        menuFlyout.Items.Add(addNoteToSongMFItem);


        var deleteSongFromLibraryMFItem = new MenuFlyoutItem { Text = "Delete Song from Device" };
        deleteSongFromLibraryMFItem.Click += async (s, args) =>
        {
            await MyViewModel.DeleteSongs(new List<SongModelView>() { selectedSong });
        };
        var menuSeparator = new MenuFlyoutSeparator();
        menuFlyout.Items.Add(menuSeparator);

        FontIcon iconDelete = new FontIcon();
        iconDelete.Glyph = "\uE74D";
        deleteSongFromLibraryMFItem.Icon=iconDelete;
        menuFlyout.Items.Add(deleteSongFromLibraryMFItem);



        var AddToNextMFItem = new MenuFlyoutItem { Text = "Add to Next" ,
        };
        AddToNextMFItem.Click += AddToNextMFItem_Click;
        FontIcon icon = new FontIcon();
        icon.Glyph = "\uE70E";
            AddToNextMFItem.Icon=icon;
        menuFlyout.Items.Add(AddToNextMFItem);

        FontIcon heartIcon = new FontIcon();
        heartIcon.Glyph = "\uEB51";

        FontIcon unheartIcon = new FontIcon();
        unheartIcon.Glyph = "\uEA92";
        var toggleFavBtn = new ToggleMenuFlyoutItem
        {
            Icon = heartIcon,
            
        };
        if (!selectedSong.IsFavorite)
        {
            toggleFavBtn.Text = "Love";
            toggleFavBtn.Click += (s, e) =>
            {

                _ = MyViewModel.AddFavoriteRatingToSong(selectedSong);
                toggleFavBtn.Text = "UnLove";
            };

        }
        else
        {
            toggleFavBtn.Text = "UnLove";
            toggleFavBtn.Icon = unheartIcon;

            toggleFavBtn.Click += (s, e) =>
            {
                _ = MyViewModel.RemoveSongFromFavorite(selectedSong);
                toggleFavBtn.Text = "Love";
            };
        }

        menuFlyout.Items.Add(toggleFavBtn);
            FlyoutShowOptions flyoutShowOpt = new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Top,
                ShowMode = FlyoutShowMode.Auto
            };

        FontIcon LocateIcon = new FontIcon();
        LocateIcon.Glyph = "\uE890";

        var locateSongInFolder = new MenuFlyoutItem()
        {
            Text = "Locate In Folder"
        ,
            Icon = LocateIcon
        };
        locateSongInFolder.Click += (s, e) =>
        {
            MyViewModel.OpenAndSelectFileInExplorer(selectedSong);
        };
        menuFlyout.Items.Add(locateSongInFolder);

        FontIcon LocateArtist = new FontIcon();
        LocateArtist.Glyph = "\uE720";
        var toArtistMFI = new MenuFlyoutItem()
        {
            Text = $"To Artist : {selectedSong.ArtistName}",
            Icon = LocateArtist
        };
        toArtistMFI.Click += (s, e) =>
        {
            var supNavTransInfo = new SuppressNavigationTransitionInfo();

            FrameNavigationOptions navigationOptions = new FrameNavigationOptions
            {
                TransitionInfoOverride = supNavTransInfo,
                IsNavigationStackEnabled = true

            };
            AnimationHelper.PrepareFromChild(
sender as DependencyObject,
"ArtistNameTxt",
AnimationHelper.Key_Forward
);
            var navParams = new SongDetailNavArgs
            {
                Song = _storedSong!,
                ExtraParam = MyViewModel,
                ViewModel = MyViewModel
            };
            Type pageType = typeof(ArtistPage);

            Frame?.NavigateToType(pageType, navParams, navigationOptions);

        };

        menuFlyout.Items.Add(toArtistMFI);

        FontIcon musicAlbumIcon = new FontIcon();
        musicAlbumIcon.Glyph = "\uE93C";

        var toAlbumMFI = new MenuFlyoutItem()
        {
            Text = $"To Album : {selectedSong.AlbumName}",
            Icon = musicAlbumIcon,
        };
        toAlbumMFI.Click += (s,e)=>
        {
            var supNavTransInfo = new SuppressNavigationTransitionInfo();

            FrameNavigationOptions navigationOptions = new FrameNavigationOptions
            {
                TransitionInfoOverride = supNavTransInfo,
                IsNavigationStackEnabled = true

            };
            AnimationHelper.PrepareFromChild(
sender as DependencyObject,
"ArtistNameTxt",
AnimationHelper.Key_Forward
); 
            var navParams = new SongDetailNavArgs
{
    Song = _storedSong!,
    ExtraParam = MyViewModel,
    ViewModel = MyViewModel
};
            Type pageType = typeof(AlbumPage);

            Frame?.NavigateToType(pageType, navParams, navigationOptions);
        };

        menuFlyout.Items.Add(toAlbumMFI);

        menuFlyout.ShowAt(UIElt, flyoutShowOpt);

    }

    private void AddToNextMFItem_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.AddToNext(new List<SongModelView>() { MyViewModel.SelectedSong! });
    }

    private SongModelView? _storedItem;

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {

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

    private void CardBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ViewSongBtn_Click(sender, e);
    }

    
    private void coverArtImage_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ViewOtherBtn_Click(sender, e);
    }

    private void HideBtmPart_Click(object sender, RoutedEventArgs e)
    {
        BtmLogPanel.Visibility = Visibility.Collapsed;
    }

    private void Animation_Completed(ConnectedAnimation sender, object args)
    {
        SmokeGrid.Visibility = WinUIVisibility.Collapsed;
    }
    private async void ViewQueue_Click(object sender, RoutedEventArgs e)
    {
        ConnectedAnimation? animation;

        FrameworkElement send = (FrameworkElement)sender;
        var itemm = send.DataContext as SongModelView;
        _storedItem = itemm;
        
        MyViewModel.SelectedSong = itemm;
      
         animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("forwardAnimation", ViewQueue);

        // 2. Make Target Visible
        SmokeGrid.Visibility = Visibility.Visible;
        SmokeGrid.ViewQueueGrid.Opacity = 0; // Hide it initially so we see the animation fly in


        await SmokeGrid.DispatcherQueue.EnqueueAsync(() =>
        {
            // 4. Start Animation
            SmokeGrid.ViewQueueGrid.Opacity = 1;
            animation.TryStart(SmokeGrid.ViewQueueGrid);
        });
    }


    private void MySongsTableView_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private async void AlbumBtn_Click(object sender, RoutedEventArgs e)
    {
        var song = ((Button)sender).DataContext as SongModelView;
        
        if(song is null) return;
        var supNavTransInfo = new SuppressNavigationTransitionInfo();

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };
        AnimationHelper.Prepare(

AnimationHelper.Key_ListToDetail, sender as UIElement,
true
);
        var navParams = new SongDetailNavArgs
        {
            Song = song!,
            ExtraParam = MyViewModel,
            ViewModel = MyViewModel
        };
        Type pageType = typeof(AlbumPage);

        if (song.Album is null)
        {
            MyViewModel.LoadAlbumDetails(song);
        }
        MyViewModel.SelectedAlbum = song.Album;
        MyViewModel.SelectedSong = song;
        Frame?.NavigateToType(pageType, navParams, navigationOptions);

    }

    private void coverArtImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var send = (Image)sender;
        var song = send.DataContext as SongModelView;

        if (song != null)
        {
            if (song.TitleDurationKey != MyViewModel.SelectedSong?.TitleDurationKey)
            {
                MyViewModel.SelectedSong = song;
            }

        }
    }

    private async void ArtistBtnStackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {

            var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
            var properties = e.GetCurrentPoint(nativeElement).Properties;



            var point = e.GetCurrentPoint(nativeElement);
            MyViewModel.SelectedSong = ((StackPanel)sender).DataContext as SongModelView;
            _storedSong = ((StackPanel)sender).DataContext as SongModelView;


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

            var dbSongArtists = MyViewModel.RealmFactory.GetRealmInstance();
            var dbSong = dbSongArtists
                .Find<SongModel>(_storedSong.Id);
            if (dbSong is null) return;
            if ((dbSong.ArtistToSong.Count < 1 || dbSong.Artist is null))
            {

                var ArtistsInSong = MyViewModel.SelectedSong?.ArtistName.
                Split(",").ToList();
                await MyViewModel.AssignArtistToSongAsync(MyViewModel.SelectedSong.Id,
                     ArtistsInSong);


            }
            var selectingg = dbSong.ArtistToSong.ToList();
            var sel2 = selectingg.Select(x => new ArtistModelView()
            {
                Id = x.Id,
                Name = x.Name,
                Bio = x.Bio,
                ImagePath = x.ImagePath
            });
            var namesOfartists = sel2.Select(a => a.Name);

            bool isSingular = namesOfartists.Count() > 1 ? true : false;
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
                    Text = $"{namesOfartists.Count()} {artistText} linked",
                    IsEnabled = false
                    
                });

            foreach (var artistName in namesOfartists)
            {
                var root = new MenuFlyoutItem { Text = artistName };
                root.PointerReleased += (obj, e) =>
                {
                    var songContext = ((MenuFlyoutItem)obj).Text;

                    var selectedArtist = MyViewModel.RealmFactory.GetRealmInstance()
                    .Find<SongModel>(_storedSong.Id)?.ArtistToSong.First(x => x.Name == songContext)
                    .ToArtistModelView();

                    
                    var nativeElementMenuFlyout = (Microsoft.UI.Xaml.UIElement)obj;
                    var propertiesMenuFlyout = e.GetCurrentPoint(nativeElementMenuFlyout).Properties;
                    if (propertiesMenuFlyout.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.RightButtonReleased)
                    {
                        MyViewModel.SearchSongForSearchResultHolder(PresetQueries.ByArtist(songContext))
                        ;
                        return;
                    }
                };
                root.Click += async (obj, routedEv) =>
                {

                    var songContext = ((MenuFlyoutItem)obj).Text;

                    var selectedArtist = MyViewModel.RealmFactory.GetRealmInstance()
                    .Find<SongModel>(_storedSong.Id).ArtistToSong.First(x => x.Name == songContext)
                    .ToArtistModelView();


                    await MyViewModel.SetSelectedArtist(selectedArtist);


                    FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                    {
                        TransitionInfoOverride = supNavTransInfo,
                        IsNavigationStackEnabled = true

                    };
                    AnimationHelper.PrepareFromChild(
     sender as DependencyObject,
     "ArtistNameTxt",
     AnimationHelper.Key_Forward
 );

                    Frame?.NavigateToType(pageType, navParams, navigationOptions);
                };

                contextMenuFlyout.Items.Add(root);
            }


            try
            {
                if (namesOfartists.Count() > 1)
                {
                    contextMenuFlyout.ShowAt(nativeElement, point.Position);
                }
                else
                {

                    var selectedArtist = MyViewModel.RealmFactory.GetRealmInstance()
                    .Find<SongModel>(_storedSong.Id).ArtistToSong.First()
                    .ToArtistModelView();
                    if (selectedArtist is null) return;
                    await MyViewModel.SetSelectedArtist(selectedArtist);


                    if (properties.IsRightButtonPressed)
                    {
                        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(selectedArtist!.Name!));
                        return;
                    }

                    FrameNavigationOptions navigationOptions = new FrameNavigationOptions
                    {
                        TransitionInfoOverride = supNavTransInfo,
                        IsNavigationStackEnabled = true

                    };
                    AnimationHelper.PrepareFromChild(
     sender as DependencyObject,
     "ArtistNameTxt",
     AnimationHelper.Key_Forward
 );

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

    private void ClosePanel_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SelectedSong = null;
    }

    private void SidePanel_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

        ClosePanel.Visibility = Visibility.Visible;
    }

    private void SidePanel_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        ClosePanel.Visibility = Visibility.Collapsed;

    }

    private void TitleColumn_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var send = (FrameworkElement)sender;
        var song = send.DataContext as SongModelView;

        if (song != null)
        {
            if (song.TitleDurationKey != MyViewModel.SelectedSong?.TitleDurationKey)
            {
                MyViewModel.SelectedSong = song;
            }

        }
    }

    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {
        ViewSongBtn_Click(sender, e);
    }

    private void SelectedSongImg_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewSongBtn_Click(sender, e);
    }

    private void ShowFavSongs_Click(object sender, RoutedEventArgs e)
    {

        var currentTQL = "my fav";
           SearchTextBox.Text = currentTQL;
    }

    private void ShowSongWithLyrics_Click(object sender, RoutedEventArgs e)
    {

        var currentTQL = "has lyrics";
        SearchTextBox.Text = currentTQL;
    }

    private void ShowSongWithLyrics_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var currentTQL = "has lyrics add " + MyViewModel.CurrentTqlQuery;
        SearchTextBox.Text = currentTQL;

    }

    private void ShowFavSongs_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var currentTQL = "my fav add " +MyViewModel.CurrentTqlQuery ;
           SearchTextBox.Text = currentTQL;
    }

    private void ShuffleSongs_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "random";
    }

    private void MiddlePointer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props != null)
        {
            if(props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased)
            {
                Debug.WriteLine("Show TQL pane");
            }
        }
    }

    private void SortByWithTQL_Loaded(object sender, RoutedEventArgs e)
    {
        //BuildSortMenu();
    }

    private void FieldSort_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuFlyoutItem)sender;
        var query = item.CommandParameter?.ToString();

        Debug.WriteLine(query);
        //SearchTextBox.Text = query;          // triggers your filter
        //MyViewModel.CurrentTqlQuery = query; // optional
    }
    private void FieldSortPointer(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint(null).Properties;
        var item = (MenuFlyoutItem)sender;
        string? field = item.CommandParameter?.ToString();
        if (field == null) return;
        if (props.IsRightButtonPressed)
            SearchTextBox.Text = field + " add " + MyViewModel.CurrentTqlQuery;

        if (props.IsMiddleButtonPressed)
            SearchTextBox.Text = "random " + field;
    }
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (SortByWithTQL.Flyout is MenuFlyout fly)
        {
            foreach (var sub in fly.Items.OfType<MenuFlyoutSubItem>())
            {
                foreach (var item in sub.Items.OfType<MenuFlyoutItem>())
                    item.RemoveClick();
            }
        }
    }

    private void SelectedSongImg_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.WhenPropertyChange( nameof(MyViewModel.SelectedSong),newVl => MyViewModel.SelectedSong)
        .ObserveOn(RxSchedulers.UI)
            .Subscribe(selectedSong =>
            {
                if (!string.IsNullOrEmpty(selectedSong?.CoverImagePath))
                    SelectedSongImg.Source = new BitmapImage(new Uri(selectedSong.CoverImagePath));
            });
    }

    private async Task ShowNotification(string message, Microsoft.UI.Xaml.Controls.InfoBarSeverity severity)
    {
        // Use a TeachingTip or create a temporary InfoBar for notifications
        DispatcherQueue.TryEnqueue(async () =>
        {
            
            await PlatUtils.ShowNewNotification(message);
            // Auto-close after 3 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, args) =>
            {
                PlatUtils.ClearNotifications();
            };

           
        });
    }

    private void QueueReOrder_Click(object sender, RoutedEventArgs e)
    {
        throw new NotImplementedException("Queue Reodering not yet implemented");
    }

    private void StackPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }

    private void DurationFormatted_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }

    private void SmokeGrid_Loaded(object sender, RoutedEventArgs e)
    {
        SmokeGrid.SetBaseViewModelWin(MyViewModel);
    }
}
