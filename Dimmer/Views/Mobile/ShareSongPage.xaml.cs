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
    
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        Shell.SetTabBarIsVisible(this, false);

        string? str = vm.SelectedSongToOpenBtmSheet.CoverImagePath;
        currentsong = vm.SelectedSongToOpenBtmSheet;
        // Open a file stream
        using var stream = File.OpenRead(str);
#if ANDROID
        var colors = await PlatSpecificUtils.GetDominantColorsAsync(stream);
        
        if (colors != null && colors.Length > 1)
        {
            // Full brightness dominant color for the top
            var topColor = colors[0].WithLuminosity(1.0f);  // Full brightness

            // Slightly darker or neutral color in the middle
            var middleColor = colors[1].WithLuminosity(0.5f);  // Adjust for mid-tone

            // Black color at the bottom
            var bottomColor = Colors.Black;

            myPage.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop { Color = topColor, Offset = 0.0f },
                    new GradientStop { Color = middleColor, Offset = 0.5f },
                    new GradientStop { Color = bottomColor, Offset = 1.0f }
                }
            };
        }
        else
        {
            myPage.BackgroundImageSource = str;
        }
#endif
    }

  

    private async void OnShareButtonClicked(object sender, EventArgs e)
    {
        // Temporarily hide the Share button for capturing
        SharingActIndic.IsVisible = true;
        SharingActIndic.IsRunning = true;
        ShareButton.IsVisible = false;
        myPage.IsEnabled = false;
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
        ShareButton.IsVisible = true;
        SharingActIndic.IsVisible = false;
        SharingActIndic.IsRunning = false;

        myPage.IsEnabled = true;
    }

}
