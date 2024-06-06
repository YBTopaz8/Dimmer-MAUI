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
    
    private async void SaveViewButton_Clicked(object sender, EventArgs e)
    { //to capture views into a png , will be useful later for saving
        
        //var image = await btmcontrols.CaptureAsync();
        //var savePath = Path.Combine("/storage/emulated/0/Documents", "test.png");
        //using Stream fileStream = File.OpenWrite(savePath);
        //await image.CopyToAsync(fileStream, ScreenshotFormat.Png);

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        //NPBtmPage.HeightRequest = this.Height;
        //NPBtmPage.IsPresented = true;
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

    private async void SearchFAB_Clicked(object sender, TouchEventArgs e)
    {
        SearchBackDrop.IsPresented = !SearchBackDrop.IsPresented;
        
    }

    private void SearchFAB_LongPressed(object sender, TouchEventArgs e)
    {
        SongsColView.ScrollTo(HomePageVM.PickedSong);
        //when longpressed, scroll to the currently playing song
    }

    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);
    }
}