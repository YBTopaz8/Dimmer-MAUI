using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;

using Dimmer.Data;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using WinUI.TableView;

using static Dimmer.WinUI.Utils.AppUtil;

using Page = Microsoft.UI.Xaml.Controls.Page;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;

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
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            MyViewModel = (args.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            DetailedSong = args.Song;
        }
        this.DataContext = MyViewModel;
        Visual? visual = ElementCompositionPreview.GetElementVisual(CoordinatedPanel);
        Visual? visual2 = ElementCompositionPreview.GetElementVisual(DestinationElement);
        ApplyEntranceEffect(visual);
        ApplyEntranceEffect(visual2, SongTransitionAnimation.Fade);


        var animation = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("ForwardConnectedAnimation");
        
        //CoordinatedPanel.Loaded += (_, __) =>
        //{
        //    animation?.TryStart(CoordinatedPanel);
        //};

        DestinationElement.Loaded += (_, __) =>
        {
            animation?.TryStart(DestinationElement, new List<UIElement>() { CoordinatedPanel});
        };

    }

    private void ApplyEntranceEffect(Visual visual, SongTransitionAnimation defAnim= SongTransitionAnimation.Spring)
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
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(DestinationElement) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", DestinationElement);
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

    private void MyArtistPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
        var properties = e.GetCurrentPoint(nativeElement).Properties;


        var point = e.GetCurrentPoint(nativeElement);

        if (properties.IsXButton1Pressed) //also properties.IsXButton2Pressed for mouse 5
        {
            CoordinatedPanel2_Click(this, e);
        }

    }

    private void IsArtFavorite_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {

    }
}
