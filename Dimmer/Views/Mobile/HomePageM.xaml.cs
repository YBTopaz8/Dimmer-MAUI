using CommunityToolkit.Maui.Core.Platform;
using Plainer.Maui.Controls;
using UraniumUI.Material.Attachments;

namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : UraniumContentPage
{
    
    public HomePageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        SearchBackDrop.PropertyChanged += SearchBackDrop_PropertyChanged;
    }

    private async void SearchBackDrop_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsPresented")
        {
            var backDrop = sender as BackdropView;
            var searchSongTextField = SearchSongSB.Content as EntryView;
            if (backDrop != null)
            {
                if (backDrop.IsPresented)
                {
                    SearchSongSB.Focus();
                    await searchSongTextField!.ShowKeyboardAsync();
                }
                else
                {
                    SearchSongSB.Unfocus();
                    await searchSongTextField!.HideKeyboardAsync();
                }
            }
        }
    }

    public HomePageVM HomePageVM { get; }
    
    private void SaveViewButton_Clicked(object sender, EventArgs e)
    { //to capture views into a png , will be useful later for saving
        
        //var image = await btmcontrols.CaptureAsync();
        //var savePath = Path.Combine("/storage/emulated/0/Documents", "test.png");
        //using Stream fileStream = File.OpenWrite(savePath);
        //await image.CopyToAsync(fileStream, ScreenshotFormat.Png);

    }
   
    
    DateTime lastKeyStroke;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        lastKeyStroke = DateTime.Now;
        var thisKeyStroke = lastKeyStroke;
        await Task.Delay(750);
        if (thisKeyStroke == lastKeyStroke)
        {
            var searchText = e.NewTextValue;
            if (searchText.Length >= 2)
            {
                HomePageVM.SearchSongCommand.Execute(searchText);
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);
            }
        }        
    }

    private void SearchFAB_Clicked(object sender, EventArgs e)
    {
        //SearchBackDrop.IsPresented = !SearchBackDrop.IsPresented;
        SongsColView.ScrollTo(HomePageVM.PickedSong, position:ScrollToPosition.Center, animate: true);
    }

    private async void PlaybackBottomBar_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        //HomePageVM.LoadSongCoverImage();
        if (HomePageVM.IsPlaying)
        {
            playImgBtn.IsVisible = false;
            pauseImgBtn.IsVisible = true;
        }
        else
        {
            playImgBtn.IsVisible = true;
            pauseImgBtn.IsVisible = false;
        }
#if ANDROID
        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
#endif
    }
    private void playImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = false;
        pauseImgBtn.IsVisible = true;
    }

    private void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = true;
        pauseImgBtn.IsVisible = false;
    }

    private void SpecificSong_Tapped(object sender, TappedEventArgs e)
    {
        playImgBtn.IsVisible = false;
        pauseImgBtn.IsVisible = true;
    }
}