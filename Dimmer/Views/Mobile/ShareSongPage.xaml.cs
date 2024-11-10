using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;
public partial class ShareSongPage : ContentPage
{
    public ShareSongPage()
    {
        InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        this.BindingContext = HomePageVM;
    }
    
    SongsModelView currentsong;
    HomePageVM HomePageVM { get; set; }
    bool hasLyrics;
    ObservableCollection<LyricPhraseModel>? sharelyrics=new();
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }

        (hasLyrics, sharelyrics) = LyricsService.HasLyrics(HomePageVM.SelectedSongToOpenBtmSheet);

        LyricsColView.ItemsSource = sharelyrics;
        if (sharelyrics.Count > 1)
        {
            addLyrText.IsVisible = true;
        }
        string? str = HomePageVM.SelectedSongToOpenBtmSheet.CoverImagePath;
        if (!string.IsNullOrEmpty(str))
        {            
            currentsong = HomePageVM.SelectedSongToOpenBtmSheet;
            
        }
        else
        {
            myPage.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Shell.SetTabBarIsVisible(this, true);
    }

    private async void OnShareButtonClicked(object sender, EventArgs e)
    {

        UtilsHSL.IsVisible = false;
        //myPage.IsEnabled = false;
        var screenshot = await myPage.CaptureAsync();
        if (screenshot != null)
        {

            var directoryPath = Path.Combine("/storage/emulated/0/Documents", "Dimmer");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var savePath = Path.Combine(directoryPath, $"DimmerStory_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
            using Stream fileStream = File.OpenWrite(savePath);
            await screenshot.CopyToAsync(fileStream, ScreenshotFormat.Png);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Currently Listening To {currentsong.Title} by {currentsong.ArtistName} on Dimmer",
                File = new ShareFile(savePath)
            });


        }
        UtilsHSL.IsVisible=true;

    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        UtilsHSL.IsVisible = false;
        //myPage.IsEnabled = false;
        var screenshot = await myPage.CaptureAsync();
        if (screenshot != null)
        {

            var directoryPath = Path.Combine("/storage/emulated/0/Documents", "Dimmer");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var savePath = Path.Combine(directoryPath, $"DimmerStory_{DateTime.Now:yyyy-MM-dd_HH-mm}.png");
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
            using Stream fileStream = File.OpenWrite(savePath);
            await screenshot.CopyToAsync(fileStream, ScreenshotFormat.Png);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Currently Listening To {currentsong.Title} by {currentsong.ArtistName} on Dimmer",
                File = new ShareFile(savePath)
            });


        }
        // Re-show the Share button
        UtilsHSL.IsVisible = true;
    }

    string singleImgSource = string.Empty;
    
    public void ToggleMode()
    {

        isDarkByDefault = !isDarkByDefault;
        if (isDarkByDefault)
        {
            //now go light
            HighlightedLyricBtn.TextColor = Microsoft.Maui.Graphics.Colors.Black;
            
            HighlightedLyricBtn.BackgroundColor= Microsoft.Maui.Graphics.Colors.White;
            HighlightedLyricBtn.BorderColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        }
        else
        {
            //now go dark
            HighlightedLyricBtn.TextColor = Microsoft.Maui.Graphics.Colors.White;
            HighlightedLyricBtn.BackgroundColor = Microsoft.Maui.Graphics.Colors.Black;
        }
    }

    bool isDarkByDefault = true;

    

    string customImgPath = string.Empty;
    private async void AddUserImg_CheckedChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            var res = await FilePicker.Default.PickMultipleAsync(
            new PickOptions()
            {
                PickerTitle = "Select Image To Share",
                FileTypes = FilePickerFileType.Images,
            });
            if (res != null)
            {
                foreach (var imgPicked in res)
                {
                    HomePageVM.PhotoDumps.Add(imgPicked.FullPath);
                }
            }
        }        
        else
        {
            if (!string.IsNullOrEmpty(customImgPath))
            {
                var ress = await Shell.Current.DisplayAlert("Confirm Action", "Are you sure you want to remove the custom image?", "Yes", "No");

                if (ress)
                {
                    customImgPath = string.Empty;
                    //SharePageImg.Source = HomePageVM.SelectedSongToOpenBtmSheet.CoverImagePath;
                    myPage.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                }
            };
        }
    }
    private async Task<bool> ConfirmActionPopup(string action)
    {
        var ress = await Shell.Current.DisplayAlert("Confirm Action", $"Are you sure you want to {action}?", "Yes", "No");

        return ress;
    }

    private void AddLyrText_Clicked(object sender, EventArgs e)
    {
        if (sharelyrics?.Count < 1)
        {
            sharelyrics = LyricsService.LoadSynchronizedAndSortedLyrics(HomePageVM.SelectedSongToOpenBtmSheet.FilePath);

            LyricsColView.ItemsSource = sharelyrics;
        }
        LyricPickerBtmSheet.Show();
    }

    private async void ToggleSongCoverBigSmoll_Clicked(object sender, EventArgs e)
    {
        bool IsNowGoingVisible = StoryBigContent.IsVisible;

        if (IsNowGoingVisible)
        {
            await Task.WhenAll( StoryBigContent.AnimateFadeOutBack(500),
                StorySmallContent.AnimateFadeInFront(500));
        }
        else
        {
            await Task.WhenAll(StoryBigContent.AnimateFadeInFront(500), 
                StorySmallContent.AnimateFadeOutBack(500));
        }
    }

    private void LyricsColView_TapConfirmed(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var ee = e.Item as LyricPhraseModel;
        
        
        LyricPickerBtmSheet.Close();
        HighlightedLyricBtn.Content = ee.Text;
        HighlightedLyricBtn.IsVisible = true;
    }

    private void rmvLyr_Clicked(object sender, EventArgs e)
    {
        HighlightedLyricBtn.Content = string.Empty;
        HighlightedLyricBtn.IsVisible = false;
        LyricPickerBtmSheet.Close();
    }

    private void OpacitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        StorySmallContent.Opacity = e.NewValue;
        StoryBigContent.Opacity = e.NewValue;
    }
    private void HeightSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (StorySmallContent.IsVisible)
        {
            StorySmallContent.TranslationY =+ e.NewValue;
        }
        else if(StoryBigContent.IsVisible)
        {
            StoryBigContent.TranslationY = +e.NewValue;
        }
    }

    private void DragDropGest_PanUpdated(object sender, PanUpdatedEventArgs e) => HandleDrag(sender, e);

    private void HandleDrag(object sender, PanUpdatedEventArgs e)
    {
        var view = sender as View;
        double xOffset = view.TranslationX;
        double yOffset = view.TranslationY;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                break;

            case GestureStatus.Running:
                view.TranslationX = xOffset + e.TotalX;
                view.TranslationY = yOffset + e.TotalY;
                break;

            case GestureStatus.Completed:
                break;
        }
    }

    double currentScale = 1;
    double startScale = 1;
    private void PinchGestureRecognizer_PinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        var ZoomableLabel = sender as View;

        if (e.Status == GestureStatus.Started)
        {
            // Store the initial scale and anchor point when the pinch begins
            startScale = currentScale;
            ZoomableLabel.AnchorX = e.ScaleOrigin.X;
            ZoomableLabel.AnchorY = e.ScaleOrigin.Y;
        }
        else if (e.Status == GestureStatus.Running)
        {
            // Calculate the scale factor relative to the starting scale
            double newScale = startScale * e.Scale;

            // Ensure the scale does not go below the original scale (1.0)
            newScale = Math.Max(1.0, newScale);

            // Apply the new scale to the view
            ZoomableLabel.Scale = newScale;
            currentScale = newScale; // Update current scale
        }
        else if (e.Status == GestureStatus.Completed)
        {
            // Store the completed scale for future reference
            currentScale = ZoomableLabel.Scale;
        }
    }

    private async void ToggleBGImg_ChipTap(object sender, ChipEventArgs e)
    {
        var send = sender as ChoiceChipGroup;
        switch (send.SelectedIndex)
        {
            case 0: //solid color
                customImgPath = string.Empty;
                
                myPage.BackgroundColor = Microsoft.Maui.Graphics.Colors.Black;
                break;
            case 1: //song cover as bg
                customImgPath = string.Empty;
                HomePageVM.PhotoDumps.Add(HomePageVM.SelectedSongToOpenBtmSheet.CoverImagePath);

                myPage.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                break;
            case 2: // one img
                HomePageVM.PhotoDumps.Clear();
                customImgPath = string.Empty;
                if (await ConfirmActionPopup("Pick Mulitple Images"))
                {
                    var res = await FilePicker.Default.PickAsync(
                    new PickOptions()
                    {
                        PickerTitle = "Select Image To Share",
                        FileTypes = FilePickerFileType.Images,
                    });
                    if (res != null)
                    {
                        HomePageVM.PhotoDumps.Add(res.FullPath);                        
                    }
                    SharePageColViewImg.ItemSpanCount = 1;
                }
                break;
            case 3:
                HomePageVM.PhotoDumps.Clear();
                if (await ConfirmActionPopup("Pick Mulitple Images"))
                {
                    var res = await FilePicker.Default.PickMultipleAsync(
                    new PickOptions()
                    {
                        PickerTitle = "Select Image To Share",
                        FileTypes = FilePickerFileType.Images,
                    });
                    if (res != null)
                    {
                        
                        foreach (var imgPicked in res)
                        {
                            HomePageVM.PhotoDumps.Add(imgPicked.FullPath);
                        }
                    }
                    SharePageColViewImg.ItemSpanCount = res.Count();
                }
                break;
            default:
                break;
        }
    }

    private async void ToggleSongCard_ChipTap(object sender, ChipEventArgs e)
    {
        var send = sender as ChoiceChipGroup;
        switch (send.SelectedIndex)
        {
            case 0: //no cover
                StoryBigContent.IsVisible = false;
                StorySmallContent.IsVisible = false;
                break;
            case 1: //smoll cover

                await Task.WhenAll(StoryBigContent.AnimateFadeOutBack(500),
                    StorySmallContent.AnimateFadeInFront(500));

                break;
            case 2: // big cover
                await Task.WhenAll(StoryBigContent.AnimateFadeInFront(500),
                    StorySmallContent.AnimateFadeOutBack(500));
                break;
            default:
                break;
        }
    }

    private void SolidBGColor_TapReleased(object sender, DXTapEventArgs e)
    {
        
    }
}
