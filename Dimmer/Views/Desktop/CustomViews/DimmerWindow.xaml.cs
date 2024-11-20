namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow(HomePageVM homepageVM)
	{
        InitializeComponent();
        HomepageVM = homepageVM;
        BindingContext = homepageVM;
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

        DimmerTitleBar.Subtitle = "v0.4.2-debug";
#endif

#if RELEASE
        DimmerTitleBar.Subtitle = "v0.4.2-release";
#endif

        StickTopImgBtn.IsVisible = HomepageVM.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.IsStickToTop;
    }

    private CancellationTokenSource _debounceTimer;

    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        // Cancel the previous task if it's still running
        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        try
        {
            await Task.Delay(300, token);

            if (!string.IsNullOrEmpty(txt))
            {
                if (txt.Length >= 1)
                {
                    HomepageVM.IsOnSearchMode = true;

                    HomepageVM.DisplayedSongs = HomepageVM.SongsMgtService.AllSongs
                        .Where(item => item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase))
                        .ToObservableCollection();
                }
            }
            else
            {
                HomepageVM.IsOnSearchMode = false;
                await HomepageVM.LoadSongsInBatchesAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (optional, for debugging/logging)
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
}