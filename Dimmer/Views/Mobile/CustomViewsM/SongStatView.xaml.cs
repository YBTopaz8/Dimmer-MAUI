namespace Dimmer_MAUI.Views.Mobile.CustomViewsM;

public partial class SongStatView : ContentView
{
	public SongStatView()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        BindingContext = MyViewModel;
        
    }
    public HomePageVM MyViewModel { get; }

    private async void ShareStatBtn_Clicked(object sender, EventArgs e)
    {
        ShareStatBtn.IsVisible = false;
        //FavSong.IsVisible = false;
        string shareCapture = "viewToShare.png";
        string filePath = Path.Combine(FileSystem.CacheDirectory, shareCapture); 

        await CaptureCurrentViewAsync(OverViewSection, filePath);

        await ShareScreenshot(filePath);

        ShareStatBtn.IsVisible = true;
        //FavSong.IsVisible = true;
    }

    public static async Task CaptureCurrentViewAsync(VisualElement view, string filePath)
    {
        IScreenshotResult? screenshot = await view.CaptureAsync();

        if (screenshot != null)
        {
            using (Stream stream = await screenshot.OpenReadAsync())
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }
        }

        await ShareScreenshot(filePath);
    }

    private static async Task ShareScreenshot(string filePath)
    {
        
        await Share.RequestAsync(new ShareFileRequest
        {
            
            Title = "Check out my Stats!",
            
            File = new ShareFile(filePath)
        });
    }

    private void FavSong_Clicked(object sender, EventArgs e)
    {

    }
    
}