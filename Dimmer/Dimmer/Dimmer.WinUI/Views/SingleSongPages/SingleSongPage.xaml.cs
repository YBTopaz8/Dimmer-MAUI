using Syncfusion.Maui.Toolkit.Charts;

namespace Dimmer.WinUI.Views.SingleSongPages;

public partial class SingleSongPage : ContentPage
{
    public SingleSongPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
    }

    public BaseViewModelWin MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
       
            await MyViewModel.LoadSongLastFMData();
            await MyViewModel.LoadSongLastFMMoreData();

        
    }

    private void DataPointSelectionBehavior_SelectionChanging(object sender, Syncfusion.Maui.Toolkit.Charts.ChartSelectionChangingEventArgs e)
    {
        return;
        var ee = e.NewIndexes;
        var old = e.OldIndexes;

        var pie = sender as PieSeries;
        if (e.NewIndexes is null || e.NewIndexes.Count<1)
        {
            return;

        }
        var indexx = e.NewIndexes[0];
        var itemm = pie.ItemsSource as ObservableCollection<DimmerStats>;
        if (itemm is null)
        {
            return;
        }
        var songg = itemm[indexx];
        if (songg is null)
        {
            return;
        }
        if (songg.Song is null)
        {
            return;
        }
        var sss = MyViewModel.SearchResults.First(x => x.Id==songg.Song.Id);


        MyViewModel.SelectedSong=sss;
        //ColViewOfTopSongs.SelectedItem=       songg;
        Debug.WriteLine($"Selected Song: {songg.Song.Title}, Played {songg.Count} times.");

        //var actSong=((PieSeries)sender)
        Debug.WriteLine(ee);
        Debug.WriteLine(old);
    }
}