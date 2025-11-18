using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;

using Dimmer.Data;
using Dimmer.DimmerSearch;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Storage.FileProperties;

using static Dimmer.WinUI.Utils.AppUtil;

using Border = Microsoft.UI.Xaml.Controls.Border;
using Button = Microsoft.UI.Xaml.Controls.Button;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
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
    private readonly SongTransitionAnimation _userPrefAnim = SongTransitionAnimation.Spring;

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
            { SectionLyrics, "Lyrics" },
            { SectionAnalytics, "Analytics" },
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
            if(current== name)
            {
                item.IsEnabled = false;
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
                MyViewModel = vm;
                DetailedSong = args.Song;

                MyViewModel.CurrentWinUIPage = this;
                Visual? visual = ElementCompositionPreview.GetElementVisual(detailedImage);
                ApplyEntranceEffect(visual);

                var animation = ConnectedAnimationService.GetForCurrentView()
               .GetAnimation("ForwardConnectedAnimation");

                detailedImage.Loaded += (_, _) =>
                {
                    animation?.TryStart(detailedImage, new [] { coordinatedPanel });
                };
                MyViewModel.SelectedSong = DetailedSong;
                await MyViewModel.LoadSelectedSongLastFMData();
            }
        }
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

    }

    private void ApplyEntranceEffect(Visual visual)
    {

        switch (_userPrefAnim)
        {
            case SongTransitionAnimation.Fade:
                visual.Opacity = 0f;
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(1f, 1f);
                fade.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                visual.CenterPoint = new Vector3((float)detailedImage.ActualWidth / 2,
                                                 (float)detailedImage.ActualHeight / 2, 0);
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

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {

        UpdateCurrentSectionIndicator();
    }
    string current = "Overview";
    private void UpdateCurrentSectionIndicator()
    {
        double scrollY = Scroller.VerticalOffset;
        double viewport = Scroller.ViewportHeight;


        foreach (var kv in _sectionNames)
        {
            var item = kv.Key;
            var transform = item.TransformToVisual(Scroller);
            var pos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

            if (pos.Y + item.ActualHeight > 0 && pos.Y < viewport / 2)
            {
                current = kv.Value;
                break;
            }
        }

        CurrentSectionLabel.Text = current;
    }



}
