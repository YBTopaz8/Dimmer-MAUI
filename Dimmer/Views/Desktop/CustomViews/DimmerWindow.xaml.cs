namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow()
	{
        InitializeComponent();
        
        
    }

    public HomePageVM MyViewModel { get; set; }

    protected override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 950;
        this.MinimumWidth = 1200;
        this.Height = 950;
        this.Width = 1200;
#if DEBUG
        DimmerTitleBar.Subtitle = "v1.2-debug";
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSeaGreen;
#endif

#if RELEASE
        DimmerTitleBar.Subtitle = "v1.2-release";
#endif

        if (!InitChecker())
        {
            return;
        }
        
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
        if(MyViewModel.CurrentUser is not null)
        {
            onlineCloud.IsVisible = MyViewModel.CurrentUser.IsAuthenticated;
        }


    }
    bool InitChecker()
    {
        MyViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();

        if (MyViewModel is null)
        {
            return false;
        }
        return true;
    }
    private CancellationTokenSource _debounceTimer;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if  (!InitChecker())
        {
            return;
        }
        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        switch (MyViewModel.CurrentPage)
        {
            case PageEnum.SetupPage:
                break;
            case PageEnum.SettingsPage:
                break;
            case PageEnum.MainPage:

                if (MyViewModel.SongsMgtService.AllSongs is null)
                {
                    return;
                }
                if (MyViewModel.DisplayedSongs is null)
                {
                    return;
                }

                try
                {
                    await Task.Delay(300, token);

                    if (!string.IsNullOrEmpty(txt))
                    {
                        if (txt.Length >= 1)
                        {
                            MyViewModel.IsOnSearchMode = true;
                            MyViewModel.DisplayedSongs.Clear();

                            // Directly filter the songs based on the search text, with null checks
                            var fSongs = MyViewModel.SongsMgtService.AllSongs
                                .Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                               (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                               (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(txt, StringComparison.OrdinalIgnoreCase)))
                                .ToList();

                            MyViewModel.FilteredSongs = fSongs;

                            foreach (var song in fSongs)
                            {
                                MyViewModel.DisplayedSongs.Add(song);
                            }
                            MyViewModel.CurrentQueue = 1;

                            OnPropertyChanged(nameof(MyViewModel.DisplayedSongs));
                            return;
                        }
                    }
                    else
                    {
                        MyViewModel.IsOnSearchMode = false;
                        MyViewModel.DisplayedSongs.Clear();

                        // Repopulate with all songs when search is empty
                        if (MyViewModel.SongsMgtService.AllSongs != null)
                        {
                            foreach (var song in MyViewModel.SongsMgtService.AllSongs)
                            {
                                MyViewModel.DisplayedSongs.Add(song);
                            }
                        }
                        MyViewModel.CurrentQueue = 0;
                    }
                }
                //catch (TaskCanceledException)
                //{
                //    // Expected if the debounce timer is cancelled
                //}
                catch (Exception ex)
                {
                    Debug.WriteLine($"Search Error: {ex}"); // Log the full exception for debugging
                }
                break;
            case PageEnum.NowPlayingPage:
                break;
            case PageEnum.PlaylistsPage:
                break;
            case PageEnum.FullStatsPage:
                break;
            case PageEnum.AllArtistsPage:
                break;
            case PageEnum.AllAlbumsPage:
                if (MyViewModel.SongsMgtService.AllAlbums is null)
                {
                    return;
                }
                
                if (MyViewModel.AllAlbums is null)
                {
                    return;
                }
                try
                {
                    await Task.Delay(3000);
                    await Shell.Current.DisplayAlert("Info", "Search is Not Available ...Yet!", "Ok");
                    return;
                    await Task.Delay(300, token);

                    if (!string.IsNullOrEmpty(txt))
                    {
                        if (txt.Length >= 1)
                        {

                            MyViewModel.AllAlbums.Clear();
                            // Directly filter the songs based on the search text, with null checks
                            var fAlbums = MyViewModel.SongsMgtService.AllAlbums
                                .Where(item => (!string.IsNullOrEmpty(item.Name) && item.Name.Contains(txt, StringComparison.OrdinalIgnoreCase))).ToList();


                            foreach (var song in fAlbums)
                            {
                                MyViewModel.AllAlbums.Add(song);
                            }

                            OnPropertyChanged(nameof(MyViewModel.AllAlbums));
                            return;
                        }
                    }
                    else
                    {
                        MyViewModel.IsOnSearchMode = false;
                        MyViewModel.AllAlbums.Clear();

                        // Repopulate with all songs when search is empty
                        if (MyViewModel.SongsMgtService.AllAlbums != null)
                        {
                            foreach (var song in MyViewModel.SongsMgtService.AllAlbums)
                            {
                                MyViewModel.AllAlbums.Add(song);
                            }
                            OnPropertyChanged(nameof(MyViewModel.AllAlbums));
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected if the debounce timer is cancelled
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Search Error: {ex}"); // Log the full exception for debugging
                }
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
        if (MyViewModel.CurrentPage != PageEnum.MainPage)
            return;

    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        MyViewModel.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        MyViewModel.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {        
        //EventEmoji.IsAnimationPlaying = !EventEmoji.IsAnimationPlaying;
    }
}