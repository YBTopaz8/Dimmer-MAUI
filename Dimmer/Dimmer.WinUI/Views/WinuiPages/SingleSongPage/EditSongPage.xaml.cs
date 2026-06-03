
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
    LastFMViewModel MyLastFMViewModel { get; set; }
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
              
            }
        }
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

    IEnumerable<ArtistModel>? listOfArtistsModel;
    private void ArtistToSongDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedSong is null) return;
        var dbSong = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<SongModel>(MyViewModel.SelectedSong.Id);
        if (dbSong is null) return;
        var artistToSong = dbSong.ArtistToSong;
        listOfArtistsModel = artistToSong;
        
        ArtistToSongDataGrid.ItemsSource =listOfArtistsModel;
        ArtistToSongDataGrid.SelectedItems.AddRange( listOfArtistsModel);
        //ArtistToSongDataGrid.ItemsSource = listOfArtistsModel;
    }

    private void ArtistNameFromAllArtistsBtn_Click(object sender, RoutedEventArgs e)
    {

    }

 

    private void ChooseImageFromOtherSongsInAlbum_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        if (MyImagePickerBtn.Flyout is not null)
        {
            MyImagePickerBtn.Flyout.ShowAt(sender);
        }
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

    
    private void ArtistToSongDataGrid_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }

    private async void EditArtistsToSong_Click(object sender, RoutedEventArgs e)
    {
        var comParam = EditArtistsToSong.CommandParameter as string;
        if (comParam is "Edit")
        {

       
            ArtistToSongDataGrid.SelectedItems.Clear();
        
            ArtistToSongDataGrid.ItemsSource = MyViewModel.ArtistsCollection;

            if (listOfArtistsModel is null) return;
            ArtistToSongDataGrid.SelectedItems.AddRange(listOfArtistsModel);

            EditArtistsToSong.CommandParameter = "Save";
            FontIcon icon = new FontIcon();
            icon.Glyph = "\uE74E";
            EditArtistsToSong.Content = icon;
        }
        else
        {
            var contentDialog = new ContentDialog();
            var allArtists = listOfArtistsModel?.Select(x=>x.Name).ToList();
            var contentText = $"Save Artists {allArtists} to song?";
            contentDialog.Content= contentText;
            contentDialog.PrimaryButtonText= "OK";
            contentDialog.CloseButtonText = "Cancel";

            contentDialog.PrimaryButtonClick += SaveArtistsToSongClickConfirm;
            await contentDialog.ShowAsync();
        }
    }

    private void SaveArtistsToSongClickConfirm(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        //
    }

    private void SongNameTB_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        
    }


    private async void SearchOnLastFM_Click(object sender, RoutedEventArgs e)
    {
        LastFMSearchStackPanel.Visibility = Visibility.Visible;
        this.PartTwoSection.Visibility = Visibility.Collapsed;
        MyViewModel.IsSearchingOnLastFM = true;

        MyViewModel.InfoFromLastFM = await MyViewModel.LastfmService.GetTrackInfoAsync(SongArtistNameTB.Text,SongNameTB.Text);
        MyViewModel.IsSearchingOnLastFM = false;

        if (MyViewModel.InfoFromLastFM is null || MyViewModel.InfoFromLastFM.IsNull)
        {
            PartTwoSection.Visibility = Visibility.Visible;
            LastFMSearchStackPanel.Visibility = Visibility.Collapsed;
            return;
        }
        MyViewModel.InfoFromLastFM.Artist = await MyViewModel.LastfmService.GetArtistInfoAsync(MyViewModel.InfoFromLastFM.Artist.Name);
        if (MyViewModel.InfoFromLastFM.Artist is not null)
            MyViewModel.InfoFromLastFM.Album = await MyViewModel.LastfmService.GetAlbumInfoAsync(MyViewModel.InfoFromLastFM.Artist.Name, MyViewModel.SelectedSong?.AlbumName);



    }

    private async void SaveInfoFromLastFMToSongInDb_Click(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.SelectedSong is null || MyViewModel.InfoFromLastFM is null) return;
        MyViewModel.SelectedSong.Title = MyViewModel.InfoFromLastFM.Name;
        MyViewModel.SelectedSong.IsFavorite = MyViewModel.InfoFromLastFM.UserLoved;

        var artInVM = MyViewModel.ArtistsCollection.FirstOrDefault(x => x.Name == MyViewModel.InfoFromLastFM.Artist.Name);
        var albInVM = MyViewModel.AlbumsCollection.FirstOrDefault(x => x.Name == MyViewModel.InfoFromLastFM.Album.Name);
        if (artInVM is not null)
        {
            MyViewModel.SelectedSong.Artist = artInVM;
        }
        else
        {
            var realm = MyViewModel.RealmFactory.GetRealmInstance();
            await realm.WriteAsync(() =>
            {
                ArtistModel newArtist = new ArtistModel();
                newArtist.Name = MyViewModel.InfoFromLastFM.Artist.Name;

                var ImgObj = MyViewModel.InfoFromLastFM.Artist.Images.LastOrDefault(x => string.IsNullOrEmpty(x.Size));
                if (ImgObj is not null)
                {
                    newArtist.ImagePath = ImgObj.Url;
                }
                newArtist.Bio = MyViewModel.InfoFromLastFM.Artist.Biography.Summary;
                newArtist.Url = MyViewModel.InfoFromLastFM.Url;
                var dbObjArt = realm.Add(newArtist, true);
                var songInDB = realm.Find<SongModel>(MyViewModel.SelectedSong.Id);

                if (songInDB is not null)
                {
                    songInDB.Artist = newArtist;
                    if (!songInDB.ArtistToSong.Contains(dbObjArt))
                    {
                        songInDB.ArtistToSong.Add(dbObjArt);
                    }
                }
                RxSchedulers.UI.ScheduleTo(() =>
                {
                    MyViewModel.SelectedSong.Artist = dbObjArt.ToArtistModelView();

                });
            });
        }
        if (albInVM is not null)
        {
            MyViewModel.SelectedSong.Album = albInVM;

        }

        else
        {
            AlbumModel newAlbum = new AlbumModel();
            var ImgObj = MyViewModel.InfoFromLastFM.Artist.Images.LastOrDefault(x => string.IsNullOrEmpty(x.Size));
            if (ImgObj is not null)
            {
                newAlbum.ImagePath = ImgObj.Url;
            }

            newAlbum.Name = MyViewModel.InfoFromLastFM.Album.Name;
            newAlbum.Url = MyViewModel.InfoFromLastFM.Album.Url;

            var dbObjAlbm = MyViewModel.RealmFactory.GetRealmInstance().Add(newAlbum, true);
            MyViewModel.SelectedSong.Album = dbObjAlbm.ToAlbumModelView();

        }

        var SongInBD = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(MyViewModel.SelectedSong.Id);
        if (SongInBD is not null)
        { 
            await MyViewModel.RealmFactory.GetRealmInstance().WriteAsync(() =>
            {
                SongInBD.Title = MyViewModel.SelectedSong.Title;
                SongInBD.Artist = MyViewModel.SelectedSong.Artist.ToArtistModel()!;
                SongInBD.Album = MyViewModel.SelectedSong.Album.ToAlbumModel()!;
                var ImgObj = MyViewModel.InfoFromLastFM.Images.LastOrDefault(x => string.IsNullOrEmpty(x.Size));
                if (ImgObj is not null)
                {
                    SongInBD.CoverImagePath = ImgObj.Url;
                }

            });
        }

    }

    private void ToggleButton_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void ToggleSearchSongLastFM_Checked(object sender, RoutedEventArgs e)
    {

        SearchSongLastFMExp.IsExpanded = true;
        SearchSongLastFMExp.Visibility = Visibility.Visible;
    }

    private void ToggleSearchSongLastFM_Unchecked(object sender, RoutedEventArgs e)
    {
        SearchSongLastFMExp.IsExpanded = false;
        SearchSongLastFMExp.Visibility = Visibility.Collapsed;
        //SearchSongLastFMExp.
    }
}
