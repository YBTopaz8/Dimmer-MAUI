using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.Desktop;

public partial class FullStatsPageD : ContentPage
{
    public FullStatsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        MyViewModel = homePageVM;
    }
    public HomePageVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        
        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }
        MyViewModel.CurrentPage = PageEnum.FullStatsPage;
        MyViewModel.CurrentPageMainLayout = MainDock;
        //MyViewModel.ShowGeneralTopXSongsCommand.Execute(null);
        StatsTabs.SelectedItem = StatsTabs.Children[0];
        MyViewModel.CallStats();
    }

    private async void StatsTabs_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {
        var selectedTab = StatsTabs.SelectedItem;
        var send = (SfChipGroup)sender;
        var selected = send.SelectedItem as SfChip;
        if (selected is null)
        {
            return; 
        }
        _ = int.TryParse(selected.CommandParameter.ToString(), out int selectedStatView);        
        
        switch (selectedStatView)
        {
            case 0:

                //GeneralStatsView front, rest back
                break;
            case 1:
                
                //SongsStatsView front, rest back
                //MyViewModel.GetNotListenedStreaks();
                //MyViewModel.GetTopStreakTracks();

                //MyViewModel.GetGoldenOldies();


                //MyViewModel.GetBiggestFallers(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetStatisticalOutlierSongs();
                //MyViewModel.GetDailyListeningVolume();
                //MyViewModel.GetUniqueTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetNewTracksInMonth(DateTime.Now.Month, DateTime.Now.Year);
                //MyViewModel.GetOngoingGapBetweenTracks();
                break;
            case 2:
                //ViewModel.
                //ViewModel.InitializeDailyPlayEventData();
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                
                break;
            case 6:
                
                break;
            default:

                break;
        }

        var viewss = new Dictionary<int, View>
        {
            {0, GeneralStatsView},
            {1, SongsStatsView},
            {2, ArtistsStatsView},
            {3, AlbumsStatsView},
            {4, DimmsStatsView},
            {5, PlaylistsStatsView},
            {6, GenreStatsView }
        };
        if (!viewss.ContainsKey(selectedStatView))
            return;

        await Task.WhenAll
            (viewss.Select(kvp =>
            kvp.Key == selectedStatView
            ? kvp.Value.AnimateFadeInFront()
            : kvp.Value.AnimateFadeOutBack()));
        return;
    }

    int SelectedGeneralView;

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {
        var send = (SfEffectsView)sender;
        int.TryParse(send.TouchUpCommandParameter.ToString(), out SelectedGeneralView);

        switch (SelectedGeneralView)
        {
            case 0:
                break;
            case 1:
                break;                
            case 2:
                break;
            case 3:
                break;                
            case 4:
                break;
            case 5:
                break;                
            case 6:
                break;
            case 7:
                break;                
            case 8:
                break;
            case 9:
                break;                
            case 10:
                break;
            case 11:
                break;
            default:
                break;
        }

    }


    private async void FocusModePointerRec_PEntered(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmIn(500);
     
    }
    private async void FocusModePointerRec_PExited(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.DimmOut(300);
     
    }

    private void StatView_Loaded(object sender, EventArgs e)
    {
        var send = (View)sender;
        _ = send.DimmOut(300);
    }

    private void DataPointSelectionBehavior_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangedEventArgs e)
    {
        var send = sender as PieSeries;
        var itemss = send.ItemsSource as ObservableCollection<DimmData>;
        
        var song = MyViewModel.DisplayedSongs.FirstOrDefault(X=> X.LocalDeviceId == itemss[e.NewIndexes[0]].SongId);

        MyViewModel.MySelectedSong = song;
        if (ClickToPreview)
        {
            MyViewModel.PlaySong(song, true);
        }        
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        MyViewModel.IsPreviewing=false;
    }


    public bool ClickToPreview { get; set; } = true;

    private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        ClickToPreview = e.Value;
    }
}
