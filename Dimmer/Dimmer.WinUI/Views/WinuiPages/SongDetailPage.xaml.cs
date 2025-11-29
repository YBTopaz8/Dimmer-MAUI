using System.Text.RegularExpressions;

using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;

using Microsoft.UI.Xaml.Documents;

using Button = Microsoft.UI.Xaml.Controls.Button;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Thickness = Microsoft.UI.Xaml.Thickness;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using Visual = Microsoft.UI.Composition.Visual;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongDetailPage : Page
{
    readonly Microsoft.UI.Xaml.Controls.Page? NativeWinUIPage;
    private SongTransitionAnimation _userPrefAnim = SongTransitionAnimation.Spring;

    private readonly Compositor _compositor;
    public SongModelView? DetailedSong { get; set; }
    public SongDetailPage()
    {
        InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        //DataContext = viewModelWin;
        //MyViewModel = viewModelWin;

        _sectionNames = new()
        {
            { SectionOverview, "Overview" },
            { SectionPlayback, "Playback" },
            { SectionLyricsStackPanel, "Lyrics" },
            { SectionHistory, "History" },
            { SectionRelated, "Related" }
        };

        SetupRightClickMenu();
    }

    private void SetupRightClickMenu()
    {
        var menu = new MenuFlyout();

        void add(string name, FrameworkElement target)
        {
            var item = new MenuFlyoutItem { Text = name };
            item.Click += (_, __) => target.StartBringIntoView();
            if(CurrentSectionLabel.Text == name)
            {
                
                item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                item.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);

            }
            menu.Items.Add(item);
        }

        foreach (var kv in _sectionNames)
            add(kv.Value, kv.Key);

        this.PointerPressed += (s, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                var pos = e.GetCurrentPoint(this).Position;
                menu.ShowAt(this,pos);
            }
        };

        this.BgImage.Loaded += async (s, e) =>
        {
            var visualBgImage = ElementCompositionPreview.GetElementVisual(this.BgImage);
            await Task.Delay(200); // slight delay to ensure smoothness
            PlatUtils.ApplyEntranceEffect(visualBgImage, BgImage, _userPrefAnim, _compositor);
            
        };
    }

    BaseViewModelWin MyViewModel { get; set; }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            var vm = args.ExtraParam is null ? args.ViewModel as BaseViewModelWin : args.ExtraParam as BaseViewModelWin;

            if (vm != null)
            {
                detailedImage.Opacity = 0;
                MyViewModel = vm;
                DetailedSong = args.Song;

                MyViewModel.CurrentWinUIPage = this;
                Visual? visual = ElementCompositionPreview.GetElementVisual(detailedImage);
                PlatUtils.ApplyEntranceEffect(visual, detailedImage, _userPrefAnim,_compositor);

                var animation = ConnectedAnimationService.GetForCurrentView()
               .GetAnimation("ForwardConnectedAnimation");

                detailedImage.Loaded += (_, _) =>
                {
                    detailedImage.Opacity = 1;
                    animation?.TryStart(detailedImage, new [] { coordinatedPanel });
                };
                MyViewModel.SelectedSong = DetailedSong;
                await MyViewModel.LoadLyricsFromOnlineOrDBIfNeededAsync(MyViewModel.SelectedSong);
                await MyViewModel.LoadSelectedSongLastFMData();
                LoadUiComponents();

            }
        }
    }

    private void LoadUiComponents()
    {
        LoadWikiOfSong();
    }

    private void LoadWikiOfSong()
    {
        if (MyViewModel.SelectedSongLastFMData is null || MyViewModel.SelectedSongLastFMData.Wiki is null ||
            MyViewModel.SelectedSongLastFMData.Wiki.Summary is null) return;
        var html = MyViewModel.SelectedSongLastFMData.Wiki.Summary;
        BioBlock.Blocks.Clear();

        Paragraph p = new Paragraph();

        var parts = html.Split(new[] { "<a", "</a>" }, StringSplitOptions.None);

        p.Inlines.Add(new Run { Text = parts[0] });

        // crude but works for Last.fm since it only has 1 link
        if (parts.Length > 1)
        {
            // Extract the URL
            var hrefMatch = Regex.Match(html, "href=\"(.*?)\"");
            string url = hrefMatch.Success ? hrefMatch.Groups[1].Value : "";

            Hyperlink h = new Hyperlink();
            h.Inlines.Add(new Run { Text = "Read more on Last.fm" });
            h.NavigateUri = new Uri(url);

            p.Inlines.Add(h);
        }

        BioBlock.Blocks.Add(p);
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(detailedImage) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", detailedImage);
            }
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // Standard navigation back
        if (Frame.CanGoBack)
        {
           
            Frame.GoBack();
        }
    }

    private void MyPage_Loaded(object sender, RoutedEventArgs e)
    {
        CalculateSectionOffsets();
    }


    private void ResultsList_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private async void ToggleViewArtist_Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (MyViewModel?.CurrentPlayingSongView is null)
                return;

            var send = (Button)sender;
            var song = (SongModelView)send.DataContext;

            char[] dividers = { ',', ';', ':', '|', '-' };
            var namesList = MyViewModel.CurrentPlayingSongView.OtherArtistsName?
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToArray() ?? [];

            string selectedArtist = string.Empty;

            if (namesList.Length > 1)
            {
                var dialog = new ContentDialog
                {
                    Title = "Select Artist",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    XamlRoot = (sender as FrameworkElement)?.XamlRoot
                };

                var list = new ListView
                {
                    SelectionMode = ListViewSelectionMode.Single,
                    ItemsSource = namesList,
                    Height = 200
                };
                dialog.Content = list;

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && list.SelectedItem is string choice)
                    selectedArtist = choice;
                else
                    return; // user canceled
            }
            else if (namesList.Length == 1)
            {
                selectedArtist = namesList[0];
            }
            else return;

            // Perform your search actions
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", selectedArtist));
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(song.AlbumName));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in ToggleViewArtist_Clicked: {ex.Message}");
        }

    }

    private void ToggleViewAlbum_Clicked(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var song = (SongModelView)send.DataContext;

        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(song.AlbumName));
    }

    private async void PlaySongGestRec_Tapped(object sender, RoutedEventArgs e)
    {
        await MyViewModel.PlayPauseToggleCommand.ExecuteAsync(null);
    }

    private void MainTabs_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var newSelection = e.AddedItems;

        Debug.WriteLine(newSelection.GetType());
    }

    private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void TabViewItem_Unloaded(object sender, RoutedEventArgs e)
    {

    }

    private void TabViewItem_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
    {

    }

    private void TabViewItem_CloseRequested(TabViewItem sender, TabViewTabCloseRequestedEventArgs args)
    {

    }
    private void ApplyColorFade(Page targetPage, Windows.UI.Color color)
    {
        var visual = _compositor.CreateSpriteVisual();
        visual.Size = new Vector2((float)targetPage.ActualWidth, (float)targetPage.ActualHeight);
        visual.Brush = _compositor.CreateColorBrush(color);
        visual.Opacity = 0f;
        ElementCompositionPreview.SetElementChildVisual(targetPage, visual);

        var fade = _compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(0.5f, 1f);
        fade.InsertKeyFrame(1f, 0f);
        fade.Duration = TimeSpan.FromMilliseconds(600);
        visual.StartAnimation("Opacity", fade);
    }

    private void ApplyParallax(UIElement foreground, UIElement background)
    {
        var fgVisual = ElementCompositionPreview.GetElementVisual(foreground);
        var bgVisual = ElementCompositionPreview.GetElementVisual(background);

        fgVisual.Offset = new Vector3(100, 0, 0);
        bgVisual.Offset = new Vector3(50, 0, 0);

        var fgAnim = _compositor.CreateVector3KeyFrameAnimation();
        fgAnim.InsertKeyFrame(1f, Vector3.Zero);
        fgAnim.Duration = TimeSpan.FromMilliseconds(400);

        var bgAnim = _compositor.CreateVector3KeyFrameAnimation();
        bgAnim.InsertKeyFrame(1f, Vector3.Zero);
        bgAnim.Duration = TimeSpan.FromMilliseconds(600);

        fgVisual.StartAnimation("Offset", fgAnim);
        bgVisual.StartAnimation("Offset", bgAnim);
    }


    private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var btn = (UIElement)sender;
        var visual = ElementCompositionPreview.GetElementVisual(btn);
        var anim = _compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1.2f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.CenterPoint = new Vector3((float)btn.RenderSize.Width / 2, (float)btn.RenderSize.Height / 2, 0);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);
    }

    private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var btn = (UIElement)sender;
        var visual = ElementCompositionPreview.GetElementVisual(btn);
        var anim = _compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);
    }



    //private void ApplyDepthZoomEffect(UIElement element)
    //{
    //    var visual = ElementCompositionPreview.GetElementVisual(element);

    //    var blur = _compositor.CreateGaussianBlurEffect();
    //    var brush = _compositor.CreateEffectFactory(blur).CreateBrush();
    //    var sprite = _compositor.CreateSpriteVisual();
    //    sprite.Brush = brush;
    //    ElementCompositionPreview.SetElementChildVisual(element, sprite);

    //    visual.CenterPoint = new Vector3((float)element.RenderSize.Width / 2, (float)element.RenderSize.Height / 2, 0);
    //    visual.Scale = new Vector3(0.85f);
    //    var zoom = _compositor.CreateVector3KeyFrameAnimation();
    //    zoom.InsertKeyFrame(1f, Vector3.One);
    //    zoom.Duration = TimeSpan.FromMilliseconds(400);
    //    visual.StartAnimation("Scale", zoom);
    //}
    private void ApplyFlipEffect(UIElement element)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        visual.RotationAxis = new Vector3(0, 1, 0); // Y-axis flip
        visual.CenterPoint = new Vector3((float)element.RenderSize.Width / 2, (float)element.RenderSize.Height / 2, 0);
        visual.RotationAngleInDegrees = -90;

        var flipAnim = _compositor.CreateScalarKeyFrameAnimation();
        flipAnim.InsertKeyFrame(1f, 0f);
        flipAnim.Duration = TimeSpan.FromMilliseconds(500);
        var easing = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0f), new Vector2(0.58f, 1f));
        flipAnim.InsertKeyFrame(1f, 0f, easing);

        visual.StartAnimation(nameof(visual.RotationAngleInDegrees), flipAnim);
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void ArtistBtn_Click(object sender, RoutedEventArgs e)
    {

        try
        {

            // Navigate to the detail page, passing the selected song object.
            // Suppress the default page transition to let ours take over.
            var supNavTransInfo = new SuppressNavigationTransitionInfo();
            Type pageType = typeof(ArtistPage);
            var navParams = new SongDetailNavArgs
            {
                Song = DetailedSong!,
                ExtraParam = MyViewModel,
                ViewModel = MyViewModel
            };

            MyViewModel.IsBackButtonVisible = WinUIVisibility.Collapsed;
            var selectedArtist = DetailedSong.ArtistToSong.FirstOrDefault(x=>x.Name== DetailedSong.ArtistName);

                  
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
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(DetailedSong.Artist.Name));
            Frame?.NavigateToType(pageType, navParams, navigationOptions);
               
             
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuFlyout.ShowAt failed: {ex.Message}");
                // fallback: anchor without position
                //flyout.ShowAt(nativeElement);
            }

    }
    private readonly Dictionary<FrameworkElement, string> _sectionNames;
    private List<(double Offset, string Name)> _sectionOffsets = new();


    private void CalculateSectionOffsets()
    {
        _sectionOffsets.Clear();
        double currentY = 0;


        foreach (var kv in _sectionNames)
        {
            var element = kv.Key;
            // Only valid if element is actually in the visual tree
            if (element.ActualHeight > 0)
            {
                var transform = element.TransformToVisual(SegmentStack); // Transform to the StackPanel inside Scroller
                var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _sectionOffsets.Add((point.Y, kv.Value));
            }
        }
    }
    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        if (_sectionOffsets.Count == 0) return;

        var scrollY = Scroller.VerticalOffset;
        // Add a buffer (e.g., 100px) so it highlights slightly before the element hits the very top
        var targetY = scrollY + 100;

        // Find the last section that has an offset less than current scroll position
        var currentSection = _sectionOffsets.LastOrDefault(x => x.Offset <= targetY);

        if (currentSection.Name != null && CurrentSectionLabel.Text != currentSection.Name)
        {
            CurrentSectionLabel.Text = currentSection.Name;
            // Intense UI: Add a small bounce animation to the label when it changes
            AnimateLabelChange();
        }
    }
    private void AnimateLabelChange()
    {
        var visual = ElementCompositionPreview.GetElementVisual(CurrentSectionLabel);
        var anim = _compositor.CreateVector3KeyFrameAnimation();
        anim.InsertKeyFrame(0f, new Vector3(0, 5, 0)); // Start slightly down
        anim.InsertKeyFrame(1f, Vector3.Zero);
        anim.Duration = TimeSpan.FromMilliseconds(300);
        visual.StartAnimation("Offset", anim);

        var fade = _compositor.CreateScalarKeyFrameAnimation();
        fade.InsertKeyFrame(0f, 0f);
        fade.InsertKeyFrame(1f, 1f);
        fade.Duration = TimeSpan.FromMilliseconds(200);
        visual.StartAnimation("Opacity", fade);
    }
    string current = "Overview";


    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if(props.IsXButton1Pressed)
        {
            if (Frame.CanGoBack)
            {
                //var image = detailedImage;
                //ConnectedAnimationService.GetForCurrentView()
                //    .PrepareToAnimate("BackwardConnectedAnimation", image);
                Frame.GoBack();
            }
        }
    }

    private void BioBlock_Loaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void SectionAnalytics_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void BgImage_Loaded(object sender, RoutedEventArgs e)
    {
        SetupCinematicBackground();

    }
    private void SetupCinematicBackground()
    {
        // 1. Get Visuals
        var bgVisual = ElementCompositionPreview.GetElementVisual(BgImage);
        var scrollerVisual = ElementCompositionPreview.GetElementVisual(Scroller);

        // 2. Create the Parallax Effect (Expression Animation)
        // Formula: bg.Offset.Y = -scroller.VerticalOffset * 0.3 (Move at 30% speed)
        var scrollPropSet = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(Scroller);
        var parallaxExpression = _compositor.CreateExpressionAnimation("(-scroller.Translation.Y * multiplier)");
        parallaxExpression.SetScalarParameter("multiplier", 0.3f);
        parallaxExpression.SetReferenceParameter("scroller", scrollPropSet);

        // Apply to the Offset.Y of the background
        bgVisual.StartAnimation("Offset.Y", parallaxExpression);

        // 3. Create a Blur/Saturation Effect using Win2D or Composition (Native approach)
        // Note: Pure Composition Blur requires a loaded Surface, which is complex with Image control.
        // A quicker "Intense" UI hack for WinUI 3 without external libraries:
        // Just animate the Opacity and Scale for a "Breathing" effect.

        bgVisual.Opacity = 0.4f; // Set base opacity

        // Scale animation to make it feel alive
        var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.InsertKeyFrame(0f, new Vector3(1.0f));
        scaleAnim.InsertKeyFrame(0.5f, new Vector3(1.05f)); // Slight zoom
        scaleAnim.InsertKeyFrame(1f, new Vector3(1.0f));
        scaleAnim.Duration = TimeSpan.FromSeconds(20);
        scaleAnim.IterationBehavior = AnimationIterationBehavior.Forever;

        bgVisual.CenterPoint = new Vector3((float)BgImage.ActualWidth / 2, (float)BgImage.ActualHeight / 2, 0);
        bgVisual.StartAnimation("Scale", scaleAnim);
    }

    private void SimilarSongStackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Apply a slight scale-up effect on pointer enter
        // fetch image from lastfm in vm and show in custom tooltip 
        // custom tooltip shows larger image and song details
        // stackpanel height 300 width 200 with two textblocks below image
        var stackPanel = (StackPanel)sender;
        var track = (stackPanel.DataContext as Hqub.Lastfm.Entities.Track);
       UIElement? uiElement = (UIElement)sender;
        var visual = ElementCompositionPreview.GetElementVisual(uiElement);
        var anim = _compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 1.05f);
        anim.Duration = TimeSpan.FromMilliseconds(150);
        visual.CenterPoint = new Vector3((float)stackPanel.RenderSize.Width / 2, (float)stackPanel.RenderSize.Height / 2, 0);
        visual.StartAnimation("Scale.X", anim);
        visual.StartAnimation("Scale.Y", anim);
        LoadToolTipForSimilarTracks(uiElement, track);
    }

    void LoadToolTipForSimilarTracks(UIElement elt, Hqub.Lastfm.Entities.Track trck)
    {
        toolTip ??= new ToolTip();

        var toolTipContent = new StackPanel
        {
            Width = 200,
            Height = 200,
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };
        var imgSourceFromUrl = trck.Images?.FirstOrDefault(img => img.Size == "large")?.Url;
        
        var img = new Microsoft.UI.Xaml.Controls.Image
        {
            Width = 120,
            Height = 120,
            Margin = new Thickness(10)
        };
        img.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(imgSourceFromUrl ?? "ms-appx:///Assets/PlaceholderImage.png"));

        toolTipContent.Children.Add(img);
        var titleBlock = new TextBlock
        {
            Text = trck.Name,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Margin = new Thickness(10, 5, 10, 0),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold
        };
        toolTipContent.Children.Add(titleBlock);
        var artistBlock = new TextBlock
        {
            Text = trck.Artist.Name,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
            Margin = new Thickness(10, 0, 10, 10)
        };
        toolTipContent.Children.Add(artistBlock);
        toolTip.Content = toolTipContent;
        toolTip.Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Top;
        


        ToolTipService.SetToolTip(elt, toolTip);
        toolTip.IsOpen = true;


    }
    ToolTip toolTip;
    private void SimilarSongStackPanel_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var send = (UIElement)sender;
        toolTip.IsOpen = false;
    }

    private void ArtistPickerAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {

    }

    private void ArtistPickerAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var chosen = args.SelectedItem as string;
        if (chosen is not null)
        {
            sender.Text = chosen;
        }
        MyViewModel.UpdateSongWithNoArtistToNewArtist(chosen);

    }

    private void ArtistPickerAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {

    }



    private void ArtistPickerAutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
    {
        var autoSuggestBox = (AutoSuggestBox)sender;
        var sourceFromDb = MyViewModel.SelectedSong.ArtistToSong
            .Where(x=> x is not null)
            .Where(x=> !string.IsNullOrWhiteSpace(x.Name))
            .Select(a => a.Name)
            .Distinct()
            .ToList();
    }

    private void SectionOverview_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
    }

    private async void SectionOverview_PointerExited(object sender, PointerRoutedEventArgs e)
    {
    }

    private void EditSongBtn_Click(object sender, RoutedEventArgs e)
    {

        // Navigate to the detail page, passing the selected song object.
        // Suppress the default page transition to let ours take over.
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(EditSongPage);
        var navParams = new SongDetailNavArgs
        {
            Song = DetailedSong!,
            ViewModel = MyViewModel
            
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);

    }

    private void AlbumBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void LyricsSection_Click(object sender, RoutedEventArgs e)
    {
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        
        Type pageType = typeof(LyricsEditorPage);
        var navParams = new SongDetailNavArgs
        {
            Song = DetailedSong!,
            ExtraParam = MyViewModel,
            ViewModel = MyViewModel
        };
        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };
        
        // prepare the animation BEFORE navigation
        var ArtistNameTxt =  PlatUtils.FindVisualChild<TextBlock>(SectionLyricsStackPanel, "LyricsSection");
        if (ArtistNameTxt != null)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", ArtistNameTxt);
        }

        Frame?.NavigateToType(pageType, navParams, navigationOptions);


    }
}
