using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow()
	{
        InitializeComponent();
        
        
    }

    public HomePageVM Homepagevm { get; set; }

    protected override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 950;
        this.MinimumWidth = 1200;
        this.Height = 950;
        this.Width = 1200;
#if DEBUG
        DimmerTitleBar.Subtitle = "v1.0e-debug";
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
#endif

#if RELEASE
        DimmerTitleBar.Subtitle = "v1.0e-release";
#endif

        if (!InitChecker())
        {
            return;
        }
        BindingContext = Homepagevm;
        StickTopImgBtn.IsVisible = Homepagevm.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !Homepagevm.IsStickToTop;
        onlineCloud.IsVisible = Homepagevm.CurrentUser.IsAuthenticated;


    }
    bool InitChecker()
    {
        Homepagevm ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();

        if (Homepagevm is null)
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
        if (Homepagevm.CurrentPage != PageEnum.MainPage)
            return;

        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        if (Homepagevm.SongsMgtService.AllSongs is null)
        {
            return;
        }
        if (Homepagevm.DisplayedSongs is null)
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
                    Homepagevm.IsOnSearchMode = true;
                    Homepagevm.DisplayedSongs.Clear();

                    // Directly filter the songs based on the search text, with null checks
                    var fSongs = Homepagevm.SongsMgtService.AllSongs
                        .Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                       (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                       (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(txt, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    Homepagevm.filteredSongs = fSongs;

                    foreach (var song in fSongs)
                    {
                        Homepagevm.DisplayedSongs.Add(song);
                    }
                    Homepagevm.CurrentQueue = 1;

                    if (Homepagevm.DisplayedSongs.Count < 1)
                    {
                        Homepagevm.DisplayedSongs.Clear();
                    }
                    OnPropertyChanged(nameof(Homepagevm.DisplayedSongs));
                    return;
                }
            }
            else
            {
                Homepagevm.IsOnSearchMode = false;
                Homepagevm.DisplayedSongs.Clear();

                // Repopulate with all songs when search is empty
                if (Homepagevm.SongsMgtService.AllSongs != null)
                {
                    foreach (var song in Homepagevm.SongsMgtService.AllSongs)
                    {
                        Homepagevm.DisplayedSongs.Add(song);
                    }
                }
                Homepagevm.CurrentQueue = 0;
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
    }
    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        Homepagevm.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = Homepagevm.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !Homepagevm.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        Homepagevm.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = Homepagevm.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !Homepagevm.IsStickToTop;
    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {
        
        EventEmoji.IsAnimationPlaying = !EventEmoji.IsAnimationPlaying;
    }
}