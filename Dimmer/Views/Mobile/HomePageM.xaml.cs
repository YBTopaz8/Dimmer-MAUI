using CommunityToolkit.Maui.Core.Platform;
using UraniumUI.Material.Attachments;

namespace Dimmer_MAUI.Views.Mobile;

public partial class HomePageM : UraniumContentPage
{
    public HomePageM(HomePageVM homePageVM)//, NowPlayingBottomPage NPBtmPage)
    {
        InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;

        nowPlayingBtmSheet.PropertyChanged += bottomView_PropertyChanged;

        SearchBackDrop.PropertyChanged += SearchBackDrop_PropertyChanged;
        
    }

    private async void SearchBackDrop_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsPresented")
        {
            var backDrop = sender as BackdropView;
            if (backDrop != null)
            {
                if (backDrop.IsPresented)
                {
                    SearchSongSB.Focus();
                    await SearchSongSB.ShowKeyboardAsync();
                }
                else
                {
                    SearchSongSB.Unfocus();
                    await SearchSongSB.HideKeyboardAsync();
                }
            }
        }
    }

    public HomePageVM HomePageVM { get; }
    public NowPlayingBottomPage NPBtmPage { get; }
    
    
    private void bottomView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "IsPresented")
        {
            var btmView = sender as BottomSheetView;
            if (btmView != null)
            {
                if (nowPlayingBtmSheet.IsPresented)
                {
                    nowPlayingBtmSheet.HeightRequest = 0;
                    nowPlayingBtmSheet.Header.IsVisible = false; 
                    nowPlayingBtmSheet.HeightRequest = myPage.Height;
                    SearchBackDrop.IsEnabled=false;
                }
                else
                {
                    nowPlayingBtmSheet.Header.IsVisible = true;
                    SearchBackDrop.IsEnabled = true;
                    Debug.WriteLine("Header request" +  nowPlayingBtmSheet.Header.Height);
                }
            }
        }
    }

    private async void SaveViewButton_Clicked(object sender, EventArgs e)
    { //to capture views into a png , will be useful later for saving
        
        var image = await btmcontrols.CaptureAsync();
        var savePath = Path.Combine("/storage/emulated/0/Documents", "test.png");
        using Stream fileStream = File.OpenWrite(savePath);
        await image.CopyToAsync(fileStream, ScreenshotFormat.Png);

    }

    protected override bool OnBackButtonPressed()
    {
        nowPlayingBtmSheet.IsPresented = false;
        Debug.WriteLine("Back btn Pressed");
        return true;
        //return base.OnBackButtonPressed();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        //NPBtmPage.HeightRequest = this.Height;
        NPBtmPage.IsPresented = true;
    }
    private void syncCol_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            SyncedLyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + " When scrolling");
        }

    }
    protected override void OnDisappearing()
    {
        nowPlayingBtmSheet.PropertyChanged -= bottomView_PropertyChanged;
        base.OnDisappearing();
    }

    private void BringNowPlayBtmSheetDownBtn_Clicked(object sender, EventArgs e)
    {
        nowPlayingBtmSheet.IsPresented = false;
        
    }

    DateTime lastKeyStroke;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        lastKeyStroke = DateTime.Now;
        var thisKeyStroke = lastKeyStroke;
        await Task.Delay(250);
        if (thisKeyStroke == lastKeyStroke)
        {
            var searchText = e.NewTextValue;
            if (searchText.Length >=2)
            {
                HomePageVM.SearchSongCommand.Execute(searchText);
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);
            }
        }        
    }

    private void SearchFAB_Clicked(object sender, TouchEventArgs e)
    {
        SearchBackDrop.IsPresented = !SearchBackDrop.IsPresented;
    }
}