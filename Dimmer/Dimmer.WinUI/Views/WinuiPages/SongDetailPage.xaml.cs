using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Maui.Core.Extensions;

using Dimmer.Charts;
using Dimmer.Interfaces.Services;
using Dimmer.Utilities.Extensions;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;

using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Foundation.Metadata;

using Border = Microsoft.UI.Xaml.Controls.Border;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using RadioButton = Microsoft.UI.Xaml.Controls.RadioButton;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using Visibility = Microsoft.UI.Xaml.Visibility;
using Visual = Microsoft.UI.Composition.Visual;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongDetailPage : Page
{
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
        if(e.Parameter is BaseViewModelWin myVm)
        {
            MyViewModel = myVm;
            this.DataContext = MyViewModel;
            DetailedSong = MyViewModel.SelectedSong;

            


            Visual? visual = ElementCompositionPreview.GetElementVisual(TitleBlock);
            PlatUtils.ApplyEntranceEffect(visual, TitleBlock, _userPrefAnim, _compositor);
            MyViewModel.CurrentWinUIPage = this;
        }
        if (e.Parameter is SongDetailNavArgs args)
        {
            var vm = args.ExtraParam is null ? args.ViewModel as BaseViewModelWin : args.ExtraParam as BaseViewModelWin;

            if (vm != null)
            {
                detailedImage.Opacity = 0;
                MyViewModel = vm;
                MyViewModel.SelectedSong = args.Song;
                DetailedSong = args.Song;
                this.DataContext = MyViewModel;


                var allAchievementsForSong = MyViewModel.RealmFactory.GetRealmInstance()
                    .All<SongModel>()
                    .Where(x => x.Id == args.Song.Id)
                    .FirstOrDefault()?.EarnedAchievementIds.ToArray();
                Debug.WriteLine(allAchievementsForSong?.Length);


                var allAchievementsForAll = MyViewModel.RealmFactory.GetRealmInstance()
                    .All<SongModel>().ToList()
                    .Where(x => x.EarnedAchievementIds.ToList() != null && 
                    x.EarnedAchievementIds.Count > 0).ToArray();
                Debug.WriteLine(allAchievementsForSong?.Length);
                Debug.WriteLine(allAchievementsForAll.Length);



                Visual? visual = ElementCompositionPreview.GetElementVisual(detailedImage);
                PlatUtils.ApplyEntranceEffect(visual, detailedImage, _userPrefAnim,_compositor);

               
            }
        }

        MyViewModel.SelectedSong = DetailedSong;

        EventsCount.Text = MyViewModel.SelectedSong.PlayEvents.Count.ToString();


        MyViewModel.CurrentWinUIPage = this;
        await MyViewModel.LoadLyricsFromOnlineOrDBIfNeededAsync(MyViewModel.SelectedSong!);
        await MyViewModel.LoadSelectedSongLastFMData();
        LoadUiComponents();
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
        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(detailedImage) != null)
            {
                AnimationHelper.Prepare(AnimationHelper.Key_DetailToList, detailedImage);
            }
        }
        base.OnNavigatingFrom(e);


    }

    private void MyPage_Loaded(object sender, RoutedEventArgs e)
    {
        CalculateSectionOffsets();
    }

    private async void ArtistBtn_Click(object sender, RoutedEventArgs e)
    {

        try
        {
            if (DetailedSong is null) return;
            // Navigate to the detail page, passing the selected song object.
            // Suppress the default page transition to let ours take over.
            var supNavTransInfo = new SlideNavigationTransitionInfo();
            Type pageType = typeof(ArtistPage);
            var navParams = new SongDetailNavArgs
            {
                Song = DetailedSong!,
                ExtraParam = MyViewModel,
                ViewModel = MyViewModel
            };

            MyViewModel.IsBackButtonVisible = WinUIVisibility.Collapsed;
            var realm = MyViewModel.RealmFactory.GetRealmInstance();
            var dbArtist = realm.All<ArtistModel>()
                .FirstOrDefaultNullSafe(a => a.Name == DetailedSong.ArtistToSong.First()!.Name);

                  
            await MyViewModel.SetSelectedArtist(dbArtist.ToArtistModelView());


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
                    .PrepareToAnimate("MoveViewToArtistPageFromSongDetailPage", ArtistNameTxt);
            }
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(DetailedSong.ArtistName));
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

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if(props.IsXButton1Pressed)
        {
                if (detailedImage != null && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(detailedImage) != null)
                {
                    ConnectedAnimationService.GetForCurrentView()
                        .PrepareToAnimate("BackConnectedAnimation", detailedImage);
                }
         
            if (Frame.CanGoBack)
            {
               
                Frame.GoBack();
            }
        }
    }

    private void BioBlock_Loaded(object sender, RoutedEventArgs e)
    {
        
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

    private void SectionOverview_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
    }

    private async void SectionOverview_PointerExited(object sender, PointerRoutedEventArgs e)
    {
    }

    private void EditSongBtn_Click(object sender, RoutedEventArgs e)
    {
        var detailedImageVisual = ElementCompositionPreview.GetElementVisual(detailedImage);
        if (detailedImageVisual != null)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("ForwardConnectedAnimation", detailedImage);
            PlatUtils.ApplyEntranceEffect(detailedImageVisual, detailedImage, SongTransitionAnimation.Spring, _compositor);
        }
        // Navigate to the detail page, passing the selected song object.
        // Suppress the default page transition to let ours take over.
        var supNavTransInfo = new SlideNavigationTransitionInfo();
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

    private async void LyricsSection_Click(object sender, RoutedEventArgs e)
    {
        var supNavTransInfo = new SlideNavigationTransitionInfo();
        
        Type pageType = typeof(SingleSongLyrics);
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
        var detailedImageVisual = ElementCompositionPreview.GetElementVisual(detailedImage);
        if (detailedImageVisual != null)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("MoveViewToLyricsPageFromSongDetailPage", SectionLyricsStackPanel);
            
        }
      

        Frame?.NavigateToType(pageType, navParams, navigationOptions);


    }
    private void ArtistToSong_Loaded(object sender, RoutedEventArgs e)
    {
        if(MyViewModel.SelectedSong is null) return;
        var dbSong = MyViewModel.RealmFactory.GetRealmInstance()            
            .Find<SongModel>(MyViewModel.SelectedSong.Id);

        if (dbSong is null) return;

        if ((dbSong.ArtistToSong.Count <1 || dbSong.Artist is null) && dbSong.ArtistName is not null)
        {
            RxSchedulers.Background.ScheduleToUI(async () =>
            {
                var ArtistsInSong = MyViewModel.SelectedSong.OtherArtistsName.
                Split(",").ToList();
               await MyViewModel.AssignArtistToSongAsync(MyViewModel.SelectedSong.Id,
                    ArtistsInSong);

            });
        }
        var artistToSong = dbSong.ArtistToSong;
        var listOfArtistsModelView = artistToSong.ToList().Select(x =>
        {
            
            var objView = x.ToArtistModelView();
            //objView.TotalSongsByArtist = x.Songs.Count();
            return objView;
        });

        ArtistToSong.ItemsSource = listOfArtistsModelView;
    }

    private void AllAchievementsIR_Loaded(object sender, RoutedEventArgs e)
    {
        //// 1. Get the current song
        //var currentSongId = MyViewModel.SelectedSong?.Id;
        //if (currentSongId == null) return;

        //// 2. Open Realm to get the song's data
        //var realm = MyViewModel.RealmFactory.GetRealmInstance();
        //var song = realm.Find<SongModel>(currentSongId);

        //if (song == null) return;

        //var earnedIds = song.EarnedAchievementIds.ToList();
        //if (earnedIds?.Count < 1)
        //{
        //    AllAchievementsIR.Header = "No Achievements Yet..";

        //}
        //else
        //{
        //    var unlockedRules = MyViewModel.BaseAppFlow.AchievementService.GetAchievementsByIds(earnedIds);
        
        //    AllAchievementsIR.ItemsSource = unlockedRules;
        
        //}
    }
    AchievementRule _storedItem;
    private async void PopUpBackButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardsAnimation", destinationElement);
        SmokeGrid.Children.Remove(destinationElement);

        // Collapse the smoke when the animation completes.
        animation.Completed += Animation_Completed;

        // If the connected item appears outside the viewport, scroll it into view.
       await AllAchievementsIR.SmoothScrollIntoViewWithItemAsync(_storedItem, (ScrollItemPlacement)ScrollIntoViewAlignment.Default);
        AllAchievementsIR.UpdateLayout();

        // Use the Direct configuration to go back (if the API is available).
        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
        {
            animation.Configuration = new DirectConnectedAnimationConfiguration();
        }

        // Play the second connected animation.
        await AllAchievementsIR.TryStartConnectedAnimationAsync(animation, _storedItem, "connectedPopUpElement");

    }
    private void Animation_Completed(ConnectedAnimation sender, object args)
    {
        SmokeGrid.Visibility = WinUIVisibility.Collapsed;
        SmokeGrid.Children.Add(destinationElement);
    }



    private void connectedPopUpElement_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ConnectedAnimation? animation = null;
        Border send = (Border)sender;
        var itemm = send.DataContext as AchievementRule;
        _storedItem = itemm;
        MyViewModel.SelectedAchievement = itemm;
        // Prepare the connected animation.
        // Notice that the stored item is passed in, as well as the name of the connected element.
        // The animation will actually start on the Detailed info page.
        animation = AllAchievementsIR.PrepareConnectedAnimation("forwardAnimation", itemm, "connectedPopUpElement");



        SmokeGrid.Visibility = WinUIVisibility.Visible;
        if (animation != null)
        {
            animation.TryStart(destinationElement);
        }

    }

    private void ViewCharts_Click(object sender, RoutedEventArgs e)
    {
        
        var supNavTransInfo = new SlideNavigationTransitionInfo();

        var pageType = typeof(SongStatsContainerPage);

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
                .PrepareToAnimate("MoveViewToArtistPageFromSongDetailPage", ArtistNameTxt);
        }
        var navParams = new SongDetailNavArgs
        {
            Song = DetailedSong!,
            ExtraParam = MyViewModel,
            ViewModel = MyViewModel
        };
        Frame?.NavigateToType(pageType, navParams, navigationOptions);


    }

    private void EditSongAudioBtn_Click(object sender, RoutedEventArgs e)
    {

        Frame?.Navigate(typeof(AudioEditorPage), MyViewModel.SelectedSong);
    }

    private void PlaySongBtn_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel.SetAsNextToPlayInQueue(MyViewModel.SelectedSong);

    }

    private void PlaySongBtn_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private FrameworkElement _storedSourceElement;

    private async void Artist_Click(object sender, RoutedEventArgs e)
    {
        // 1. Identify the Source
        // We cast the sender to the Button defined in the template
        Button clickedButton = (Button)sender;

        var selArtist = clickedButton.DataContext as ArtistModelView;
        if (selArtist is null) return;

        ArtistDetailBorder.DataContext = selArtist;
        // We store this button so we know where to return to later
        _storedSourceElement = clickedButton;

        // 2. Prepare the Connected Animation
        // Key: "artistPreviewConAnim"
        // Source: The button the user just clicked
        ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView()
            .PrepareToAnimate("artistPreviewConAnim", clickedButton);

        // 3. Make the Target Visible (The Red Border)
        ArtistDetailBorder.Visibility = Visibility.Visible;
        ArtistDetailBorder.Opacity = 0; // Hide initially to prevent flicker before anim starts

        // 4. Wait for Layout Update to ensure Destination has correct coordinates
        if (ArtistDetailBorder.DispatcherQueue != null)
        {
            await ArtistDetailBorder.DispatcherQueue.EnqueueAsync(() =>
            {
                // 5. Start the Animation
                ArtistDetailBorder.Opacity = 1;
                animation.Configuration = new GravityConnectedAnimationConfiguration();
                animation.TryStart(ArtistDetailBorder);

            });
        }
    }

    private async void CloseArtistDetail_Click(object sender, RoutedEventArgs e)
    {
        // 1. Prepare the Animation BACKWARDS
        // Key: We reuse the name or use a specific return key. 
        // Usually, we prepare a new animation from the Detail view back to the List.
        ConnectedAnimation animation = ConnectedAnimationService.GetForCurrentView()
            .PrepareToAnimate("artistPreviewConAnim_Back", ArtistDetailBorder);

        // 2. Handle Completion (Collapse the Red Border when done)
        animation.Completed += BackAnimation_Completed;

        // Optional: Add Gravity configuration for the return trip if supported
        animation.Configuration = new GravityConnectedAnimationConfiguration();

        // 3. Start Animation targeting the stored list item
        if (_storedSourceElement != null)
        {
            await RootGrid.DispatcherQueue.EnqueueAsync(() =>
            {
                // Try to animate back to the specific button we clicked earlier
                bool started = animation.TryStart(_storedSourceElement);

                // Fallback: If the list scrolled or the item is gone, just collapse immediately
                if (!started)
                {
                    ArtistDetailBorder.Visibility = Visibility.Collapsed;
                }
            });
        }
        else
        {
            // Safety fallback if no source is stored
            ArtistDetailBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void BackAnimation_Completed(ConnectedAnimation sender, object args)
    {
        // Ensure the Red Border is collapsed after the visual morph is done
        ArtistDetailBorder.Visibility = Visibility.Collapsed;
        
        // Clean up the event handler to avoid memory leaks
        sender.Completed -= BackAnimation_Completed;
    }

    private async void ArtistNameBtn_Click(object sender, RoutedEventArgs e)
    {
        var currentView = (Button)sender;

        var supNavTransInfo = new SlideNavigationTransitionInfo();

        Type pageType = typeof(ArtistPage);
        var navParams = new SongDetailNavArgs
        {
            Song = DetailedSong!,
            ExtraParam = MyViewModel,
            ViewModel = MyViewModel
        };
        var artist = currentView.DataContext as ArtistModelView;
        MyViewModel.SelectedArtist = artist;

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };
        var detailedImageVisual = ElementCompositionPreview.GetElementVisual(currentView);
        if (detailedImageVisual != null)
        {
            ConnectedAnimationService.GetForCurrentView()
                .PrepareToAnimate("MoveViewToArtistPageFromSongDetailPage", currentView);

        }


        Frame?.NavigateToType(pageType, navParams, navigationOptions);


    }

    private void detailedImage_Loaded(object sender, RoutedEventArgs e)
    {
        AnimationHelper.TryStart(
       detailedImage,
       new List<UIElement> { TitleBlock }, // Coordinated elements (optional)
       AnimationHelper.Key_DetailToList,       // Check this key
       AnimationHelper.Key_ListToDetail,       // OR Check this key
       AnimationHelper.Key_ArtistToSong        // OR Check this key
   );
    }


    private void SongPlayEvents_Loaded(object sender, RoutedEventArgs e)
    {
        var allEvents = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<SongModel>(MyViewModel.SelectedSong?.Id)?
            .PlayHistory.OrderByDescending(ev => ev.EventDate)
            .Select(evt=>evt.ToDimmerPlayEventView());
        if (allEvents is null) return;
        SongPlayEvents.ItemsSource = allEvents.ToList();
    }

    private async void DeleteEventBtn_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var evt = (send.DataContext as DimmerPlayEventView);
        if (evt is null) return;
        await MyViewModel.DeletePlayEventAsync(evt);
    }

    private void MySongAchievements_Loading(FrameworkElement sender, object args)
    {
        if(MyViewModel.SelectedSong is null)return;

        MySongAchievements.FinishLoadAll(MyViewModel,MyViewModel.SelectedSong);
    }

    private void MySongAchievements_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        
    }

    private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
    {

    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void FilterEventButton_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;

        if (send.Content is null) return;
        var btnContetnt = send.Content as string;
        if (btnContetnt == null) return;

        if (btnContetnt == "All")
        {
            var defList = MyViewModel.SelectedSong.PlayEvents.OrderByDescending(ev => ev.EventDate)
                .ToList();
            SongPlayEvents.ItemsSource = defList;
            EventsCount.Text = defList.Count.ToString();
            return;
        }

        else
        {
            int eventType=0; 
            if (btnContetnt == "Started")
            {
                eventType = (int)PlayEventType.Play;
            }
            else
            {

                if (Enum.TryParse<PlayEventType>(btnContetnt, ignoreCase: true, out var parsedType))
                {
                    eventType = (int)parsedType;
                }
            }
            var filteredEvents = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<SongModel>(MyViewModel.SelectedSong?.Id)?
            .PlayHistory.Where(ev => ev.PlayType == eventType)
            .OrderByDescending(ev => ev.EventDate)
            .Select(evt => evt.ToDimmerPlayEventView());
            SongPlayEvents.ItemsSource = filteredEvents.ToList();
            EventsCount.Text = filteredEvents.Count().ToString();
        }

    }

    private void EditArtist_Click(object sender, RoutedEventArgs e)
    {

    }
}

