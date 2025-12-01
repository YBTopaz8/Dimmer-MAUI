
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.WinUI;

using Dimmer.WinUI.Views.CustomViews.WinuiViews;

using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using Windows.ApplicationModel.DataTransfer;

using WinRT;
using WinRT.Dimmer_WinUIVtableClasses;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using DataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using PropertyPath = Microsoft.UI.Xaml.PropertyPath;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using TextWrapping = Microsoft.UI.Xaml.TextWrapping;
using Thickness = Microsoft.UI.Xaml.Thickness;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using UIElement = Microsoft.UI.Xaml.UIElement;
using Visual = Microsoft.UI.Composition.Visual;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class EditSongPage : Page
{
    readonly Microsoft.UI.Xaml.Controls.Page? NativeWinUIPage;
    private SongTransitionAnimation _userPrefAnim = SongTransitionAnimation.Spring;

    private readonly Compositor _compositor;
    public SongModelView? DetailedSong { get; set; }
    public EditSongPage()
    {
        InitializeComponent(); 
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
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
                PlatUtils.ApplyEntranceEffect(visual, detailedImage, _userPrefAnim, _compositor);

                var animation = ConnectedAnimationService.GetForCurrentView()
               .GetAnimation("ForwardConnectedAnimation");

                detailedImage.Loaded += (_, _) =>
                {
                    detailedImage.Opacity = 1;
                    animation?.TryStart(detailedImage);
                };
                MyViewModel.SelectedSong = DetailedSong;
                await MyViewModel.LoadSelectedSongLastFMData();
                //LoadUiComponents();

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

    private void UserNoteBtn_Click(object sender, RoutedEventArgs e)
    {
        var isMiddleClick = e is Microsoft.UI.Xaml.Input.PointerRoutedEventArgs ptrArgs &&
            ptrArgs.GetCurrentPoint(null).Properties.IsMiddleButtonPressed;
    }

    private async void UserNoteBtn_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var button = sender as Button;
        if (button is null) return;
        var uNote = button.DataContext as UserNoteModelView;
        if (uNote == null) return;
        var content = button?.Content as string;
        var isRightClick = e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed;
        if (isRightClick)
        {
            var contentDialog = new ContentDialog
            {
                Title = $"Edit User Note {content}",
                Content = new TextBox
                {
                    Text = content ?? string.Empty,
                    AcceptsReturn = true,
                    Height = 200,
                    Width = 400,
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Clear",
                CloseButtonText = "Cancel"
            };
            var result = await contentDialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.None:
                    break;
                case ContentDialogResult.Primary:
                    await MyViewModel.UpdateSongNoteWithGivenNoteModelView(MyViewModel.SelectedSong,uNote);
                    return;
                case ContentDialogResult.Secondary:
                    await MyViewModel.RemoveSongNoteById(DetailedSong!, uNote);
                    break;
                default:
                    break;
            }
        }
    }

    private async void ChooseImageFromOtherSongsInAlbum_Click(object sender, RoutedEventArgs e)
    {
        try
        {

            var realm = MyViewModel.RealmFactory.GetRealmInstance();

            var dbSong = realm.Find<SongModel>(DetailedSong!.Id);
            GridOfOtherImages.Visibility = WinUIVisibility.Visible;
            //MyImagePickerBtn
            if(dbSong is null) return;
            if (dbSong.Album is null)
            {
                var albumFromDB =
                    realm.All<AlbumModel>()
                    .FirstOrDefault(a => a.Name == dbSong.AlbumName);
                if(albumFromDB is null) return;
                ImagesFromOtherSongsItemsRepeater.ItemsSource = albumFromDB.SongsInAlbum?.ToList().Select(s=>s.CoverImagePath);

                await MyViewModel.AssignSongToAlbumAsync((MyViewModel.SelectedSong, albumFromDB.Name));
            }
            else
            {
                IEnumerable<string?>? albumPathsFromOtherSongsIngAlbum =
                    dbSong?.Album.SongsInAlbum?.ToList().Select(s => s.CoverImagePath);
                ImagesFromOtherSongsItemsRepeater.ItemsSource = albumPathsFromOtherSongsIngAlbum;

            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void ChooseImageFromFileSystem_Click(object sender, RoutedEventArgs e)
    {
        await MyViewModel.PickAndApplyImageToSong(MyViewModel.SelectedSong);
    }

    private void RemoveImageFromSong_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SelectedSong?.CoverImagePath = string.Empty;
    }

    private void Image_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var img = sender as Microsoft.UI.Xaml.Controls.Image;
        var pathh = img?.DataContext.ToString();
        MyViewModel.SelectedSong?.CoverImagePath = pathh;
    }

   
    private void GridOfOtherImagesCloseButton_Click(object sender, RoutedEventArgs e)
    {
        GridOfOtherImages.Visibility= WinUIVisibility.Collapsed;
    }

    private async void ArtistBtn_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var
            button = sender as Button;
        var artistModel = button?.DataContext as ArtistModelView;
        var clipBoardText = artistModel?.Name;
        if (clipBoardText is null) return;
        var dataPackage = new DataPackage();
        dataPackage.SetText(clipBoardText);
        Clipboard.SetContent(dataPackage);
        await ToastToolTipNotif2Secs(button);
    }

    private static async Task ToastToolTipNotif2Secs(UIElement? Elt)
    {
        if(Elt is null) return;
        var toolTipContent = new ToolTip
        {
            Content = "Artist name copied to clipboard!"
        };
        ToolTipService.SetToolTip(Elt!, toolTipContent);
        toolTipContent.IsOpen = true;
        await Task.Delay(2000);
        toolTipContent.IsOpen = false;
        ToolTipService.SetToolTip(Elt!, null);
    }

    private async void AddNoteToSong_Click(object sender, RoutedEventArgs e)
    {
        await MyViewModel.AddNoteToSongAsync();
    }
    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props.IsXButton1Pressed)
        {
            if (Frame.CanGoBack)
            {

                Frame.GoBack();
            }
        }
    }
    private async void SaveChangeBtn_Click(object sender, RoutedEventArgs e)
    {
        var vis = ElementCompositionPreview.GetElementVisual(PageNotificationText);

        MyViewModel.UpdateSongInDB(MyViewModel.SelectedSong!);
        Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        PageNotificationText.Text = "Changes saved!";
        PlatUtils.ApplyEntranceEffect(vis, PageNotificationText,SongTransitionAnimation.Spring , _compositor);

        await Task.Delay(1600); // slight delay to ensure smoothness
        PageNotificationText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {

        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(detailedImage) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", detailedImage);
            }
        }
        base.OnNavigatingFrom(e);

    }

  
}
