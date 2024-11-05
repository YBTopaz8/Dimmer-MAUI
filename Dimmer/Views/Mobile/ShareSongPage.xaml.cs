using DevExpress.Maui.Core;
using DevExpress.Maui.Editors;
using System.Diagnostics;

namespace Dimmer_MAUI.Views.Mobile;
public partial class ShareSongPage : ContentPage
{
    public ShareSongPage()
    {
        InitializeComponent();
        vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        this.BindingContext = vm;
    }
    SongsModelView currentsong;
    HomePageVM vm { get; set; }
    LinearGradientBrush bgBrush { get; set; }
    ObservableCollection<LyricPhraseModel>? sharelyrics=new();
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        sharelyrics = LyricsService.LoadSynchronizedAndSortedLyrics(vm.SelectedSongToOpenBtmSheet.FilePath);

        LyricsColView.ItemsSource = sharelyrics;
        if (sharelyrics.Count > 1)
        {
            addLyrText.IsVisible = true;
        }
        string? str = vm.SelectedSongToOpenBtmSheet.CoverImagePath;
        if (!string.IsNullOrEmpty(str))
        {            
            currentsong = vm.SelectedSongToOpenBtmSheet;
            SharePageImg.Source = str;
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
            var savePath = Path.Combine(directoryPath, $"DimmerStory_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
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
            var res = await FilePicker.Default.PickAsync(
            new PickOptions()
            {
                PickerTitle = "Select Image To Share",
                FileTypes = FilePickerFileType.Images
            });
            if (res is not null)
            {
                customImgPath = res.FullPath;
                SharePageImg.Source = res.FullPath;
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
                    SharePageImg.Source = vm.SelectedSongToOpenBtmSheet.CoverImagePath;
                }
            };
        }
    }

    private void ToggleDrawingMode_CheckedChanged(object sender, DevExpress.Maui.Core.ValueChangedEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            SharePageDV.IsEnabled = true;
            SharePageDV.IsVisible = true;
            
        }
        else
        {
            SharePageDV.IsEnabled = false;
            SharePageDV.IsVisible = false;
            SharePageDV.Lines.Clear();
        }
        Debug.WriteLine(SharePageDV.ZIndex);
        Debug.WriteLine(StoryBigContent.ZIndex);
    }

    private void ClearDrawing_Clicked(object sender, EventArgs e)
    {        
        SharePageDV.Lines.Clear();          
    }

    private void AddLyrText_Clicked(object sender, EventArgs e)
    {
        if (sharelyrics?.Count < 1)
        {
            sharelyrics = LyricsService.LoadSynchronizedAndSortedLyrics(vm.SelectedSongToOpenBtmSheet.FilePath);

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
}
