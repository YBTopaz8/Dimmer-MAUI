using CommunityToolkit.Maui.Core.Extensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class ArtistPage : Page
{
    public ArtistPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        _rootVisual = ElementCompositionPreview.GetElementVisual(this);
        // TODO: load from user settings or defaults
        _userPrefAnim = SongTransitionAnimation.Spring;
    }

    private readonly Microsoft.UI.Composition.Visual _rootVisual;
    private readonly Microsoft.UI.Composition.Compositor _compositor;
    private readonly SongTransitionAnimation _userPrefAnim;
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


        }

    }


    BaseViewModelWin MyViewModel { get; set; }

    private TableViewCellSlot _lastActiveCellSlot;

    public SongModelView? DetailedSong { get; set; }
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            MyViewModel = (args.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            MyViewModel.CurrentWinUIPage = this;
            DetailedSong = args.Song;
        }
        MyViewModel.IsBackButtonVisible = WinUIVisibility.Visible;

        this.DataContext = MyViewModel;
        pressedCounter = 0;
        var animation = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("ForwardConnectedAnimation");

        //CoordinatedPanel.Loaded += (_, __) =>
        //{
        //    animation?.TryStart(CoordinatedPanel);
        //};
        if (animation is not null)
        {
            ArtistNameInArtistPage.Loaded += async (_, __) =>
            {
                animation?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { CoordinatedPanel });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(CoordinatedPanel);
                Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                ApplyEntranceEffect(visual);
                ApplyEntranceEffect(visual2);
            };
            return;
        }

        var animFromSingleSongPage = ConnectedAnimationService.GetForCurrentView().
            GetAnimation("MoveViewToArtistPageFromSongDetailPage");
        if (animFromSingleSongPage is not null)
        {
            ArtistNameInArtistPage.Loaded += async (s, ee) =>
            {
                animFromSingleSongPage?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { CoordinatedPanel });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(CoordinatedPanel);
                Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                ApplyEntranceEffect(visual);
                ApplyEntranceEffect(visual2);
            };            
        }
    }

    private void ApplyEntranceEffect(Visual visual, SongTransitionAnimation defAnim = SongTransitionAnimation.Spring)
    {

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
                visual.CenterPoint = new Vector3((float)CoordinatedPanel.ActualWidth / 2,
                                                 (float)CoordinatedPanel.ActualHeight / 2, 0);
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
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(ArtistNameInArtistPage) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", ArtistNameInArtistPage);
            //}
            if (CoordinatedPanel != null && VisualTreeHelper.GetParent(CoordinatedPanel) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", CoordinatedPanel);
            }
        }
    }

    private void CoordinatedPanel2_Click(object sender, RoutedEventArgs e)
    {
        // Standard navigation back
        if (Frame.CanGoBack)
        {
            //var image = detailedImage;
            //ConnectedAnimationService.GetForCurrentView()
            //    .PrepareToAnimate("BackwardConnectedAnimation", image);
            Frame.GoBack();
        }
    }
    int pressedCounter = 0;
    private void MyArtistPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if(pressedCounter > 0)
        {
            return;
        }
        var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
        var properties = e.GetCurrentPoint(nativeElement).Properties;


        var point = e.GetCurrentPoint(nativeElement);

        if (properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed)
        {
            CoordinatedPanel2_Click(this, e);
            pressedCounter++;
        }

    }

    private void IsArtFavorite_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {

    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void AllAlbumsBtn_Loaded(object sender, RoutedEventArgs e)
    {
        return;
        DropDownButton send = (DropDownButton)sender;

        var realm = MyViewModel.RealmFactory.GetRealmInstance();

        var AlbumsByArtist = realm.Find<ArtistModel>(MyViewModel.SelectedArtist.Id).Albums;

        ObservableCollection<AlbumModelView> albums = new();

        ArtistAlbums.ItemsSource = AlbumsByArtist.AsEnumerable().Select(x=>x.ToAlbumModelView()).ToObservableCollection();

    }

    private void ArtistNameInArtistPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(MyViewModel.SelectedArtist.Name));

    }

    private void ArtistAlbums_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props.IsXButton1Pressed)
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

}