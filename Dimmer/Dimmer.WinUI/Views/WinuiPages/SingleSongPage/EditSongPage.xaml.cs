
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.WinUI;
using Dimmer.Utilities.Extensions;
using Dimmer.WinUI.ViewModel.SingleSongVMSection;
using Dimmer.WinUI.Views.CustomViews.WinuiViews;
using Dimmer.WinUI.Views.CustomViews.WinuiViews.SingleSongSection;
using DynamicData;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.DataTransfer;
using WinRT;
using WinRT.Dimmer_WinUIVtableClasses;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;
using DataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using DataTemplate = Microsoft.UI.Xaml.DataTemplate;
using FrameworkElement = Microsoft.UI.Xaml.FrameworkElement;
using ListView = Microsoft.UI.Xaml.Controls.ListView;
using ListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using PropertyPath = Microsoft.UI.Xaml.PropertyPath;
using RoutedEventArgs = Microsoft.UI.Xaml.RoutedEventArgs;
using TextWrapping = Microsoft.UI.Xaml.TextWrapping;
using Visibility = Microsoft.UI.Xaml.Visibility;
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
                _editViewModel = new EditSongViewModel(vm, vm.SelectedSong!);
                MyViewModel.SelectedSong = DetailedSong;
                await MyViewModel.LoadSelectedSongLastFMData();
                //LoadUiComponents();
                SetupArtistBindings();
            }
        }
    }
    private void SetupArtistBindings()
    {
        // Bind AllArtistsIR to EditViewModel.AllArtists
        AllArtistsIR.ItemsSource = _editViewModel.AllArtists;

        // Bind selected artists
        ArtistsToBeAdded.ItemsSource = _editViewModel.SelectedArtists;
    }
    private EditSongViewModel _editViewModel;
    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            AnimationHelper.Prepare(AnimationHelper.Key_Backward,
                detailedImage,
                AnimationHelper.ConnectedAnimationStyle.ScaleDown
                );
            Frame.GoBack();
        }
      
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
                    await MyViewModel.UpdateSongNoteWithGivenNoteModelView(MyViewModel.SelectedSong!,uNote);
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
        _editViewModel.EditingSong.CoverImagePath = string.Empty;
    }

    private void Image_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Image img && img.DataContext is string path)
        {
            _editViewModel.EditingSong.CoverImagePath = path;
        }
    }

    private void GridOfOtherImagesCloseButton_Click(object sender, RoutedEventArgs e)
    {
        GridOfOtherImages.Visibility= WinUIVisibility.Collapsed;
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
            BackButton_Click(sender, e);
            
        }
    }


    private async Task ShowChangesReviewPopup()
    {
        // Create and show the review popup
        var popup = new ChangesReviewPopup
        {
            ViewModel = _editViewModel,
            
            Width = 600,
            Height = 700
        };

        //var popupResult = await popup.ShowAsync();

        //if (popupResult == ContentDialogResult.Primary) // Save clicked
        //{
        //    await _editViewModel.SaveAcceptedChangesAsync();
        //    ShowNotification("Changes saved successfully!");

        //    // Update current playing song if needed
        //    if (MyViewModel.CurrentPlayingSongView?.TitleDurationKey ==
        //        _editViewModel.OriginalSong.TitleDurationKey)
        //    {
        //        MyViewModel.CurrentPlayingSongView?.CoverImagePath =
        //            _editViewModel.OriginalSong.CoverImagePath;
        //    }
        //}
        //else if (popupResult == ContentDialogResult.Secondary) // Cancel clicked
        //{
        //    _editViewModel.DiscardAllChanges();
        //    ShowNotification("Changes discarded");
        //}
    }
    private void ShowNotification(string message)
    {
        PageNotificationText.Text = message;
        PageNotificationText.Visibility = Visibility.Visible;

        var vis = ElementCompositionPreview.GetElementVisual(PageNotificationText);
        PlatUtils.ApplyEntranceEffect(vis, PageNotificationText,
            SongTransitionAnimation.Fade, _compositor);

        _ = Task.Delay(2600).ContinueWith(_ =>
        {
            DispatcherQueue.TryEnqueue(() =>
                PageNotificationText.Visibility = Visibility.Collapsed);
        });
    }

    private async void SaveChangeBtn_Click(object sender, RoutedEventArgs e)
    {
        var vis = ElementCompositionPreview.GetElementVisual(PageNotificationText);
        if (IsInstrumentalBox.IsChecked.HasValue)
        {
            _editViewModel.EditingSong.IsInstrumental = IsInstrumentalBox.IsChecked.Value;
        }

        // Show review popup if there are changes
        if (_editViewModel.HasChanges)
        {
            await ShowChangesReviewPopup();
        }
        else
        {
            // No changes, just show notification
            ShowNotification("No changes to save");
        }
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {

        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            if (detailedImage != null && VisualTreeHelper.GetParent(detailedImage) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", detailedImage);
            }
        }
        base.OnNavigatingFrom(e);

    }

    IEnumerable<ArtistModelView?>? listOfArtistsModelView;
    private void ArtistToSong_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedSong is null) return;
        var dbSong = MyViewModel.RealmFactory.GetRealmInstance()
            .All<SongModel>()
            .FirstOrDefault(s => s.Id == MyViewModel.SelectedSong.Id);
        if (dbSong is null) return;
        var artistToSong = dbSong.ArtistToSong;
        listOfArtistsModelView = artistToSong.AsEnumerable().Select(x=>x.ToArtistModelView());
        foreach (var art in listOfArtistsModelView)
        {
            var songs = artistToSong.FirstOrDefault(x => x.Id == art.Id)?
                .Songs;
            art.SongsByArtist = songs.AsEnumerable().Select(x=>x.ToSongModelView()).ToObservableCollection()!;
        }
        ArtistToSong.ItemsSource = listOfArtistsModelView;
    }

    private void ArtistNameFromAllArtistsBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void AddArtist_Click(object sender, RoutedEventArgs e)
    {
        // Toggle visibility logic
        if (AddArtistGrid.Visibility == Visibility.Visible)
        {
            AddArtistGrid.Visibility = Visibility.Collapsed;
            (AddArtist.Content as FontIcon).Glyph = "\uE8FA";
        }
        else
        {
            AddArtistGrid.Visibility = Visibility.Visible;
            (AddArtist.Content as FontIcon).Glyph = "\uE711";

            // Load artists if needed
            if (AllArtistsIR.ItemsSource == null)
            {
                AllArtistsIR.ItemsSource = _editViewModel.AllArtists;
            }
        }
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

    List<string>? selectedItems;
    private void AllArtistsIR_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var added = e.AddedItems.Cast<string>();
        var removed = e.RemovedItems.Cast<string>();

        foreach (var artist in added)
            _editViewModel.AddArtist(artist);

        foreach (var artist in removed)
            _editViewModel.RemoveArtist(artist);
    }

    private void ResetFieldsBtn_Click(object sender, RoutedEventArgs e)
    {

        SongTitleSearch.Text = string.Empty;
        SongArtistNameSearch.Text = string.Empty;
        SongAlbumNameSearch.Text = string.Empty;
    }

    private void PrefillFieldsBtn_Click(object sender, RoutedEventArgs e)
    {
        SongTitleSearch.Text = _editViewModel.EditingSong.Title;
        SongArtistNameSearch.Text = _editViewModel.EditingSong.ArtistName;
        SongAlbumNameSearch.Text = _editViewModel.EditingSong.AlbumName;
    }

    private void DetailedImage_Loaded(object sender, RoutedEventArgs e)
    {
        // The Helper handles the null checks, the opacity setting, 
        // and the DispatcherQueue automatically.

        AnimationHelper.TryStart(
            detailedImage,
            new List<UIElement>() { BackBtn },
            // Pass all possible keys here. 
            // The helper loops through them and starts the FIRST one it finds.
            "SwingFromSongDetailToEdit",      // Priority 1: Coming from Edit Page
            AnimationHelper.Key_ListToDetail, // Priority 2: Coming from List
            AnimationHelper.Key_ArtistToSong  // Priority 3: Coming from Artist
        );
    }
}
