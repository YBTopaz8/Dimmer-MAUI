
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.WinUI;

using Dimmer.Utilities.Extensions;
using Dimmer.WinUI.Views.CustomViews.WinuiViews;

using DynamicData;

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
        var ischecked = IsInstrumentalBox.IsChecked;
        if(ischecked is not null)
        {
            MyViewModel.SelectedSong?.IsInstrumental = (bool)ischecked;
        }
        if(MyViewModel.CurrentPlayingSongView.TitleDurationKey == MyViewModel.SelectedSong.TitleDurationKey)
        {
            MyViewModel.CurrentPlayingSongView.CoverImagePath ??= MyViewModel.SelectedSong.CoverImagePath;
        }
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

    private void ArtistToSong_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedSong is null) return;
        var dbSong = MyViewModel.RealmFactory.GetRealmInstance()
            .All<SongModel>()
            .FirstOrDefault(s => s.Id == MyViewModel.SelectedSong.Id);
        if (dbSong is null) return;
        var artistToSong = dbSong.ArtistToSong;
        var listOfArtistsModelView = artistToSong.AsEnumerable().Select(x=>x.ToArtistModelView());
        foreach (var art in listOfArtistsModelView)
        {
            var songs = artistToSong.FirstOrDefault(x => x.Id == art.Id)?
                .Songs;
            art.SongsByArtist = songs.AsEnumerable().Select(x=>x.ToSongModelView()).ToObservableCollection();
        }
        ArtistToSong.ItemsSource = listOfArtistsModelView;
    }

    private void ArtistNameFromAllArtistsBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void AddArtist_Click(object sender, RoutedEventArgs e)
    {
        if (AddArtistGrid.Visibility == Microsoft.UI.Xaml.Visibility.Visible)
        {
            FontIcon addArt = new FontIcon();
            addArt.Glyph = "\uE8FA";
            AddArtist.Content = addArt;
            AddArtistGrid.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            return;
        }
        else
        {

            AddArtistGrid.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
        LoadAddArtistSectionView();

        FontIcon icon = new FontIcon();
        icon.Glyph = "\uE711";
        AddArtist.Content = icon;
    }

    private void LoadAddArtistSectionView()
    {
        var allArtistsInDb = MyViewModel.RealmFactory.GetRealmInstance()
            .All<ArtistModel>().ToList();
        var artistView = allArtistsInDb.ToList();

        PageScrollviewer.ChangeView(0, 210, 1);
        var OnlyArtName = allArtistsInDb
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .DistinctBy(x => x.Name)
                    .OrderBy(c => c.Name)
                   .Select(a => a.Name)
                    .ToList();
        AllArtistsIR.ItemsSource = OnlyArtName;

        var listOfOnlyFirstLetterOfArtist = OnlyArtName.Select(x => x.First()).Distinct();
        ListOfFirstLetters.ItemsSource = listOfOnlyFirstLetterOfArtist.ToList();
    }

    private void MyImagePickerBtn_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {

    }

    private void ChooseImageFromOtherSongsInAlbum_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        ChooseImageFromOtherSongsInAlbum_Click(sender, args);
    }
    private async void UniqueLetterToScrollTo_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var letter = (char)send.DataContext;

        // Find the first artist starting with that letter
        var artists = AllArtistsIR.ItemsSource as List<string>;
        if (artists == null || artists.Count == 0) return;

        var firstArtist = artists.FirstOrDefault(a => a.First() == letter);
        //var lastArtist = artists.LastOrDefault(a => !string.IsNullOrWhiteSpace(a));

        if (firstArtist == null) return;
        
        await AllArtistsIR.SmoothScrollIntoViewWithItemAsync(firstArtist);
    }


    private void RemoveArtistFromSelection_Clicked(object sender, RoutedEventArgs e)
    {
        var artNameFontIcon = (Button)sender;
        var artName = artNameFontIcon.DataContext as string;
        if(artName == null) return;
        selectedItems.Remove(artName);
    }

    private void ArtistToSong_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }

    List<string> selectedItems;
    private void AllArtistsIR_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var selectedStrings = e.AddedItems.Cast<string>().ToList();
        var unSelectedStrings = e.RemovedItems.Cast<string>().ToList();

        if (selectedItems is null && selectedStrings is not null)
        {        
            selectedItems =selectedStrings;
            ArtistsToBeAdded.ItemsSource = selectedStrings;
            return;
        }

        if (selectedStrings?.Count > 0)
        {            
            selectedItems?.Add(selectedStrings);
        }
        
        if (selectedItems is null)return;
        ArtistsToBeAdded.ItemsSource = selectedItems.ToList();

        if (unSelectedStrings.Count == 0)return;
        selectedItems.RemoveMany(unSelectedStrings);
        ArtistsToBeAdded.ItemsSource = selectedItems.ToList();


    }
}
