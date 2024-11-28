namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow(Lazy<HomePageVM> viewModel)
	{
        InitializeComponent();
        HomepageVM = viewModel;
        BindingContext = viewModel;
        
    }

    public Lazy<HomePageVM> HomepageVM { get; }

    protected override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 950;
        this.MinimumWidth = 1200;
        this.Height = 950;
        this.Width = 1200;

#if DEBUG
        DimmerTitleBar.Subtitle = "v0.5.0-debug";
#endif

#if RELEASE
        DimmerTitleBar.Subtitle = "v0.5.0-release";
#endif

        StickTopImgBtn.IsVisible = HomepageVM.Value.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.Value.IsStickToTop;
        syncingCloud.IsVisible = false;
        loggedInCloud.IsVisible = false;
        
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
                    HomepageVM.Value.IsOnSearchMode = true;

                    HomepageVM.Value.DisplayedSongs = HomepageVM.Value.SongsMgtService.AllSongs
                        .Where(item => item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase))
                        .ToObservableCollection();
                }
            }
            else
            {
                HomepageVM.Value.IsOnSearchMode = false;
                await HomepageVM.Value.LoadSongsInBatchesAsync();
            }
        }
        catch (TaskCanceledException)
        {
            // Handle task cancellation (optional, for debugging/logging)
        }
    }

    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        HomepageVM.Value.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = HomepageVM.Value.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.Value.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        HomepageVM.Value.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = HomepageVM.Value.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !HomepageVM.Value.IsStickToTop;
    }
}