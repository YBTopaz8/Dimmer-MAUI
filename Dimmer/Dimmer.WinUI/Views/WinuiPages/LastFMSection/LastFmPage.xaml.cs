using Hqub.Lastfm.Entities;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;
using Track = Hqub.Lastfm.Entities.Track;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.LastFMSection;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LastFmPage : Page
{
   
public ObservableCollection<Track> RecentTracks { get; } = new();
    public ObservableCollection<Track> TopTracks { get; } = new();
    public ObservableCollection<Track> LovedTracks { get; } = new();

    public LastFMViewModel MyLastFMViewModel { get; private set; }
    public BaseViewModelWin MyViewModel { get; private set; }

    public LastFmPage()
    {
        this.InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        MyLastFMViewModel = IPlatformApplication.Current!.Services.GetService<LastFMViewModel>()!;

        MyLastFMViewModel.LoadBaseViewModel(MyViewModel);
    }

    private readonly Compositor _compositor;
    protected async override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Resolve VM

        

        await LoadUserData();


       
    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        
        base.OnNavigatingFrom(e);

    }
    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var pivot = sender as Pivot;
        if (pivot != null)
            switch (pivot.SelectedIndex)
            {
                case 0: /*LoadRecentTracks();*/ break;
                case 1: /*LoadTopTracks();*/ break;
                case 2: /*LoadLovedTracks();*/ break;
            }
    }
    private void LoginLastFM_Click(object sender, RoutedEventArgs e)
    {
        //LastFMViewModel.LastFMName = LastFMUname.Text;
        MyLastFMViewModel?.LoginToLastfmCommand.Execute(null);
    }
    private void LastFMUname_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        var send = (TextBox)sender;
        if (send is null) return;
        if (MyViewModel is null) return;

        var isPressedKeyEnterOrReturn = e.Key == Windows.System.VirtualKey.Enter;
        if (isPressedKeyEnterOrReturn)
        {
 
            //LoginLastFMBtn.IsEnabled = false;
            MyLastFMViewModel?.LoginToLastfmCommand.Execute(null);

        }
    }
    LastFMUserView? user;
    private async Task LoadUserData()
    {

        // 1. Get Data from VM
        //user = MyViewModel.CurrentUserLocal?.LastFMAccountInfo; // Assuming this property exists on your VM parity
        
        if (MyLastFMViewModel.LastFMService.IsAuthenticated)
        {
            LastFMGridNonAuth.Visibility = WinUIVisibility.Collapsed;
            LastFMAuthedSection.Visibility = WinUIVisibility.Visible;
            // User Info
            var userr = await MyLastFMViewModel.LastFMService.GetUserInfoAsync();
            if (userr is null)
            {
                var usr = MyViewModel.CurrentUserLocal.LastFMAccountInfo;
                if (usr is not null)
                {
                    user = usr;
                }
                else
                {
                    return;
                }
            }
                else
            {
                user = userr.ToLastFMUserView();
            }
            
            UserNameTxt.Text = user?.Name;
            TotalScrobblesTxt.Text = $"{user?.Playcount:N0} Scrobbles";
            scrobblingSince.Text = $"Scrobbling since {user?.Registered:dd MMM yyyy}";
            // If user.Image is a string URL:
            if (!string.IsNullOrEmpty(user?.Image?.Url))
            {
                if (!string.IsNullOrEmpty(user.Image.Url))
                {
                    UserAvatarImg.ProfilePicture
                        = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Image.Url));
                }
                UserAvatarImg.DisplayName = user.Name;
            }

            if (Connectivity.NetworkAccess != NetworkAccess.Internet) return;
            await MyLastFMViewModel.LoadUserLastFMDataAsync(user);

        }
        else
        {
            LastFMGridNonAuth.Visibility = WinUIVisibility.Visible;
            LastFMAuthedSection.Visibility = WinUIVisibility.Collapsed;
            UserNameTxt.Text = "Not Connected";
            TotalScrobblesTxt.Text = "Log in via Settings";
        }
    }

    Microsoft.UI.Xaml.Controls.Button? SongTitlebutton;
    private Album? trackAlbum;

    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {
        SongTitlebutton = sender as Button;
        trackModel = SongTitlebutton?.DataContext as Track;
        MyViewModel.SelectedTrack = trackModel;
        var songModelView = trackModel.IsOnPresentDevice ? MyViewModel.SearchResults.FirstOrDefault(song => song.Id.ToString() == trackModel?.OnDeviceObjectId)
            :null;

        AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail, SongTitlebutton);

        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(SongDetailPage);
        var navParams = new SongDetailNavArgs
        {
            Song = songModelView,
            ViewModel = MyViewModel,
        };

        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        Frame?.NavigateToType(songDetailType, navParams, navigationOptions);

    }
    Track? trackModel = null;
    private async void RecentTracksList_Loaded(object sender, RoutedEventArgs e)
    {
       
    }

    private void LogoutLastFM_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.LogoutFromLastfmCommand.Execute(null);
    }

    private void SongAlbum_Click(object sender, RoutedEventArgs e)
    {
        SongTitlebutton = sender as Button;
        trackAlbum = SongTitlebutton?.DataContext as Album;
        if (trackAlbum != null)
        {
            MyViewModel.SelectedLastFMAlbum = trackAlbum;
            var AlbumModelview = trackAlbum.IsOnPresentDevice ? MyViewModel.SearchResults.FirstOrDefault(song => song.Album?.Name.ToString() == trackAlbum.Artist.Name)
                : null;

            AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail, SongTitlebutton);

            var supNavTransInfo = new SuppressNavigationTransitionInfo();
            Type songDetailType = typeof(AlbumPage);
            var navParams = new SongDetailNavArgs
            {
                ExtraParam = trackAlbum,
                Song= null!,
                ViewModel = MyViewModel,
            };

            FrameNavigationOptions navigationOptions = new FrameNavigationOptions
            {
                TransitionInfoOverride = supNavTransInfo,
                IsNavigationStackEnabled = true

            };

            Frame?.NavigateToType(songDetailType, navParams, navigationOptions);
        }
    }

    private async void LoveTrackButton_Click(object sender, RoutedEventArgs e)
    {

        var send = (sender as Button)!;
        var trackk = (send.DataContext as Track)!;
        var songg = MyViewModel.SearchResults.First(x => x.Id.ToString() == trackk.OnDeviceObjectId);
        await MyViewModel.AddFavoriteRatingToSongAsync(songg);
    }

    private void BanTrackButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void DeleteScrobble_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var btn = (sender as Button)!;
        var trackk = (btn.DataContext as Hqub.Lastfm.Entities.Track)!;
        var comParam = btn.Content as string;
        
        await MyViewModel.OpenSongInOnlineSearch(comParam, trackk.Name,trackk.Artist.Name);
    }

    private async void ExportAbsentSongsToTxtBtn_Click(object sender, RoutedEventArgs e)
    {
        var abSentSongs= MyLastFMViewModel.CollectionOfUserLovedTracks?.Where(x=>!x.IsOnPresentDevice)?.ToList();
        string LineText = string.Empty;
        if (abSentSongs is null) return;
        foreach (var song in abSentSongs)
        {
            LineText = LineText+ $"{song.Name} - {song.Artist.Name} - {song.Album?.Name} {Environment.NewLine}";

        }

        if (sender is Button button)
        {
            button.IsEnabled = false;

            var picker = new FileSavePicker();

            picker.DefaultFileExtension = ".txt";

            picker.SuggestedFileName = "LastFMFavoritesAbsentOnDevice";

            picker.CommitButtonText = "Save File";

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var hwnd = PlatUtils.DimmerHandle;
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeChoices.Add("text", new List<string>() { ".txt" });

            // Show the picker dialog
            var result = await picker.PickSaveFileAsync();

            if (result != null)
            {
                string savePath = result.Path;
                await File.WriteAllTextAsync(savePath, LineText);

            }
            else
            {

            }

            button.IsEnabled = true;

        }


    }

    private void LastFMUname_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        MyLastFMViewModel.LastFMName = LastFMUname.Text;
    }
}