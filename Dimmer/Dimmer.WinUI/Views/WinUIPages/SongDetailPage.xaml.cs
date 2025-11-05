using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

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

using Button = Microsoft.Maui.Controls.Button;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using Page = Microsoft.UI.Xaml.Controls.Page;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinUIPages;
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
    }
    BaseViewModelWin MyViewModel { get; set; }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            MyViewModel = (args.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            DetailedSong = args.Song;
        }
        

        ApplyEntranceEffect();

        var animation = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("ForwardConnectedAnimation");

        detailedImage.Loaded += (_, __) =>
        {
            animation?.TryStart(detailedImage, new UIElement[] { coordinatedPanel });
        };

    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && VisualTreeHelper.GetParent(detailedImage) != null)
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

    private void ApplyEntranceEffect()
    {
        var visual = ElementCompositionPreview.GetElementVisual(detailedImage);

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
                spring.FinalValue = Vector3.Zero;
                spring.DampingRatio = 0.5f;
                spring.Period = TimeSpan.FromMilliseconds(250);
                visual.Offset = new Vector3(0, 40, 0);
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
            var song = (SongModelView)send.BindingContext;

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
        }catch (Exception ex)
        {
           Debug.WriteLine($"Error in ToggleViewArtist_Clicked: {ex.Message}");
        }
    
    }

    private void ToggleViewAlbum_Clicked(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var song = (SongModelView)send.BindingContext;
        
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

}
