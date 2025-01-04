using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow(Lazy<HomePageVM> viewModel)
	{
        InitializeComponent();
        HomepageVM = viewModel.Value;
        BindingContext = viewModel.Value;
        
    }

    public HomePageVM HomepageVM { get; }

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

        StickTopImgBtn.IsVisible = HomepageVM.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.IsStickToTop;
        onlineCloud.IsVisible = HomepageVM.CurrentUser.IsAuthenticated;


    }

    private CancellationTokenSource _debounceTimer;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (HomepageVM.CurrentPage != PageEnum.MainPage)
            return;

        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        if (HomepageVM.SongsMgtService.AllSongs is null)
        {
            return;
        }
        if (HomepageVM.DisplayedSongs is null)
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
                    HomepageVM.IsOnSearchMode = true;
                    HomepageVM.DisplayedSongs.Clear();

                    // Directly filter the songs based on the search text, with null checks
                    var fSongs = HomepageVM.SongsMgtService.AllSongs
                        .Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                       (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                       (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(txt, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    HomepageVM.filteredSongs = fSongs;

                    foreach (var song in fSongs)
                    {
                        HomepageVM.DisplayedSongs.Add(song);
                    }
                    HomepageVM.CurrentQueue = 1;

                    if (HomepageVM.DisplayedSongs.Count < 1)
                    {
                        HomepageVM.DisplayedSongs.Clear();
                    }
                    OnPropertyChanged(nameof(HomepageVM.DisplayedSongs));
                    return;
                }
            }
            else
            {
                HomepageVM.IsOnSearchMode = false;
                HomepageVM.DisplayedSongs.Clear();

                // Repopulate with all songs when search is empty
                if (HomepageVM.SongsMgtService.AllSongs != null)
                {
                    foreach (var song in HomepageVM.SongsMgtService.AllSongs)
                    {
                        HomepageVM.DisplayedSongs.Add(song);
                    }
                }
                HomepageVM.CurrentQueue = 0;
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
        HomepageVM.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = HomepageVM.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        HomepageVM.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = HomepageVM.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.IsStickToTop;
    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {
        
        EventEmoji.IsAnimationPlaying = !EventEmoji.IsAnimationPlaying;
    }
}