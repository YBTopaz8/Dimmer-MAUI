
#if ANDROID
using Dimmer_MAUI.Platforms.Android;
#endif

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
    public NowPlayingSongPageBtmSheet NowPlayingBtmSheet { get; set; }

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
                HomePageVM.TemporarilyPickedSong = HomePageVM.PickedSong;
            }
            else
            {
                HomePageVM.SearchSongCommand.Execute(string.Empty);
                SongsColView.SelectedItem = HomePageVM.TemporarilyPickedSong;
            }
        }        
    }

    private void SearchFAB_Clicked(object sender, EventArgs e)
    {
        SongsColView.ScrollTo(HomePageVM.TemporarilyPickedSong, position:ScrollToPosition.Center, animate: false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.LoadSongCoverImage();

#if ANDROID
        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
#endif
    }


    
    private void SpecificSong_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 0;
        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private void SongsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {        

    }
}

public class ThumblessSlider : Slider
{
    public ThumblessSlider()
    {
#if ANDROID
        Microsoft.Maui.Handlers.SliderHandler.Mapper.AppendToMapping("No Thumb", (handler, view) =>
        {
            if(view is ThumblessSlider)
            {
                handler.PlatformView.SetThumb(null);
            }
        });
#endif
    }
}

